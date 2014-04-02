
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{

    public class PADI_Worker : MarshalByRefObject
    {
        private ObjectServer[] objectServerList;

        private Dictionary<int,ServerPadInt> padIntActiveList;
       // private Dictionary<int, ServerPadInt> padIntInactiveList;
        
        public PADI_Worker()
        {
            padIntActiveList = new Dictionary<int,ServerPadInt>();
           
        }

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void ReceiveObjectServerList(ObjectServer[] objectServerList)
        {
            this.objectServerList=objectServerList;
            Console.WriteLine("New Object Server List received");
            Common.Logger().LogInfo("New Object Server List received",string.Empty,string.Empty);
        }

        public ServerPadInt CreatePadInt(int uid)
        {
            ServerPadInt newPadInt = null;
            if (!padIntActiveList.ContainsKey(uid))
            {
                newPadInt = new ServerPadInt(uid,this);
                padIntActiveList.Add(uid, newPadInt);
            }
            return newPadInt;
        }

        public ServerPadInt AccessPadInt(int uid)
        {
            ServerPadInt padInt=null;
            if (padIntActiveList.ContainsKey(uid))
                padInt = padIntActiveList[uid];
            return padInt;
        }

   /*     public void MoveInactiveToActive(int uid)
        {
            lock (this)
            {
                if (padIntInactiveList.ContainsKey(uid))
                {
                    padIntActiveList.Add(uid,padIntInactiveList[uid]);
                    padIntInactiveList.Remove(uid);
                }
            }
        }*/

        public bool CanCommit(long TID)
        {
            lock (this)
            {
                bool canCommit = false;
                List<int> uidsToCommit = GetUidsToBeCommited(TID);
                foreach (var uid in uidsToCommit)
                {
                    canCommit = AccessPadInt(uid).CanCommit(TID);
                    if (!canCommit)
                    {
                        break;
                    }
                }
                return canCommit;
            }
        }

        public bool DoCommit(long TID)
        {
            lock (this)
            {
                bool isCommited = false;
                List<int> uidsToCommit = GetUidsToBeCommited(TID);
                foreach (var uid in uidsToCommit)
                {
                    isCommited = AccessPadInt(uid).Commit(TID);
                    if (!isCommited)
                    {
                        break;
                    }
                }

                //TODO: abort the previously completed commits if any
                return isCommited;
            }
        }

        private List<int> GetUidsToBeCommited(long TID)
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

        public void DumpStatus()
        {
            lock (this)
            {
                Console.WriteLine("Active list");
                foreach (var val in padIntActiveList)
                {
                    Console.WriteLine("Uid = " + val.Key);
                    Console.WriteLine("Value = " + val.Value.Value);
                    Console.WriteLine("Commited = "+val.Value.IsCommited);
                    foreach (var tentative in val.Value.TentativeList)
                    {
                        Console.WriteLine("Tentative TID = " + tentative.WriteTS + " Value = " + tentative.Value);
                    }
                }
            }
        }

    }


}

/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PADI_LIBRARY.UTIL_CLASSES;

namespace PADI_LIBRARY
{
    public class PADI_Worker : MarshalByRefObject
    {
        private ObjectServer[] objectServerList;

        ServerPadInt padint ;
        ServerPadInt tentativePadint;

        private List<ServerPadInt> storedObjects = new List<ServerPadInt>();
        private List<ServerPadInt> tentativeVersions = new List<ServerPadInt>();

        DateTime timestamp;
        DateTime oldTimestamp;
        DateTime currentTimestamp;
        private static long lastTime;

        private static object timeLock = new object();
        private static object createLock = new object();

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void ReceiveObjectServerList(ObjectServer[] objectServerList)
        {
            this.objectServerList=objectServerList;
            Console.WriteLine("New Object Server List received");
            Common.Logger().LogInfo("New Object Server List received",string.Empty,string.Empty);
        }

        /// <summary>
        /// Creates a new shared object with the given uid
        /// </summary>
        /// <returns>false or true</returns>
        public bool CreatePadInt(int uid, int value)
        {
            lock (createLock) //prevent the same object to be created at the same time
            {
                if (storedObjects.Exists(x => x.Uid == uid))
                    return false;
                else
                {
                    timestamp = GetCurrentTime();
                    padint = new ServerPadInt(uid, value);
                    padint.Timestamp = timestamp;

                    storedObjects.Add(padint);
                   // allObjects.Add(String.Format("{0}: {1}", uid, value));
                    return true;
                }
            }

            //TO DO - Handle replication 
        }

        /// <summary>
        /// Returns a reference to the shared object
        /// <param name="uid"></param>
        /// </summary>
        /// <returns>value or 0</returns>
        public ServerPadInt AccessPadInt(int uid)
        {
            if (storedObjects.Exists(x => x.Uid == uid))
                return storedObjects.Find(x => x.Uid == uid);
            else
                return null;
        }

        /// this should be moved to the coordinator or the master 
        /// <summary>
        /// Generates a unique timestamps for each transaction
        /// </summary>
        /// <returns>unique timestamp value</returns>
        public DateTime GetCurrentTime()
        {
            lock (timeLock) // prevent concurrent access to ensure uniqueness
            {
                DateTime result = DateTime.UtcNow;
                if (result.Ticks <= lastTime)
                    result = new DateTime(lastTime + 1);
                lastTime = result.Ticks;
                return result;
            }
        }

