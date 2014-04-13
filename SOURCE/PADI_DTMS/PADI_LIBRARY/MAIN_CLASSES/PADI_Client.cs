#region Directive Section

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

#endregion

namespace PADI_LIBRARY
{
    public class PADI_Client : MarshalByRefObject
    {
        #region Initialization

        private static PADI_Coordinator coordinator;
        private static PADI_Master master;
        private static List<PADI_Worker> workers;
        private static List<int> padIntUids;
        private static Information info;
        private static long transactionId;
        public delegate bool AsyncOperation(long TID,int[] uidArray);

        public static long TransactionId
        {
            get { return transactionId; }
            set { transactionId = value; }
        }

        #endregion

        #region Public Members

        public static bool Init()
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
                LoadServerMap();
                isInitSuccessful = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return isInitSuccessful;
        }

        /// <summary>
        /// Commit request will be replied here
        /// </summary>
        /// <param name="ar"></param>
        public  static void AsyncCommitCallBack(IAsyncResult ar)
        {
            try
            {
                AsyncOperation del = (AsyncOperation)((AsyncResult)ar).AsyncDelegate;
                Console.WriteLine("Commit is successful");
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Abort request will be replied here
        /// </summary>
        /// <param name="ar"></param>
        public static void AsyncAbortCallBack(IAsyncResult ar)
        {
            try
            {
                AsyncOperation del = (AsyncOperation)((AsyncResult)ar).AsyncDelegate;
                Console.WriteLine("Abort is successful");
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Abort failed");
                //throw ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Abort failed");
                //throw ex;
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

        /// <summary>
        /// Create a ServerPadInt object in the remote server.
        /// Returns null if creation failed.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static PadInt CreatePadInt(int uid)
        {
            PadInt padInt = null;
            int modIndex = Common.GetModuloServerIndex(uid, info.ObjectServerMap);
            if (modIndex >= 0)
            {
                if (workers[modIndex].CreatePadInt(uid))
                {
                    padInt = new PadInt(uid);
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

        /// <summary>
        /// Already existing object server is selected for further manipulation. (Read/Write)
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static PadInt AccessPadInt(int uid)
        {
            int modIndex = Common.GetModuloServerIndex(uid, info.ObjectServerMap);
            PadInt padInt = null;
            if (modIndex >= 0)
            {
                if (workers[modIndex].AccessPadInt(uid))
                {
                    padInt = new PadInt(uid);
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

        /// <summary>
        /// Set the TID from coordinator and load server map if the available map is expired
        /// </summary>
        /// <returns></returns>
        public static bool TxBegin()
        {
            bool trancationEstablished = false;
            //NOte: Clear padIntUids. This is only required if multiple transactions are checked in a single machine.
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

        /// <summary>
        /// Dumps the status of worker serers to their consoles.
        /// </summary>
        /// <returns></returns>
        public static bool Status()
        {
            master.DumpObjectServerStatus();
            return true;
        }

        /// <summary>
        /// Call the coordinator to proceed with transaction commit
        /// </summary>
        /// <returns></returns>
        public static bool TxCommit()
        {
            bool isCommited = false;
            int[] uidArray = padIntUids.ToArray();
            if (HasAnyServerFreezed(uidArray))
            {
                AsyncOperation commit = new AsyncOperation(coordinator.Commit);
                AsyncCallback commitCallback = new AsyncCallback(AsyncCommitCallBack);
                IAsyncResult RemAr = commit.BeginInvoke(TransactionId, uidArray, commitCallback, null);
                Console.WriteLine("Commit delays due to the freezed server.");
            }
            else
            {
                isCommited = coordinator.Commit(TransactionId, uidArray);
                Console.WriteLine("Transaction commit status is " + isCommited);                
            }
            return isCommited;
        }

        /// <summary>
        /// Call coordinator to abort the transaction
        /// </summary>
        /// <returns></returns>
        public static bool TxAbort()
        {
            //TODO: abort the transaction. Call coordinator's abort method. The implementation is similar to commit.
            bool isAborted = false;
            try
            {
                if (padIntUids.Count() > 0)
                {
                    int[] uidArray = padIntUids.ToArray();
                    if (HasAnyServerFreezed(uidArray))
                    {
                        AsyncOperation abort = new AsyncOperation(coordinator.AbortTxn);
                        AsyncCallback abortCallback = new AsyncCallback(AsyncAbortCallBack);
                        IAsyncResult RemAr = abort.BeginInvoke(TransactionId, uidArray, abortCallback, null);
                        Console.WriteLine("Abort delays due to the freezed server.");
                    }
                    else
                    {
                        isAborted = coordinator.AbortTxn(TransactionId, uidArray);
                    }
                }
                else
                {
                    Console.WriteLine("No UID has been manipulated to abort.");
                }
            }
            catch (Exception ex)
            {
                Common.Logger().LogError(ex.Message,ex.StackTrace,ex.Source);
                Console.WriteLine("Default abort is not executed.");
            }
            return isAborted;
        }

        /// <summary>
        /// This method makes the server at the URL stop responding to external calls except for a Recover call
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool Fail(string url)
        {
            //TODO: this method makes the server at the URL stop responding to external calls except for a Recover call
            info.ObjectServerMap = master.WorkerServerList.ToArray();
            foreach (var server in info.ObjectServerMap)
            {
                if (server.TcpUrl == url)
                {
                    PADI_Worker worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), url);
                    bool isFailed= worker.Fail();
                    Console.WriteLine("Wait until master detects failure");
                    Thread.Sleep(17000);
                    Console.WriteLine("Server Failed, Status "+isFailed+"; server url: {0}, server name: {1}", server.TcpUrl, server.ServerName);
                    break;
                }
            }
            return true;
        }

        /// <summary>
        /// This method makes the server at URL stop responding to external calls 
        /// but it maintains all calls for later reply, as if the communication to that server were
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool Freeze(string url)
        {
            //TODO: this method makes the server at URL stop responding to external calls but it maintains all calls for later reply, as if the communication to that server were
            //only delayed.
            PADI_Worker worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker),url);
            bool isFreezed=worker.Freeze();
            Console.WriteLine("Server Freezed. Status = "+isFreezed);
            return isFreezed;
        }

        /// <summary>
        /// This recovers the Fail and Freeze servers.        
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool Recover(string url)
        {
            //TODO: recover the Freeze and Fail.
            PADI_Worker worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), url);
            bool hasRecoveredServer= worker.Recover();
            coordinator.RecoverOperations();
            Console.WriteLine("Server recovered, Status = "+hasRecoveredServer);



         //Failed recsover
        /* info.ObjectServerMap = master.FailServerList.ToArray();
            foreach (var server in info.ObjectServerMap)
            {
                if (server.TcpUrl == url)
                {
                    master.WorkerServerList.Add(server);
                    master.UpdateRecoverList(server);
                    Console.WriteLine("Server Recovered; server url: {0}, server name: {1}", server.TcpUrl, server.ServerName);
                }
                else
                    Console.WriteLine("OOPs the specified server could not be found");
            }*/
            return true;
        }

        

        #endregion 

        #region Private Members

        private static void LoadServerMap()
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

        private static void UpdatePadIntTrack(int uid)
        {
            if (!padIntUids.Contains(uid))
            {
                padIntUids.Add(uid);
            }
        }

        private static bool HasAnyServerFreezed(int[] uidArray)
        {
            bool hasFreezed = false;
            foreach (int uid in uidArray)
            {
                int modIndex = Common.GetModuloServerIndex(uid, info.ObjectServerMap);
                if (workers[modIndex].IsThisServerFreezed)
                {
                    hasFreezed = true;
                    break;
                }
            }
            return hasFreezed;
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
