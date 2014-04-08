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
            client = new PADI_Client();
            client.Init();
        }

        public void Transaction2()
        {
            try
            {
                client.TxBegin();
                PadInt padInt = client.AccessPadInt(1);
                padInt.Write(102);
                padInt.Read();
                padInt = client.AccessPadInt(2);
                padInt.Write(202);
                padInt.Read();
                padInt = client.AccessPadInt(3);
                padInt.Write(302);
                padInt.Read();
                client.TxCommit();
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
                client.TxBegin();
                PadInt padInt = client.AccessPadInt(1);
                padInt.Write(103);
                padInt = client.AccessPadInt(2);
                padInt.Write(203);
                padInt = client.AccessPadInt(3);
                padInt.Write(303);
                client.TxCommit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
