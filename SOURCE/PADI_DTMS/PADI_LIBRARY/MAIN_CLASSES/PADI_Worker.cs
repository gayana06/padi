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
        private ObjectServer[] objectServerList;
        private Dictionary<int, ServerPadInt> padIntActiveList;
        private Dictionary<int, ServerPadInt> padIntReplicaList;
        // private List<FreezedOperation> freezedOperations;
        // private int freezeOperationIndex;
        // bool isRecovering;
        bool isThisServerFreezed;
        bool isThisServerFailed;

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
        /// Redistribute the PadIntActiveList to the new group
        /// </summary>
        /// <param name="state"></param>
        public void Shuffle()
        {
            Dictionary<ObjectServer,List<int>> shuffList = 
                new Dictionary<ObjectServer, List<int>>();

            foreach (var obj in padIntActiveList)
            {
                if (!shuffList.ContainsKey(objectServerList[obj.Key % objectServerList.Count()]) 
                    && objectServerList[obj.Key % objectServerList.Count()] != thisServer)
                {
                    List<int> uids = new List<int>();
                    uids.Add(obj.Key);
                    shuffList.Add(objectServerList[obj.Key % objectServerList.Count()], uids);
                }
                else
                {
                    shuffList[objectServerList[obj.Key % objectServerList.Count()]].Add(obj.Key);
                }
            }

            foreach (var server in shuffList.Keys)
            {
                Dictionary<int, ServerPadInt> shuff = new Dictionary<int,ServerPadInt>();
                PADI_Worker worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker),
                    Common.GenerateTcpUrl(server.ReplicaServerName, server.ServerPort, 
                    Constants.OBJECT_TYPE_PADI_WORKER));

                //populate temp list and clean the redundant
                foreach (var obj in shuffList[server])
                {
                    shuff.Add(obj, padIntActiveList[obj]);
                    padIntActiveList.Remove(obj);
                }
                //TODO: shuff list may be passing references instead of values
                worker.UpdateObjects(shuff);
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
            Console.WriteLine("New Object Server List received");
            Common.Logger().LogInfo("New Object Server List received", string.Empty, string.Empty);
        }

        /// <summary>
        /// Set the ReplicaServerNames after every leave and join
        /// </summary>
        /// <param name="objectServerList"></param>
        public void PrintReplicaServerName(ObjectServer[] objectServerList)
        {
            this.objectServerList = objectServerList;
            int noOfServers = objectServerList.Count();
           // Array.Sort(objectServerList, (s1, s2) => s1.ServerName.CompareTo(s2.ServerName));
            for (int j = 0; j < noOfServers; j++)
            {
                    Console.WriteLine("My ReplicaServerName is Mrs. " + objectServerList[j].ReplicaServerName);
            }
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
        /// Update the list of replicated (padInt) objects
        /// </summary>
        /// <param name="replicaPadints"></param>
        public void UpdateReplica(Dictionary<int, ServerPadInt> replicaPadints)
        {
            foreach (var valReplica in replicaPadints)
            {
                if (!padIntReplicaList.ContainsKey(valReplica.Key)) //add if it doesnot exist
                    padIntReplicaList.Add(valReplica.Key, valReplica.Value);
                else
                {
                    padIntReplicaList[valReplica.Key] = valReplica.Value; //else update the value
                }
            }
            Console.WriteLine("Successfully updated the replicas!");
        }

        /// <summary>
        /// Update the padIntActiveList as part of the reshuffling procedure
        /// </summary>
        /// <param name="PadInts"></param>
        public void UpdateObjects(Dictionary<int, ServerPadInt> PadInts)
        {
            foreach (var obj in PadInts)
            {
                if (!padIntActiveList.ContainsKey(obj.Key))
                    padIntActiveList.Add(obj.Key, obj.Value);
                else
                {
                    padIntActiveList[obj.Key] = obj.Value;
                }
            }
            Console.WriteLine("Successfully *reshuffled* the active objects objects!");
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
                //TODO: Ask coordinator to abort the transaction TID
                Console.WriteLine("Write aborted TID=" + TID);
                throw new TxException("Write aborted TID=" + TID);
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
        public bool DoCommit(long TID)
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
                }
            }
            else
            {
                return true;
            }

            //TODO: abort the previously completed commits if any
            return isCommited;

        }

        /// <summary>
        /// Force to dump the server current status to console
        /// </summary>
        public void DumpStatus()
        {
            Console.WriteLine("\n---------------------Server Status (Start)------------------------");
            Dictionary<int, ServerPadInt> tempPadIntActiveList = new Dictionary<int, ServerPadInt>(padIntActiveList);
            Dictionary<int, ServerPadInt> tempPadIntReplicaList = new Dictionary<int, ServerPadInt>(padIntReplicaList);
            Console.WriteLine("\n---------------------Active PadInt Status (Start)------------------------");
            foreach (var val in tempPadIntActiveList)
            {
                Console.WriteLine("\nUid = " + val.Key + ", Value = " + val.Value.Value + ", Commited = " + val.Value.IsCommited + ", TID = " + val.Value.WriteTS);
                Console.WriteLine("\nReaders of this UID");
                if (val.Value.ReadTSList.Count > 0)
                {
                    foreach (var reader in val.Value.ReadTSList)
                    {
                        Console.WriteLine("TID = " + reader);
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
            Console.WriteLine("\n---------------------Replica PadInt Status (Start)------------------------");
            foreach (var val in tempPadIntReplicaList)
            {
                Console.WriteLine("\nUid = " + val.Key + ", Value = " + val.Value.Value + ", Commited = " + val.Value.IsCommited + ", TID = " + val.Value.WriteTS);
                Console.WriteLine("\nReaders of this UID");
                if (val.Value.ReadTSList.Count > 0)
                {
                    foreach (var reader in val.Value.ReadTSList)
                    {
                        Console.WriteLine("TID = " + reader);
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
                    if (!isAborted)
                    {
                        break;
                    }
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
            /*
            lock (this)
            {
                if (!isThisServerFreezed)
                {
                    freezeOperationIndex = 0;
                    isRecovering = false;
                    freezedOperations = new List<FreezedOperation>();
                    isThisServerFreezed = true;
                }
            }
             */
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
            /* 
            isRecovering = true;
            RecoverFreezedOperations();
            Monitor.PulseAll(this);
            isRecovering = false;
            isThisServerFreezed = false;
             */
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

        /*   Freeze old
        
                 /// <summary>
                /// Async method invoked by the clients when server is freezed
                /// </summary>
                /// <param name="operation"></param>
                /// <param name="TID"></param>
                /// <param name="UID"></param>
                /// <param name="value"></param>
                /// <returns></returns>
                public string ReadWriteWhenFreeze(string operation, long TID, int UID, int value)
                {
                    lock(this)
                    {
                        string result = string.Empty;
                        if (!isRecovering)
                        {
                            FreezedOperation freezedOp = new FreezedOperation();
                            freezedOp.Index = GetFreezeOperationIndex();
                            freezedOp.Operation = operation;
                            freezedOp.Tid = TID;
                            freezedOp.Uid = UID;
                            freezedOp.Value = value;
                            freezedOp.IsReleased = false;
                            freezedOperations.Add(freezedOp);
                            while (true)
                            {
                                if (freezedOperations.Exists(x => x.Index == freezedOp.Index && x.IsReleased))
                                {
                                    result = freezedOp.Result;
                                    break;
                                }
                                else
                                {
                                    Monitor.Wait(this);
                                }
                            }
                        }
                        else
                        {
                            result = "Server is recovering. Wait for a while";
                        }
                        return result;
                    }
                } 
  
  
  
               public void RecoverFreezedOperations()
                {
                    foreach (var operaion in freezedOperations)
                    {
                        Thread t;
                        if (operaion.Operation == Constants.OPERATION_READ)
                        {
                            t = new Thread(new ParameterizedThreadStart(ReleaseRead));
                            t.Start(operaion);

                        }
                        else if (operaion.Operation == Constants.OPERATION_WRITE)
                        {
                            t = new Thread(new ParameterizedThreadStart(ReleaseWrite));
                            t.Start(operaion);
                        }
                    }            
                }

                public void ReleaseWrite(Object operation)
                {
                    FreezedOperation op = (FreezedOperation)operation;
                    try
                    {
                        Write(op.Uid, op.Tid, op.Value);
                        op.Result = "Write value "+op.Value+" by TID = " + op.Tid;
                        op.IsReleased = true;
                    }
                    catch (Exception ex)
                    {
                        op.Result = "Error writing value by TID = " + op.Tid + "Error is " + ex.Message;
                        op.IsReleased = true;
                    }
                }

                public void ReleaseRead(Object operation)
                {
                    FreezedOperation op = (FreezedOperation)operation;
                    try
                    {                
                        int value = Read(op.Uid, op.Tid);
                        op.Result="Read the value = "+value+" by TID = "+op.Tid;
                        op.IsReleased = true;
                    }
                    catch (Exception ex)
                    {
                        op.Result = "Error reading value by TID = "+op.Tid +"Error is "+ex.Message;
                        op.IsReleased = true;
                    }

                }
                */


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
        /*
        private int GetFreezeOperationIndex()
        {
            lock(this)
            {
                return freezeOperationIndex++;
            }
        }
        */
        #endregion
    }
}