        /// <summary>
        /// Still not clear how this should be done!
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Read(int uid, int value)
        {
            ServerPadInt original = AccessPadInt(uid);
            if (original != null)
                if (TxtBegin(original, value))
                    return true; //the read can proceed
                else
                    return false; //the read can't proceed
            else
                return false; //Can't retrieve the object

        }

        public bool TxtBegin(ServerPadInt original, int value)
        {
            currentTimestamp = GetCurrentTime();
            oldTimestamp = original.Timestamp;
            if (currentTimestamp > oldTimestamp)
            {
                tentativePadint.Uid = original.Uid;
                tentativePadint.Timestamp = currentTimestamp;
                tentativePadint.Value = value;
                tentativeVersions.Add(tentativePadint);
                return true;
            }
            else
                return false; //A read has already occurred
        }

    }
}
 * 
 * */

/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PADI_LIBRARY;

namespace PADI_LIBRARY
{
    public class PADI_Worker : MarshalByRefObject
    {
        private ObjectServer[] objectServerList;

        ServerPadInt padint;
        ServerPadInt tentativePadint;

        private List<ServerPadInt> storedObjects = new List<ServerPadInt>();
        private List<ServerPadInt> tentativeVersions = new List<ServerPadInt>();

        DateTime timestamp;
        DateTime rts; // Read timestamp
        DateTime wts; // write timestamp
        DateTime cts; // current timestamp

        Dictionary<int, List<DateTime>> readDic = new Dictionary<int, List<DateTime>>();
        Dictionary<int, List<DateTime>> writeDic = new Dictionary<int, List<DateTime>>();

        Dictionary<int, List<DateTime>> tentativeReads = new Dictionary<int, List<DateTime>>();
        Dictionary<int, List<DateTime>> tentativeWrites = new Dictionary<int, List<DateTime>>();

        private static long lastTime;
        private static object timeLock = new object();
        private static object createLock = new object();

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void ReceiveObjectServerList(ObjectServer[] objectServerList)
        {
            this.objectServerList = objectServerList;
            Console.WriteLine("New Object Server List received");
            Common.Logger().LogInfo("New Object Server List received", string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a new shared object with the given uid
        /// </summary>
        /// <returns>false or true</returns>
        public bool CreatePadInt(int uid, int value)
        {
            lock (createLock) //prevent the same object to be created at the same time
            {
                if (storedObjects.Exists(x => x.Uid == uid))
                    return false;
                else
                {
                    timestamp = GetCurrentTime();
                    padint = new ServerPadInt();
                    padint.Uid = uid;
                    padint.Value = value;
                    padint.Timestamp = timestamp;

                    storedObjects.Add(padint);
                    // allObjects.Add(String.Format("{0}: {1}", uid, value));
                    return true;
                }
            }

            //TO DO - Handle replication 
        }

        /// <summary>
        /// Returns a reference to the shared object
        /// <param name="uid"></param>
        /// </summary>
        /// <returns>value or 0</returns>
        public ServerPadInt AccessPadInt(int uid)
        {
            if (storedObjects.Exists(x => x.Uid == uid))
                return storedObjects.Find(x => x.Uid == uid);
            else
                return null;
        }

        /// this should be moved to the coordinator or the master 
        /// <summary>
        /// Generates a unique timestamps for each transaction
        /// </summary>
        /// <returns>unique timestamp value</returns>
        public DateTime GetCurrentTime()
        {
            lock (timeLock) // prevent concurrent access to ensure uniqueness
            {
                DateTime result = DateTime.UtcNow;
                if (result.Ticks <= lastTime)
                    result = new DateTime(lastTime + 1);
                lastTime = result.Ticks;
                return result;
            }
        }

        /// <summary>
        /// Handles the read operations
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool Read(int uid)
        {
            ServerPadInt original = AccessPadInt(uid);
            if (original != null)
            {
                cts = GetCurrentTime();

                if (writeDic.ContainsKey(uid))
                {
                    wts = writeDic[uid].Max(); //Get the latest write of the committed object
                    if (wts > cts)
                        return false; //abort this transaction, a write with a greater timestamp has taken place
                    else
                    {
                        tentativeReads[uid].Add(cts); //record the tentative value
                        return true; //allow this transaction to continue
                    }
                }
                else if (readDic.ContainsKey(uid) && !writeDic.ContainsKey(uid))
                {
                    tentativeReads[uid].Add(cts);
                    return true; //transaction continues as long as the no write has taken place
                }
                else
                {
                    //this happens only during the object creation when no read or write has taken place
                    tentativeReads[uid].Add(cts);
                    return true;
                }
            }
            else
                return false; //object not found
        }

        /// <summary>
        /// Handles a write operation
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool Write(int uid)
        {
            ServerPadInt original = AccessPadInt(uid);
            if (original != null)
            {
                cts = GetCurrentTime();

                if (writeDic.ContainsKey(uid))
                {
                    wts = writeDic[uid].Max(); //latest write
                    if (wts > cts)
                        return false; //a write with larger timestamp has taken place
                    else
                    {
                        if (readDic.ContainsKey(uid))
                        {
                            rts = readDic[uid].Max();
                            if (cts >= rts)
                            {
                                tentativeWrites[uid].Add(cts);
                                return true; //a read with a larger timestamp has taken place
                            }
                            else
                                return false;//a read with a larger timesatamp has taken place
                        }
                        else
                            return true;
                    }
                }
                else
                {
                    //this happens only during the object creation when no read or write has taken place
                    tentativeWrites[uid].Add(cts);
                    return true;
                }
            }
            else
                return false; //object not found
        }

    }
}
 * */

