#region Directive Section

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    public class PADI_Worker : MarshalByRefObject
    {
        #region Initialization

        ObjectServer thisServer;
        private ObjectServer[] objectServerList;

        private Dictionary<int,ServerPadInt> padIntActiveList;
        
        public PADI_Worker()
        {
            padIntActiveList = new Dictionary<int,ServerPadInt>();
           
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Bootstarp object servers with the master
        /// </summary>
        /// <param name="workerPort"></param>
        /// <returns></returns>
        public bool BootstrapMaster(string workerPort)
        {
            bool isBootstraped = false;
            String masterUrl = Common.GetMasterTcpUrl();
            //String workerIp = Common.GetLocalIPAddress();
            String workerIp = ConfigurationManager.AppSettings[Constants.APPSET_WORKER_IP];
            PADI_Master masterObj = (PADI_Master)Activator.GetObject(typeof(PADI_Master), masterUrl);
            thisServer = masterObj.Bootstrap(workerIp, workerPort);
            if (thisServer != null)
            {
                isBootstraped = true;
            }
            Console.WriteLine("Worker server :" + thisServer.ServerName + "started. Bootstrap status:" + isBootstraped);
            Common.Logger().LogInfo("Worker server :" + thisServer.ServerName + " started", "Port : " + workerPort, "Bootstrap status:" + isBootstraped);
            return isBootstraped;
        }

        /// <summary>
        /// Send periodic heart beats to the master
        /// </summary>
        /// <param name="state"></param>
        public void SendHeartBeatMessage(object state)
        {
            PADI_Master master = (PADI_Master)Activator.GetObject(typeof(PADI_Master), Common.GetMasterTcpUrl());
            master.HeartBeatReceiver(thisServer.ServerName);
        }

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// Receive the new object server map from server when object server join or left
        /// </summary>
        /// <param name="objectServerList"></param>
        public void ReceiveObjectServerList(ObjectServer[] objectServerList)
        {
            this.objectServerList=objectServerList;
            Console.WriteLine("New Object Server List received");
            Common.Logger().LogInfo("New Object Server List received",string.Empty,string.Empty);
        }

        /// <summary>
        /// Read the value from the UID within the transaction TID
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="TID"></param>
        /// <returns></returns>
        public int Read(int uid,long TID) 
        {
            lock (this)
            {
                try
                {
                    return padIntActiveList[uid].Read(TID);
                }
                catch (Exception ex)
                {
                    throw new TxException(ex.Message,ex);
                }
            }

        }

        /// <summary>
        /// Write the value to the UID within the transaction TID
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="TID"></param>
        /// <param name="value"></param>
        public void Write(int uid, long TID, int value)
        {
            lock (this)
            {
                bool isWriteSuccessful=padIntActiveList[uid].Write(TID, value);
                if (!isWriteSuccessful)
                {
                    //TODO: Ask coordinator to abort the transaction TID
                    throw new TxException("Write aborted TID=" + TID);
                }

            }
        }

        /// <summary>
        /// Create a ServerPadInt for the requests of clients
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool CreatePadInt(int uid)
        {
            lock (this)
            {
                bool isCreated = false;
                ServerPadInt newPadInt = null;
                if (!padIntActiveList.ContainsKey(uid))
                {
                    newPadInt = new ServerPadInt(uid, this);
                    padIntActiveList.Add(uid, newPadInt);
                    isCreated = true;
                }
                return isCreated;
            }
        }

        /// <summary>
        /// Inform the availability of the requested ServerPdInt in the server
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool AccessPadInt(int uid)
        {
            lock (this)
            {
                bool isAccessed = false;
                ServerPadInt padInt = null;
                if (padIntActiveList.ContainsKey(uid))
                {
                    padInt = padIntActiveList[uid];
                    isAccessed = true;
                }
                return isAccessed;
            }
        }
        
        /// <summary>
        /// Check posibility of commit for the requests of Coordiator
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool CanCommit(long TID)
        {
            lock (this)
            {
                bool canCommit = false;
                List<int> uidsToCommit = GetUidsRelatedToTid(TID);
                foreach (var uid in uidsToCommit)
                {
                    canCommit = padIntActiveList[uid].CanCommit(TID);
                    if (!canCommit)
                    {
                        break;
                    }
                }
                return canCommit;
            }
        }

        /// <summary>
        /// Enforce the commits for the requests of Coordinator
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool DoCommit(long TID)
        {
            lock (this)
            {
                bool isCommited = false;
                List<int> uidsToCommit = GetUidsRelatedToTid(TID);
                foreach (var uid in uidsToCommit)
                {
                    isCommited = padIntActiveList[uid].Commit(TID);
                    if (!isCommited)
                    {
                        break;
                    }
                }

                //TODO: abort the previously completed commits if any
                return isCommited;
            }
        }

        /// <summary>
        /// Force to dump the server current status to console
        /// </summary>
        public void DumpStatus()
        {
            lock (this)
            {
                Console.WriteLine("\n---------------------Server Status (Start)------------------------");
                foreach (var val in padIntActiveList)
                {
                    Console.WriteLine("Uid = " + val.Key + ", Value = " + val.Value.Value + ", Commited = " + val.Value.IsCommited);
                    foreach (var tentative in val.Value.TentativeList)
                    {
                        Console.WriteLine("Tentative TID = " + tentative.WriteTS + " Value = " + tentative.Value);
                    }
                    Console.WriteLine("\n");
                }
                Console.WriteLine("---------------------Server Status (END)------------------------\n");
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Get UIDs related to a transaction id. 
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        private List<int> GetUidsRelatedToTid(long TID)
        {
            lock (this)
            {
                List<int> uids = new List<int>();
                foreach (var item in padIntActiveList)
                {
                    if (item.Value.TentativeList.Exists(x => x.WriteTS == TID))
                        uids.Add(item.Key);
                }
                return uids;
            }
        }

        #endregion
    }
}
