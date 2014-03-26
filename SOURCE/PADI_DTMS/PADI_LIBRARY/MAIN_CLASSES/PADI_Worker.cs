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

        PadInt padint;
        PadInt tentativePadint;

        private List<PadInt> storedObjects = new List<PadInt>();
        private List<PadInt> tentativeVersions = new List<PadInt>();

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
                    padint = new PadInt();
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
        public PadInt AccessPadInt(int uid)
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
            PadInt original = AccessPadInt(uid);
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
            PadInt original = AccessPadInt(uid);
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
