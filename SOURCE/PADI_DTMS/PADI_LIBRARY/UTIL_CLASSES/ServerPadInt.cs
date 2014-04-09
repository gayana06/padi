#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace PADI_LIBRARY
{

    public class ServerPadInt : MarshalByRefObject, ICloneable
    {
        #region Initialization

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

        #endregion

        #region Public Members
        
        /// <summary>
        /// Clone the ServerPadInt
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Do write on the object itself
        /// </summary>
        /// <param name="TID"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Write(long TID, int value)
        {
            lock(this)
            {
                bool isWriteSuccessful = false;
                Console.WriteLine("\n---------------Write (START, TID="+TID+")-------------------");
                Console.WriteLine("ReadTSList count = " + ReadTSList.Count());
                Console.WriteLine("WriteTS = " + WriteTS);
                Console.WriteLine("TID = "+TID);
                Console.WriteLine("UID = "+this.Uid);
                Console.WriteLine("Committed Version Value = "+this.Value);
                if ((ReadTSList.Count() == 0 || TID >= ReadTSList.Max()) && TID > WriteTS)
                {
                    if (TentativeList.Exists(x => x.WriteTS == TID))
                    {
                        TentativeList.First(x => x.WriteTS == TID).Value = value;
                        Console.WriteLine("Already in tentativelist, value updated TID = " + TID + ", Value = " + value);
                    }
                    else
                    {
                        TentativePadInt tentative = new TentativePadInt(TID, value);
                        TentativeList.Add(tentative);
                        Console.WriteLine("Added to tentativelist TID = "+TID+", Value = "+value);
                    }
                    isWriteSuccessful = true;
                }
                else
                {                    
                    TxAbort(TID);
                    Console.WriteLine("Transaction aborts. TID = "+TID);
                    Common.Logger().LogInfo("Write aborted TID=" + TID, string.Empty, string.Empty);                                      
                }
                Console.WriteLine("---------------Write (END, TID=" + TID + ")-------------------\n");
                return isWriteSuccessful;
            }
            
        }
        
        /// <summary>
        /// Do read the object itself
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public int Read(long TID)
        {
            lock (this)
            {
                Console.WriteLine("\n---------------Read (START, TID=" + TID + ")-------------------");           
                int val=-1;
                Console.WriteLine("TID = "+TID);
                Console.WriteLine("WriteTS = " + WriteTS);
                Console.WriteLine("UID = " + this.Uid);
                Console.WriteLine("Committed Version Value = " + this.Value);
                while (true)
                {
                    if (WriteTS > 0 && TID > WriteTS)
                    {
                        if (TentativeList.Exists(x => x.WriteTS < TID))
                        {
                            Console.WriteLine("Read operation waits. TID : " + TID);
                            Monitor.Wait(this);
                        }
                        else
                        {
                            val = Value;
                            Console.WriteLine("Read Value = " + val + ", TID = " + TID);
                            readTSList.Add(TID);
                            break;
                        }
                    }
                    else
                    {
                        throw new TxException("No committed version exists to read. UID = "+this.Uid+" ,TID = "+TID);                        
                    }
                }
                Console.WriteLine("---------------Read (END, TID=" + TID + ")-------------------\n");           
                return val;
            }
        }

        /// <summary>
        /// Remove tentative records from the object
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool TxAbort(long TID)
        {
            lock (this)
            {
                bool isAbort = false;
                if (TentativeList.Exists(x => x.WriteTS == TID))
                {
                    TentativeList.Remove(TentativeList.Find(x => x.WriteTS == TID));
                    Monitor.PulseAll(this);
                    isAbort = true;
                }
                else
                    isAbort = true;
                return isAbort;
            }
        }

        /// <summary>
        /// Check the commit is possible for a given transaction id
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
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
                            Console.WriteLine("Waiting at cancommit TID = "+TID+ " ,UID="+this.Uid);
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

        /// <summary>
        /// do commit on the object itself
        /// </summary>
        /// <param name="TID"></param>
        /// <returns></returns>
        public bool Commit(long TID)
        {
            lock (this)
            {
                bool isCommited = false;
                Console.WriteLine("\n---------------Commit (START, TID=" + TID + " ,UID=" + this.Uid + ")-------------------");        
                while (true)
                {
                    if (TentativeList.Exists(x => x.WriteTS == TID))
                    {
                        if (TentativeList.Exists(x => x.WriteTS < TID))
                        {
                            Console.WriteLine("Wait TID = "+TID);
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
                            Console.WriteLine("commited TID : "+TID+", Value = "+Value);
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
                Console.WriteLine("---------------Commited (End, TID=" + TID + ")-------------------\n");        
                return isCommited;
            }
        }

        #endregion
    }
}
