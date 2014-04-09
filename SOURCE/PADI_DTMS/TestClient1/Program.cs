using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestClient1
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p=new Program();
            for(int i=0;i<2;i++)
            {
                Console.WriteLine(i);
                Thread.Sleep(1000);
            }
            Thread t = new Thread(new ThreadStart(p.Transaction2));
            t.Start();
            Thread.Sleep(2000);
            p.Transaction3();
            Console.ReadLine();
        }

        PADI_Client client;
        public Program()
        {
            //client = new PADI_Client();
            PADI_Client.Init();
        }

        public void Transaction2()
        {
            try
            {
                PADI_Client.TxBegin();
                PadInt padInt = PADI_Client.AccessPadInt(1);
                padInt.Write(102);
                padInt.Read();
                padInt = PADI_Client.AccessPadInt(2);
                padInt.Write(202);
                padInt.Read();
                padInt = PADI_Client.AccessPadInt(3);
                padInt.Write(302);
                padInt.Read();
                PADI_Client.TxCommit();
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
            }
           catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "--" + ex.StackTrace);
            }
        }

        public void Transaction3()
        {
            try
            {
                PADI_Client.TxBegin();
                PadInt padInt = PADI_Client.AccessPadInt(1);
                padInt.Write(103);
                padInt = PADI_Client.AccessPadInt(2);
                padInt.Write(203);
                padInt = PADI_Client.AccessPadInt(3);
                padInt.Write(303);
                PADI_Client.TxCommit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
