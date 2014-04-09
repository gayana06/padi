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
        Dictionary<long, List<OperationRequestStatus>> transactionIdDict;

        public PADI_Coordinator(PADI_Master master)
        {
            this.master = master;
            transactionIdDict = new Dictionary<long, List<OperationRequestStatus>>();
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

        /// <summary>
        /// Pre-commit Abort
        /// Abort the transaction contacting object servers
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uidArray"></param>
        /// <returns></returns>
        public bool Abort(long tid, int[] uidArray)
        {
            ObjectServer selectedSrv;
            OperationRequestStatus abortReq;
            PADI_Worker worker;

            foreach (var uid in uidArray)
            {
                selectedSrv = master.WorkerServerList[uid % master.WorkerServerList.Count()];
                if (!transactionIdDict[tid].Exists(x => x.Server == selectedSrv))
                {
                    abortReq = new OperationRequestStatus();
                    abortReq.Server = selectedSrv;
                    abortReq.HasAborted = false;
                    transactionIdDict[tid].Add(abortReq);
                }
            }

            foreach (var abortRequest in transactionIdDict[tid])
            {
                worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker),
                    Common.GenerateTcpUrl(abortRequest.Server.ServerIp, abortRequest.Server.ServerPort,
                    Constants.OBJECT_TYPE_PADI_WORKER));
                abortRequest.HasAborted = worker.Abort(tid);

                if (abortRequest.HasAborted)
                {
                    transactionIdDict[tid].Remove(abortRequest);
                }

            }

            //TODO: Handle abort with object servers same as commit
            return true;
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
            //This method works if transaction commits first and aborts after that.
            //If abort occur directly, this will not work
            PADI_Worker worker;

            foreach (var rollbackReq in transactionIdDict[tid])
            {
                worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker),
                    Common.GenerateTcpUrl(rollbackReq.Server.ServerIp, rollbackReq.Server.ServerPort,
                    Constants.OBJECT_TYPE_PADI_WORKER));
                rollbackReq.HasAborted = worker.Abort(tid);
                transactionIdDict[tid].Remove(rollbackReq); // Remove the requests as well?
            }
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
                Thread.Sleep(1);
                long tid = DateTime.Now.Ticks;
                transactionIdDict.Add(tid, new List<OperationRequestStatus>());
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
                selectedServer = master.WorkerServerList[uid % master.WorkerServerList.Count()];
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
