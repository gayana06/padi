#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace PADI_LIBRARY
{
    public class PADI_Coordinator : MarshalByRefObject
    {
        #region Initialization

        PADI_Master master;
        List<ObjectServer> objectServerList;
        Dictionary<long, List<OperationRequestStatus>> transactionIdDict;
        bool hasHaltTidGeneration;
        bool hasPendingTransactions;
        System.Threading.Timer monitorTransaction = null;
        int monitorCounter;

        public bool HasPendingTransactions
        {
          get { return hasPendingTransactions; }
          set { hasPendingTransactions = value; }
        }

        public bool HasHaltTidGeneration
        {
            get { return hasHaltTidGeneration; }
            set { hasHaltTidGeneration = value; }
        }

        public PADI_Coordinator(PADI_Master master)
        {
            this.master = master;
            transactionIdDict = new Dictionary<long, List<OperationRequestStatus>>();
            HasHaltTidGeneration = false;
        }

        public void UpdateObjectServerList(List<ObjectServer> newList)
        {
            objectServerList = new List<ObjectServer>(newList);
        }

        public void MonitorPendingTransactionsBeforeStabilize()
        {
            HasHaltTidGeneration = true;
            monitorTransaction = new System.Threading.Timer(CheckPendingTransAvailable, null, 1000, 1000);
            monitorCounter = 0;
        }

        public void CheckPendingTransAvailable(Object state)
        {
            if (transactionIdDict.Count == 0)
            {
                monitorTransaction.Dispose();
                Console.WriteLine("No pending transactions at coordinator now.");
                HasPendingTransactions = false;
                master.HasPendingTransactions = false;                
            }
            else if (monitorCounter > 3)
            {
                monitorCounter = -1000; //just to make sure this will not run again until all abort
                Console.WriteLine("Forcefully aborting transactions at coordinator now.");
                Dictionary<long, List<OperationRequestStatus>> PendingTransactions = new Dictionary<long, List<OperationRequestStatus>>(transactionIdDict);
                foreach (var item in PendingTransactions)
                {
                    AbortTxn(item.Key);
                }
            }
            else
            {
                monitorCounter++;
            }
        }

        public void StartTransactions()
        {
            HasHaltTidGeneration = false;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Perform two phase commit with the object servers
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        /// <returns></returns>
        public bool Commit(long tid, int[] uidArray)
        {
            bool finished = false;
            if (transactionIdDict.ContainsKey(tid))
            {
                GenerateCommitRequests(tid, uidArray);
                BlockIfAnyWorkerFreezed(tid);
                GatherCanCommitVotes(tid);

                if (transactionIdDict[tid].Exists(x => x.Vote == false))
                {                   
                    TransactionDoAbort(tid);
                    Console.WriteLine("Transaction aborted tid = " + tid);
                }
                else
                {
                    TransactionDoCommit(tid);
                    finished = CheckHasCommitted(tid);
                    if (finished)
                    {                       
                        PADI_Worker replica;
                       
                        string replicaServerName;
                        ObjectServer myReplica;
                        Dictionary<int, ServerPadInt> replicaPadints;
                        foreach (var commitR in transactionIdDict[tid])
                        {
                            try
                            {
                                replicaServerName = commitR.Server.ReplicaServerName;
                                myReplica = objectServerList.SingleOrDefault(item => item.ServerName == replicaServerName);
                                replicaPadints = commitR.ReplicaSet;
                                replica = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(myReplica.ServerIp, myReplica.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                                replica.UpdateReplica(replicaPadints);
                                replicaPadints.Clear();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("In replication, Server = " + commitR.Server.ReplicaServerName + " is already failed." + ex.Message);
                            }
                        }
                        transactionIdDict.Remove(tid);
                    }
                    else
                    {
                        TransactionDoAbort(tid);
                        Console.WriteLine("Transaction aborted tid = " + tid);
                    }
                }
            }
            else
            {
                Console.WriteLine("TID = "+tid +" has already been completed.");
            }
            return finished;

        }

        /// <summary>
        /// Abort the transaction contacting object servers
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        /// <returns></returns>
        public bool AbortTxn(long tid, int[] uidArray)
        {           
            bool finished = false;
            GenerateCommitRequests(tid, uidArray);
            BlockIfAnyWorkerFreezed(tid);
            TransactionDoAbort(tid);
            finished = CheckHasAborted(tid);            
            if (finished)
            {
                transactionIdDict.Remove(tid);
            }
            else
            {
                //TODO: retry abort
            }            
            return finished;
        }

        /// <summary>
        /// Abort the transaction contacting object servers
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        /// <returns></returns>
        public bool AbortTxn(long tid)
        {
            //NOTE: There can be a situation where no commitR in the dictionary.
            //This method works if transaction commits first but fails and aborts after that.
            
            bool finished = false;
            PADI_Worker worker;
            foreach (var server in objectServerList)
            {
                try
                {
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker),
                        Common.GenerateTcpUrl(server.ServerIp, server.ServerPort,
                        Constants.OBJECT_TYPE_PADI_WORKER));
                    worker.Abort(tid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("At AbortTxn "+ex.Message);
                }
            }
            transactionIdDict.Remove(tid);
            finished = true;
            return finished;
        }


        /// <summary>
        /// Create a Transaction in the below format.
        /// TID:TIMESTAMP_WS_MAP
        /// 
        /// TIMESTAMP_WS_MAP is the latest ticks with a stable view of worker servers
        /// </summary>
        /// <returns></returns>
        public string BeginTxn()
        {
            lock (this)
            {
                string txnReply=null;
                long tid = GetTransactionId();
                if (tid > 0)
                {
                    txnReply = tid.ToString() + Constants.SEP_COLON + master.LatestWorkerServerViewTimeStamp.ToString();
                    Common.Logger().LogInfo("Transaction Begun : " + txnReply, string.Empty, string.Empty);                    
                }
                return txnReply;
            }
        }

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Generate a unique value and always increase with time
        /// </summary>
        /// <returns></returns>
        private long GetTransactionId()
        {
            long tid = -1;
            if (!HasHaltTidGeneration)
            {
                Thread.Sleep(1);
                tid = DateTime.Now.Ticks;
                transactionIdDict.Add(tid, new List<OperationRequestStatus>());  
                hasPendingTransactions=true;
                master.HasPendingTransactions = true;
            }
            return tid;
        }

        /// <summary>
        /// Commit/Abort related support object generation
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        private void GenerateCommitRequests(long tid, int[] uidArray)
        {
            ObjectServer selectedServer;
            OperationRequestStatus commitRS;

            foreach (var uid in uidArray)
            {
                selectedServer = objectServerList[uid % objectServerList.Count()];
                if (!transactionIdDict[tid].Exists(x => x.Server == selectedServer))
                {
                    commitRS = new OperationRequestStatus();
                    commitRS.Server = selectedServer;
                    commitRS.Vote = false;
                    commitRS.HasAborted = false;
                    transactionIdDict[tid].Add(commitRS);
                }
            }
        }

        /// <summary>
        /// This blocks if any of the worker servers contains a freezed server
        /// </summary>
        /// <param name="tid"></param>
        private void BlockIfAnyWorkerFreezed(long tid)
        {
            lock (this)
            {
                while (true)
                {
                    bool hasFreezedServer = false;
                    PADI_Worker worker;
                    foreach (var commitR in transactionIdDict[tid])
                    {
                        worker=(PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                        if (worker.IsThisServerFreezed)
                        {
                            hasFreezedServer = true;
                            break;
                        }
                    }

                    if (hasFreezedServer)
                        Monitor.Wait(this);
                    else
                        break;
                }
            }
        }

        public void RecoverOperations()
        {
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Gather can commit votes in Two phase commit
        /// </summary>
        /// <param name="tid"></param>
        private void GatherCanCommitVotes(long tid)
        {
            PADI_Worker worker;
            foreach (var commitR in transactionIdDict[tid])
            {
                try
                {
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                    commitR.Vote = worker.CanCommit(tid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("In commit voteing, Server = " + commitR.Server.ReplicaServerName + " has already left.");
                    commitR.Vote = false;
                }
            }

        }

        /// <summary>
        /// Confirm transaction commit to the object servers
        /// </summary>
        /// <param name="tid"></param>
        private void TransactionDoCommit(long tid)
        {
            PADI_Worker worker;
            Dictionary<int, ServerPadInt> replicaSet;
            foreach (var commitR in transactionIdDict[tid])
            {
                try
                {
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                    replicaSet = new Dictionary<int, ServerPadInt>();
                    commitR.HasCommited = worker.DoCommit(tid,ref replicaSet);
                    commitR.ReplicaSet = replicaSet;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("In commit, Server = "+commitR.Server.ReplicaServerName+" has already left.");
                    commitR.HasCommited = false;
                }
            }
        }

        /// <summary>
        /// Confirm transaction abort to the object servers
        /// </summary>
        /// <param name="tid"></param>
        private void TransactionDoAbort(long tid)
        {
            PADI_Worker worker;
            foreach (var commitR in transactionIdDict[tid])
            {
                try
                {
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                    commitR.HasAborted = worker.Abort(tid);
                    Console.WriteLine("Transaction = " + tid + " has aborted, status =  " + commitR.HasAborted);
                }
                catch (Exception ex)
                {
                    commitR.HasAborted = true;
                    Console.WriteLine("In abort, Server = " + commitR.Server.ReplicaServerName +"has already left.");
                }
            }
        }

        /// <summary>
        /// Check all object has committed the transaction
        /// </summary>
        /// <param name="tid"></param>
        /// <returns></returns>
        private bool CheckHasCommitted(long tid)
        {
            bool hasCommited = false;
            if (transactionIdDict[tid].Exists(x => x.HasCommited == false))
            {
                hasCommited = false;
            }
            else
            {
                hasCommited = true;
            }
            return hasCommited;
        }

        /// <summary>
        /// Check all object has committed the transaction
        /// </summary>
        /// <param name="tid"></param>
        /// <returns></returns>
        private bool CheckHasAborted(long tid)
        {
            bool hasAborted = false;
            if (transactionIdDict[tid].Exists(x => x.HasAborted == false))
            {
                hasAborted = false;
            }
            else
            {
                hasAborted = true;
            }
            return hasAborted;
        }

        #endregion
    }
}
