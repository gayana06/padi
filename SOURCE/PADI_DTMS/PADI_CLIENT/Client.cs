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
        PADI_Coordinator coordinator;
        PADI_Master master;
        Information info;
        public Client()
        {
            //TODO:Load the Info object when start and save it when client exit. Currently new object is created.
            info = new Information();
            StartClientTransactions();
        }

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
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Get the TID from coordinator and load server map if the available map is expired
        /// </summary>
        /// <returns></returns>
        public long BeginTxn()
        {
            coordinator=(PADI_Coordinator)Activator.GetObject(typeof(PADI_Coordinator),Common.GenerateTcpUrl(ConfigurationManager.AppSettings[Constants.APPSET_MASTER_IP],ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT],Constants.OBJECT_TYPE_PADI_COORDINATOR));
            string tidReply = coordinator.BeginTxn();
            Console.WriteLine("Coordinator reply : "+tidReply);
            string[] tempSep = tidReply.Split(Constants.SEP_CHAR_COLON);
            long receivedTimeStamp =long.Parse(tempSep[1]);
            if (receivedTimeStamp != info.AvailableMasterMapTimeStamp)
            {
                info.AvailableMasterMapTimeStamp = receivedTimeStamp;
                LoadServerMap();
            }
            return long.Parse(tempSep[0]);
        }

        public void LoadServerMap()
        {            
            master = (PADI_Master)Activator.GetObject(typeof(PADI_Master), Common.GetMasterTcpUrl());
            info.ObjectServerMap = master.WorkerServerList.ToArray();
            Console.WriteLine("Loaded the new map, size="+info.ObjectServerMap.Count());
        }


    }

    [Serializable]
    class Information
    {
        private long availableMasterMapTimeStamp;

        public long AvailableMasterMapTimeStamp
        {
            get { return availableMasterMapTimeStamp; }
            set { availableMasterMapTimeStamp = value; }
        }

        private ObjectServer[] objectServerMap;

        public ObjectServer[] ObjectServerMap
        {
            get { return objectServerMap; }
            set { objectServerMap = value; }
        }


    }
}
