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
        PADI_Client client;
        public Client()
        {
            client = new PADI_Client();
            client.Init();
        }

        public void Start()
        {
            try
            {
                client.BeginTxn();
                PadInt padInt = client.CreatePadInt(1);
                padInt.Write(10);
                padInt = client.CreatePadInt(2);
                padInt.Write(20);
                padInt = client.CreatePadInt(3);
                padInt.Write(30);
                client.TxCommit();
                Thread.Sleep(2000);
                Transaction1();
                Console.ReadLine();
                client.Status();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Transaction1()
        {
            try
            {
                client.BeginTxn();
                PadInt padInt = client.AccessPadInt(1);                
                padInt.Write(101);
                padInt = client.AccessPadInt(2);
                padInt.Write(201);
                padInt = client.AccessPadInt(3);
                padInt.Write(301);
                client.TxCommit();                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }         
        }


    }

}
