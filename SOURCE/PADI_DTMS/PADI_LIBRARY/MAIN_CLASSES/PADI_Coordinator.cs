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
        Dictionary<long, List<CommitRequestStatus>> transactionIdDict;

        public PADI_Coordinator(PADI_Master master)
        {
            this.master = master;
            transactionIdDict = new Dictionary<long, List<CommitRequestStatus>>();
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
            lock (this)
            {
                bool finished = false;
                GenerateCommitRequests(tid, uidArray);
                GatherCanCommitVotes(tid);

                if (transactionIdDict[tid].Exists(x => x.Vote == false))
                {
                    //TODO: Send Abort to all the servers
                }
                else
                {
                    TransactionDoCommit(tid);
                    finished = CheckHasCommitted(tid);
                    //TODO: After everyone commits update the replicas
                    if (finished)
                    {
                        transactionIdDict.Remove(tid);
                    }
                    else
                    {
                        //TODO: Send Abort to all the servers
                    }
                }
                return finished;
            }
        }

        /// <summary>
        /// Abort the transaction contacting object servers
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        /// <returns></returns>
        public bool Abort(long tid, int[] uidArray)
        {
            //TODO: Handle abort with object servers same as commit
            return true;
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
                string txnReply = GetTransactionId().ToString() + Constants.SEP_COLON + master.LatestWorkerServerViewTimeStamp.ToString();
                Common.Logger().LogInfo("Transaction Begun : " + txnReply, string.Empty, string.Empty);
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
            lock (this)
            {
                Thread.Sleep(1);
                long tid = DateTime.Now.Ticks;
                transactionIdDict.Add(tid, new List<CommitRequestStatus>());
                return tid;
            }

        }

        /// <summary>
        /// Commit/Abort related support object generation
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        private void GenerateCommitRequests(long tid, int[] uidArray)
        {
            ObjectServer selectedServer;
            CommitRequestStatus commitRS;

            foreach (var uid in uidArray)
            {
                selectedServer = master.WorkerServerList[uid % master.WorkerServerList.Count()];
                if (!transactionIdDict[tid].Exists(x => x.Server == selectedServer))
                {
                    commitRS = new CommitRequestStatus();
                    commitRS.Server = selectedServer;
                    commitRS.Vote = false;
                    commitRS.HasAborted = false;
                    transactionIdDict[tid].Add(commitRS);
                }
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
                worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                commitR.Vote = worker.CanCommit(tid);
            }
        }

        /// <summary>
        /// Confirm transaction commit to the object servers
        /// </summary>
        /// <param name="tid"></param>
        private void TransactionDoCommit(long tid)
        {
            PADI_Worker worker;
            foreach (var commitR in transactionIdDict[tid])
            {
                worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                commitR.HasCommited = worker.DoCommit(tid);
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

        #endregion
    }
}
