#region Directive Section

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace PADI_LIBRARY
{
    public class PADI_Worker : MarshalByRefObject
    {
        #region Initialization

        ObjectServer thisServer;
        ObjectServer replicaServer;
        private ObjectServer[] objectServerList;
        private Dictionary<int, ServerPadInt> padIntActiveList;
        private Dictionary<int, ServerPadInt> padIntReplicaList;
        private Dictionary<int, ServerPadInt> padIntShuffelList;
        // private List<FreezedOperation> freezedOperations;
        // private int freezeOperationIndex;
        // bool isRecovering;
        bool isThisServerFreezed;
        bool isThisServerFailed;
        bool isShufflingActive;

        public bool IsThisServerFreezed
        {
            get { return isThisServerFreezed; }
            set { isThisServerFreezed = value; }
        }

        public PADI_Worker()
        {
            padIntActiveList = new Dictionary<int, ServerPadInt>();
            padIntReplicaList = new Dictionary<int, ServerPadInt>();
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
            if (!isThisServerFailed)
            {
                PADI_Master master = (PADI_Master)Activator.GetObject(typeof(PADI_Master), Common.GetMasterTcpUrl());
                //if (!master.FailServerList.Exists(s => s.ServerName == thisServer.ServerName))
                master.HeartBeatReceiver(thisServer.ServerName);
            }
        }

        /// <summary>
        /// Fail the server. This will stop sending heartbeats to master 
        /// </summary>
        public bool Fail()
        {
            lock (this)
            {
                bool canFail = false;
                if (!isThisServerFailed && !isThisServerFreezed)
                {
                    isThisServerFailed = true;
                    padIntActiveList.Clear();
                    padIntReplicaList.Clear();
                    canFail = true;
                }
                return canFail;
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
        /// Receive the new object server map from server when object server join or left
        /// </summary>
        /// <param name="objectServerList"></param>
        public void UpdateServerList(ObjectServer[] objectServerList)
        {
            this.objectServerList = objectServerList;
            if (objectServerList != null)
            {
                thisServer = Common.GetObjectServerByName(thisServer.ServerName,objectServerList.ToList());
                replicaServer = Common.GetObjectServerByName(thisServer.ReplicaServerName, objectServerList.ToList());
            }
            Console.WriteLine("New Object Server List received");
            Console.WriteLine("This server = " + thisServer.ServerName + " and replica = " + replicaServer.ServerName);
            Common.Logger().LogInfo("New Object Server List received", string.Empty, string.Empty);
        }

        /// <summary>
        /// Redistribute the PadIntActiveList to the new group
        /// </summary>
        /// <returns></returns>
        public bool Shuffle()
        {
            bool hasCompleted = false;
            try
            {
                Dictionary<String, PADI_Worker> workerSet = new Dictionary<string, PADI_Worker>();
                ProcessShuffle(workerSet, padIntActiveList);
                ProcessShuffle(workerSet, padIntReplicaList);
                hasCompleted=true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Shuffel failed. "+ex.Message);
                Common.Logger().LogError(ex.Message, ex.StackTrace, ex.Source);
            }
            return hasCompleted;
        }

        /// <summary>
        /// Redistribute the PadIntActiveList to the new group
        /// </summary>
        /// <param name="workerSet"></param>
        /// <param name="dataset"></param>
        private void ProcessShuffle(Dictionary<String, PADI_Worker> workerSet, Dictionary<int,ServerPadInt> dataset)
        {
            PADI_Worker workerRef;
            ObjectServer worker;
            foreach (var item in dataset)
            {
                int workerIndex = item.Key % objectServerList.Length;
                worker = objectServerList[workerIndex];

                if (worker.ServerName == thisServer.ServerName)
                {
                    AddShuffleData(item.Key, item.Value);
                }
                else
                {
                    if (!workerSet.ContainsKey(worker.ServerName))
                    {
                        workerRef = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(worker.ServerIp, worker.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                        workerSet.Add(worker.ServerName, workerRef);
                    }
                    else
                    {
                        workerRef = workerSet[worker.ServerName];
                    }
                    workerRef.AddShuffleData(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Initiate the padIntShuffelList data structure
        /// </summary>
        /// <returns></returns>
        public void WorkerReadyForShuffel()
        {
            isShufflingActive = true;
            padIntShuffelList = new Dictionary<int, ServerPadInt>();
        }

        /// <summary>
        /// Initiate the padIntShuffelList data structure
        /// </summary>
        /// <returns></returns>
        public void WorkerReadyForReplicate()
        {
            padIntReplicaList = new Dictionary<int, ServerPadInt>();
        }

        //Replicate the full data structure
        public bool DoReplicate()
        {
            bool hasReplicated = false;
            PADI_Worker workerRef;
            try
            {
                workerRef = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(replicaServer.ServerIp, replicaServer.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                workerRef.UpdateReplica(padIntActiveList);
                hasReplicated = true;
            }
            catch (Exception ex)
            {

            }
            return hasReplicated;
        }


        public void AddShuffleData(int key, ServerPadInt value)
        {
            if (value.IsCommited && !padIntShuffelList.ContainsKey(key))
            {
                ServerPadInt serverPadInt = new ServerPadInt(key,this,value.WriteTS,value.ReadTSList,value.TentativeList,value.IsCommited,value.Value);
                padIntShuffelList.Add(key, serverPadInt);
            }
        }

        /// <summary>
        /// Copy shuffled data to the active padint data structure
        /// </summary>
        public void PersistShuffleData()
        {
            padIntActiveList=new Dictionary<int,ServerPadInt>(padIntShuffelList);
            padIntShuffelList = null;
            padIntReplicaList.Clear();
            Console.WriteLine("Shuffled data persisted");
            isShufflingActive = false;
        }

        /// <summary>
        /// Get the padints to be replicated after a successful commit
        /// </summary>
        /// <param name="uidArray"></param>
        /// <returns></returns>
        public Dictionary<int, ServerPadInt> GetReplicaPadints(int[] uidArray)
        {

            Console.WriteLine("Starting the data Replication...");
            Console.WriteLine("This are the Uids to be replicated! ");
            Dictionary<int, ServerPadInt> replicaPadints = new Dictionary<int, ServerPadInt>();
            replicaPadints.Clear();
            Dictionary<int, ServerPadInt> tempPadIntActiveList = new Dictionary<int, ServerPadInt>(padIntActiveList);
            foreach (var val in tempPadIntActiveList)
            {
                for (int i = 0; i < uidArray.Length; i++)
                {
                    if (val.Key == uidArray[i])
                    {
                        Console.WriteLine("Added Uid: " + val.Key + " To replicaPadints");
                        replicaPadints.Add(val.Key, val.Value);
                    }
                }
            }
            return replicaPadints;
        }

        /// <summary>
        /// Connect the replica and update the results
        /// </summary>
        /// <param name="replicaPadints"></param>
        public void UpdateReplica(Dictionary<int, ServerPadInt> replicaPadints)
        {
            lock (this)
            {
                foreach (var replica in replicaPadints)
                {
                    if (padIntReplicaList.ContainsKey(replica.Key))
                    {
                        padIntReplicaList.Remove(replica.Key);
                    }
                    ServerPadInt serverPadInt = new ServerPadInt(replica.Key, this, replica.Value.WriteTS, replica.Value.ReadTSList, replica.Value.TentativeList, replica.Value.IsCommited, replica.Value.Value);
                    padIntReplicaList.Add(replica.Key, serverPadInt);
                }
                //Console.WriteLine("Successfully updated the replicas!");
            }
        }

        /// <summary>
        /// Read the value from the UID within the transaction TID
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="TID"></param>
        /// <returns></returns>
        public int Read(int uid, long TID)
        {
            try
            {
                int result = padIntActiveList[uid].Read(TID);
                lock (this)
                {
                    if (isThisServerFreezed)
                        Monitor.Wait(this);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new TxException(ex.Message, ex);
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
            bool isWriteSuccessful = padIntActiveList[uid].Write(TID, value);
            lock (this)
            {
                if (isThisServerFreezed)
                    Monitor.Wait(this);
            }
            if (!isWriteSuccessful)
            {
                PADI_Coordinator coordinator = (PADI_Coordinator)Activator.GetObject(typeof(PADI_Coordinator), Common.GetCoordinatorTcpUrl());
                coordinator.AbortTxn(TID);
                Console.WriteLine("Write aborted TID=" + TID);
               // throw new TxException("Write aborted TID=" + TID);
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
            bool isAccessed = false;
            ServerPadInt padInt = null;
            if (padIntActiveList.ContainsKey(uid))
            {
                padInt = padIntActiveList[uid];
                isAccessed = true;
            }
            return isAccessed;

        }

        /// <summary>
        /// Check posibility of commit for the requests of Coordiator.
        /// If no UID related to TID is not in tentative this return true.
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool CanCommit(long TID)
        {
            bool canCommit = false;
            List<int> uidsToCommit = GetUidsRelatedToTid(TID);
            if (uidsToCommit.Count() > 0)
            {
                foreach (var uid in uidsToCommit)
                {
                    canCommit = padIntActiveList[uid].CanCommit(TID);
                    if (!canCommit)
                    {
                        break;
                    }
                }
            }
            else
            {
                canCommit = true;
            }
            return canCommit;

        }

        /// <summary>
        /// Enforce the commits for the requests of Coordinator.
        /// Return true even if nothing available to commit.
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool DoCommit(long TID, ref Dictionary<int,ServerPadInt> replicaSet)
        {
            bool isCommited = false;
            List<int> uidsToCommit = GetUidsRelatedToTid(TID);
            if (uidsToCommit.Count() > 0)
            {
                foreach (var uid in uidsToCommit)
                {
                    isCommited = padIntActiveList[uid].Commit(TID);
                    if (!isCommited)
                    {
                        break;
                    }
                    replicaSet.Add(uid, padIntActiveList[uid]);
                }
            }
            else
            {
                return true;
            }

            return isCommited;

        }

        /// <summary>
        /// Force to dump the server current status to console
        /// </summary>
        public void DumpStatus()
        {
            try
            {
                Console.WriteLine("\n---------------------Server Status (Start)------------------------");
                Dictionary<int, ServerPadInt> tempPadIntActiveList = new Dictionary<int, ServerPadInt>(padIntActiveList);
                Dictionary<int, ServerPadInt> tempPadIntReplicaList = new Dictionary<int, ServerPadInt>(padIntReplicaList);
                Console.WriteLine("\n---------------------Active PadInt Status (Start) Server = "+thisServer.ServerName+" ------------------------");
                foreach (var val in tempPadIntActiveList)
                {
                    Console.WriteLine("\nUid = " + val.Key + ", Value = " + val.Value.Value + ", Commited = " + val.Value.IsCommited + ", TID = " + val.Value.WriteTS);
                    Console.WriteLine("\nLatest 5 Readers of this UID");
                    if (val.Value.ReadTSList.Count > 0)
                    {
                        if (val.Value.ReadTSList.Count > 5)
                        {
                            for (int i = (val.Value.ReadTSList.Count - 1); i > (val.Value.ReadTSList.Count - 6); i--)
                            {
                                Console.WriteLine("TID = " + val.Value.ReadTSList[i]);
                            }
                        }
                        else
                        {
                            foreach (var reader in val.Value.ReadTSList)
                            {
                                Console.WriteLine("TID = " + reader);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No transaction has read this UID");
                    }

                    Console.WriteLine("\nTentative writes of this UID");
                    if (val.Value.TentativeList.Count > 0)
                    {
                        foreach (var tentative in val.Value.TentativeList)
                        {
                            Console.WriteLine("Tentative TID = " + tentative.WriteTS + " Value = " + tentative.Value);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No tentative writes for this UID");
                    }
                    Console.WriteLine("\n");
                }
                Console.WriteLine("\n---------------------Active PadInt Status (End)------------------------");
                Console.WriteLine("\n---------------------Replica PadInt Status (Start) Server = " + thisServer.ServerName + "------------------------");
                foreach (var val in tempPadIntReplicaList)
                {
                    Console.WriteLine("\nUid = " + val.Key + ", Value = " + val.Value.Value + ", Commited = " + val.Value.IsCommited + ", TID = " + val.Value.WriteTS);
                    Console.WriteLine("\nLatest five Readers of this UID");
                    if (val.Value.ReadTSList.Count > 0)
                    {
                        if (val.Value.ReadTSList.Count > 5)
                        {
                            for (int i = (val.Value.ReadTSList.Count - 1); i > (val.Value.ReadTSList.Count - 6); i--)
                            {
                                Console.WriteLine("TID = " + val.Value.ReadTSList[i]);
                            }
                        }
                        else
                        {
                            foreach (var reader in val.Value.ReadTSList)
                            {
                                Console.WriteLine("TID = " + reader);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No transaction has read this UID");
                    }

                    Console.WriteLine("\nTentative writes of this UID");
                    if (val.Value.TentativeList.Count > 0)
                    {
                        foreach (var tentative in val.Value.TentativeList)
                        {
                            Console.WriteLine("Tentative TID = " + tentative.WriteTS + " Value = " + tentative.Value);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No tentative writes for this UID");
                    }
                    Console.WriteLine("\n");
                }
                Console.WriteLine("---------------------Replica PadInt Status (END)------------------------");
                Console.WriteLine("---------------------Server Status (END)------------------------\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Call TxAbort for each of the tentative objects.
        /// Return true even if nothing available to abort.
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool Abort(long TID)
        {
            bool isAborted = false;
            List<int> uidsToAbort = GetUidsRelatedToTid(TID);
            if (uidsToAbort.Count() > 0)
            {
                foreach (var uid in uidsToAbort)
                {
                    isAborted = padIntActiveList[uid].TxAbort(TID);
                }
            }
            else
            {
                return true;
            }

            return isAborted;
        }


        public bool Freeze()
        {
            lock (this)
            {
                bool canFreeze = false;
                if (!isThisServerFailed && !isThisServerFreezed)
                {
                    isThisServerFreezed = true;
                    canFreeze = true;
                }
                return canFreeze;
            }

        }

        public bool Recover()
        {
            lock (this)
            {
                bool hasRecovered = false;
                if (isThisServerFreezed)
                {
                    Monitor.PulseAll(this);
                    isThisServerFreezed = false;
                    hasRecovered = true;
                }
                else if (isThisServerFailed)
                {
                    BootstrapMaster(thisServer.ServerPort);
                    Console.WriteLine("Recovered failed server");
                    isThisServerFailed = false;
                    hasRecovered = true;
                }
                return hasRecovered;
            }

        }

        /// <summary>
        /// Periodically runs and remove older tentitive rights to prvent deadlock
        /// </summary>
        public void TentativeWriteTimeOutMonitor(Object state)
        {
            try
            {
                if (!isThisServerFailed && !IsThisServerFreezed && !isShufflingActive)
                {
                    PADI_Coordinator coordinator = (PADI_Coordinator)Activator.GetObject(typeof(PADI_Coordinator), Common.GetCoordinatorTcpUrl());
                    long tid = 0;
                    foreach (var item in padIntActiveList)
                    {
                        if (item.Value.TentativeList.Count > 1)
                        {
                            if (DateTime.Now.Subtract(item.Value.TentativeList[0].CreatedTimeStamp).Seconds > 3)
                            {
                                tid = item.Value.TentativeList[0].WriteTS;
                                coordinator.AbortTxn(tid);
                                item.Value.TentativeList[1].CreatedTimeStamp = DateTime.Now;
                                Console.WriteLine("TentativeWriteTimeOutMonitor order abort to TID = "+tid);                                                           
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TentativeWriteTimeOutMonitor :"+ex.Message);
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
            List<int> uids = new List<int>();
            Dictionary<int, ServerPadInt> tempPadIntActiveList = new Dictionary<int, ServerPadInt>(padIntActiveList);
            foreach (var item in tempPadIntActiveList)
            {
                if (item.Value.TentativeList.Exists(x => x.WriteTS == TID))
                    uids.Add(item.Key);
            }
            return uids;
        }
        
        #endregion
    }
}
