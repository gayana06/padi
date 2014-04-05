using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_CLIENT
{
    class Client
    {

        public void Start()
        {
            PADI_Client client = new PADI_Client();
            bool isTransactionCreated=client.BeginTxn();
            PadInt padInt = client.CreatePadInt(1);
           // PadInt padInt = client.AccessPadInt(1);
            padInt.Write(10);
            bool isCommited=client.TxCommit();
            Console.ReadLine();
           // client.Status();
        }



/*
        public void StartClientTransactions()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    long tid = BeginTxn();
                    Console.WriteLine("Trancation ID received:" + tid);
                    Thread.Sleep(7000);
                } 
                Thread t1 = new Thread(new ThreadStart(TransactionA));
                t1.Start();
                Thread.Sleep(2000);
                Thread t2 = new Thread(new ThreadStart(TransactionB));
                t2.Start();
                Thread.Sleep(2000);
                master.DumpObjectServerStatus();

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void TransactionA()
        {
            long tid = BeginTxn();
            ServerPadInt p = CreatePadInt(1);
            p.Write(tid, 10);
            //Thread.Sleep(2000);
            ServerPadInt pp = CreatePadInt(2);
            pp.Write(tid, 20);            
            testcommit(tid);
           // master.DumpObjectServerStatus();

            Thread.Sleep(10000);
            tid = BeginTxn();
            p = AccessPadInt(1);
            Console.WriteLine("Read val for uid1 in A " +p.Read(tid));
        }
        public void TransactionB()
        {
            long tid = BeginTxn();
            ServerPadInt p = AccessPadInt(1);
            p.Write(tid, 100);
           // Thread.Sleep(2000);
            ServerPadInt pp = AccessPadInt(2);
            pp.Write(tid, 200);
            Console.WriteLine("Read val for uid1 in B" + p.Read(tid));
            testcommit(tid);
           // master.DumpObjectServerStatus();
        }

        public bool testcommit(long TID)
        {
            lock (this)
            {
                bool b = false;
                  foreach (var worker in workers)
                  {
                      b = worker.CanCommit(TID);

                  }
                foreach (var worker in workers)
                {
                    b = worker.DoCommit(TID);

                }
                return b;
            }
        }
        */
        

    }

}
