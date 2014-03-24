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

        PadInt padint = new PadInt();
        PadInt tentativePadint = new PadInt();

        private List<PadInt> storedObjects = new List<PadInt>();
        private List<PadInt> tentativeVersions = new List<PadInt>();

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
        /// Still not clear how this should be done!
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Read(int uid, int value)
        {
            PadInt original = AccessPadInt(uid);
            if (original != null)
                if (TxtBegin(original, value))
                    return true; //the read can proceed
                else
                    return false; //the read can't proceed
            else
                return false; //Can't retrieve the object

        }

        public bool TxtBegin(PadInt original, int value)
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
