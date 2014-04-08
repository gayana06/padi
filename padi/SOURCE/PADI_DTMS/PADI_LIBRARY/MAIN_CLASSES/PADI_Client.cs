#region Directive Section

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    public class PADI_Client : MarshalByRefObject
    {
        #region Initialization

        PADI_Coordinator coordinator;
        PADI_Master master;
        List<PADI_Worker> workers;
        List<int> padIntUids;
        Information info;
        long transactionId;

        public long TransactionId
        {
            get { return transactionId; }
            set { transactionId = value; }
        }

        #endregion

        #region Public Members

        public bool Init()
        {
            bool isInitSuccessful = false;
            try
            {
                //TODO:Load the Info object when start and save it when client exit. Currently new object is created.
                coordinator = (PADI_Coordinator)Activator.GetObject(typeof(PADI_Coordinator), Common.GenerateTcpUrl(ConfigurationManager.AppSettings[Constants.APPSET_MASTER_IP], ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT], Constants.OBJECT_TYPE_PADI_COORDINATOR));
                master = (PADI_Master)Activator.GetObject(typeof(PADI_Master), Common.GetMasterTcpUrl());
                workers = new List<PADI_Worker>();
                info = new Information();
                padIntUids = new List<int>();
                isInitSuccessful = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return isInitSuccessful;
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
        /// Create a ServerPadInt object in the remote server.
        /// Returns null if creation failed.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public PadInt CreatePadInt(int uid)
        {
            lock (this)
            {
                PadInt padInt = null;
                int modIndex = Common.GetModuloServerIndex(uid, info.ObjectServerMap);
                if (modIndex >= 0)
                {
                    if (workers[modIndex].CreatePadInt(uid))
                    {
                        padInt = new PadInt(uid, this);
                        padInt.Worker = workers[modIndex];
                        UpdatePadIntTrack(uid);
                        Console.WriteLine("PadInt successfully created, UID = " + uid);
                        Common.Logger().LogInfo("PadInt successfully created, UID = " + uid, string.Empty, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("CreatePadInt for UID = " + uid + " returned null. Already exists");
                        Common.Logger().LogInfo("CreatePadInt for UID = " + uid + " returned null. Already exists", string.Empty, string.Empty);
                    }
                }
                else
                {
                    Console.WriteLine("No worker server found");
                    Common.Logger().LogInfo("No worker server found", string.Empty, string.Empty);
                }
                return padInt;
            }
        }

        /// <summary>
        /// Already existing object server is selected for further manipulation. (Read/Write)
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public PadInt AccessPadInt(int uid)
        {
            lock (this)
            {
                int modIndex = Common.GetModuloServerIndex(uid, info.ObjectServerMap);
                PadInt padInt = null;
                if (modIndex >= 0)
                {
                    if (workers[modIndex].AccessPadInt(uid))
                    {
                        padInt = new PadInt(uid, this);
                        padInt.Worker = workers[modIndex];
                        UpdatePadIntTrack(uid);
                        Console.WriteLine("PadInt successfully retrieved, UID = " + uid);
                        Common.Logger().LogInfo("PadInt successfully retrieved, UID = " + uid, string.Empty, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("AccessPadInt for UID = " + uid + " returned null. Not available");
                        Common.Logger().LogInfo("AccessPadInt for UID = " + uid + " returned null. Not available", string.Empty, string.Empty);
                    }
                }
                else
                {
                    Console.WriteLine("No worker servers found");
                    Common.Logger().LogInfo("No worker servers found", string.Empty, string.Empty);
                }
                return padInt;
            }
        }

        /// <summary>
        /// Set the TID from coordinator and load server map if the available map is expired
        /// </summary>
        /// <returns></returns>
        public bool TxBegin()
        {
            lock (this)
            {
                bool trancationEstablished = false;
                //Clear padIntUids. This is only required if multiple transactions are checked in a single machine.
                padIntUids.Clear();
                try
                {
                    string tidReply = coordinator.BeginTxn();
                    Console.WriteLine("Coordinator reply : " + tidReply);
                    string[] tempSep = tidReply.Split(Constants.SEP_CHAR_COLON);
                    long receivedTimeStamp = long.Parse(tempSep[1]);
                    if (receivedTimeStamp != info.AvailableMasterMapTimeStamp)
                    {
                        info.AvailableMasterMapTimeStamp = receivedTimeStamp;
                        LoadServerMap();
                    }
                    TransactionId = long.Parse(tempSep[0]);
                    trancationEstablished = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return trancationEstablished;
            }
        }

        /// <summary>
        /// Dumps the status of worker serers to their consoles.
        /// </summary>
        /// <returns></returns>
        public bool Status()
        {
            master.DumpObjectServerStatus();
            return true;
        }

        /// <summary>
        /// Call the coordinator to proceed with transaction commit
        /// </summary>
        /// <returns></returns>
        public bool TxCommit()
        {
            lock (this)
            {
                bool isCommited = false;
                int[] uidArray = padIntUids.ToArray();
                isCommited = coordinator.Commit(TransactionId, uidArray);
                return isCommited;
            }
        }

        /// <summary>
        /// Call coordinator to abort the transaction
        /// </summary>
        /// <returns></returns>
        public bool TxAbort()
        {
            //TODO: abort the transaction. Call coordinator's abort method. The implementation is similar to commit.
            return true;
        }

        /// <summary>
        /// This method makes the server at the URL stop responding to external calls except for a Recover call
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool Fail(string url)
        {
            //TODO: this method makes the server at the URL stop responding to external calls except for a Recover call
            return true;
        }

        /// <summary>
        /// This method makes the server at URL stop responding to external calls 
        /// but it maintains all calls for later reply, as if the communication to that server were
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool Freeze(string url)
        {
            //TODO: this method makes the server at URL stop responding to external calls but it maintains all calls for later reply, as if the communication to that server were
            //only delayed.
            return true;
        }

        /// <summary>
        /// This recovers the Fail and Freeze servers.        
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool Recover(string url)
        {
            //TODO: recover the Freeze and Fail.
            return true;
        }


        #endregion 

        #region Private Members

        private void LoadServerMap()
        {
            lock (this)
            {
                info.ObjectServerMap = master.WorkerServerList.ToArray();
                workers.Clear();
                PADI_Worker worker;
                foreach (var server in info.ObjectServerMap)
                {
                    string ip = server.ServerIp;
                    string port = server.ServerPort;
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(ip, port, Constants.OBJECT_TYPE_PADI_WORKER));
                    workers.Add(worker);
                }
                Console.WriteLine("Loaded the new map, size=" + info.ObjectServerMap.Count());
            }
        }

        private void UpdatePadIntTrack(int uid)
        {
            if (!padIntUids.Contains(uid))
            {
                padIntUids.Add(uid);
            }
        }

        #endregion
    }

    [Serializable]
    class Information
    {
        private long availableMasterMapTimeStamp;

        public long AvailableMasterMapTimeStamp
        {
            get { return availableMasterMapTimeStamp; }
            set { availableMasterMapTimeStamp = value; }
        }

        private ObjectServer[] objectServerMap;

        public ObjectServer[] ObjectServerMap
        {
            get { return objectServerMap; }
            set { objectServerMap = value; }
        }


    }
}
