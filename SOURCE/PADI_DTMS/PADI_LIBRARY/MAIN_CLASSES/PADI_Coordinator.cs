using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_LIBRARY
{
    public class PADI_Coordinator : MarshalByRefObject
    {
        PADI_Master master;
       // List<long> transactionIdList;
        Dictionary<long, List<CommitRequestStatus>> transactionIdDict;

        public PADI_Coordinator(PADI_Master master)
        {
            this.master=master;
            transactionIdDict=new Dictionary<long, List<CommitRequestStatus>>();
        }

        /// <summary>
        /// Generate a unique value and always increase with time
        /// </summary>
        /// <returns></returns>
        private long GetTransactionId()
        {
            lock(this)
            {
                Thread.Sleep(1);
                long tid=DateTime.Now.Ticks;
                transactionIdDict.Add(tid, new List<CommitRequestStatus>());
                return tid;
            }

        }

        public bool Commit(long tid, int[] uidArray)
        {
            ObjectServer selectedServer;
            CommitRequestStatus commitRS;
            bool finished = false;

            foreach (var uid in uidArray)
            {
                selectedServer = master.WorkerServerList[uid % master.WorkerServerList.Count()];
                commitRS = new CommitRequestStatus();
                commitRS.Server = selectedServer;
                commitRS.Vote = false;
                transactionIdDict[tid].Add(commitRS);
            }

            PADI_Worker worker;
            foreach (var commitR in transactionIdDict[tid])
            {
                worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                commitR.Vote = worker.CanCommit(tid);
            }

            if (transactionIdDict[tid].Exists(x => x.Vote == false))
            {
                //TODO Send Abort to all the servers
            }
            else
            {
                foreach (var commitR in transactionIdDict[tid])
                {
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(commitR.Server.ServerIp, commitR.Server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                    commitR.HasCommited = worker.DoCommit(tid);
                }

                //TODO After everyone commits update the replicas
                if (transactionIdDict[tid].Exists(x => x.HasCommited == false))
                {
                    finished = false;
                }
                else
                {
                    finished = true;
                }

            }
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
            string txnReply=GetTransactionId().ToString()+Constants.SEP_COLON+master.LatestWorkerServerViewTimeStamp.ToString();
            Common.Logger().LogInfo("Transaction Begun : "+txnReply,string.Empty,string.Empty);
            return txnReply;
        }

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
         
    }
}
