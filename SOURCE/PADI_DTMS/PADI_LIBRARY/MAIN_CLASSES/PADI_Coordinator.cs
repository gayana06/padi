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
        List<long> transactionIdList;
        public PADI_Coordinator(PADI_Master master)
        {
            this.master=master;
            transactionIdList=new List<long>();
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
                transactionIdList.Add(tid);
                return tid;
            }

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
