using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_LIBRARY
{
    
    public class ServerPadInt: MarshalByRefObject
    {
        
        public ServerPadInt(int uid,PADI_Worker worker)
        {
            this.uid = uid;
            this.WriteTS = 0;
            ReadTSList = new List<long>();
            TentativeList = new List<TentativePadInt>();
            this.worker = worker;
            IsCommited = false;
        }

        PADI_Worker worker;

        private int uid;

        public int Uid
        {
            get { return uid; }
            set { uid = value; }
        }

        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        private long writeTS;

        public long WriteTS
        {
            get { return writeTS; }
            set { writeTS = value; }
        }

        private bool isCommited;

        public bool IsCommited
        {
            get { return isCommited; }
            set { isCommited = value; }
        }


        private List<long> readTSList;

        public List<long> ReadTSList
        {
            get { return readTSList; }
            set { readTSList = value; }
        }

        private List<TentativePadInt> tentativeList;

        public List<TentativePadInt> TentativeList
        {
            get { return tentativeList; }
            set { tentativeList = value; }
        }


        public void Write(long TID, int value)
        {
            lock(this)
            {
                if ((ReadTSList.Count() == 0 || TID >= ReadTSList.Max()) && TID > WriteTS)
                {
                    if (TentativeList.Exists(x => x.WriteTS == TID))
                    {
                        TentativeList.First(x => x.WriteTS == TID).Value = value;
                    }
                    else
                    {
                        TentativePadInt tentative = new TentativePadInt(TID, value);
                        TentativeList.Add(tentative);
                        Console.WriteLine("Added to tentativelist - "+TID);
                    }
                }
                else
                {
                    Common.Logger().LogInfo("Write aborted TID=" + TID, string.Empty, string.Empty);
                    throw new Exception("Write aborted TID=" + TID);
                }
            }
        }



        public int Read(long TID)
        {
            lock (this)
            {
                int val=-1;
                while (true)
                {
                    if (WriteTS > 0 && TID > WriteTS)
                    {
                        if (TentativeList.Exists(x => x.WriteTS < TID))
                        {
                            Monitor.Wait(this);
                        }
                        else
                        {
                            val = Value;
                            readTSList.Add(TID);
                            break;
                        }
                    }
                }
                return val;
            }
        }

        public bool CanCommit(long TID)
        {
            lock (this)
            {                                    
                bool canCommit = false;
                if (TentativeList.Exists(x => x.WriteTS == TID))
                {
                    while (true)
                    {
                        if (TentativeList.Exists(x => x.WriteTS < TID))
                        {
                            Monitor.Wait(this);
                        }
                        else
                        {
                            canCommit = true;
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
        }

        public bool Commit(long TID)
        {
            lock (this)
            {
                bool isCommited = false;
                while (true)
                {
                    if (TentativeList.Exists(x => x.WriteTS == TID))
                    {
                        if (TentativeList.Exists(x => x.WriteTS < TID))
                        {
                            Console.WriteLine("Wait");
                            Monitor.Wait(this);
                        }
                        else
                        {
                            TentativePadInt temp = TentativeList.Find(x => x.WriteTS == TID);
                            this.Value = temp.Value;
                            this.WriteTS = temp.WriteTS;
                            this.IsCommited = true;
                            tentativeList.Remove(temp);
                            Monitor.PulseAll(this);
                            isCommited = true;                            
                            Console.WriteLine("commited");
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("TID is not in this object to commit");
                        isCommited = true;
                        break;
                    }
                }
                return isCommited;
            }
        }

    }
}
