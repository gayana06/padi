using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Configuration;
using PADI_LIBRARY;
using System.Runtime.Remoting;

namespace PADI_OBJECT_SERVER
{
    class WorkerServer
    {
        TcpChannel workerChannel;
        PADI_Worker worker;
        String serverName;

        public WorkerServer()
        {
            StartWorkerServer();
        }

        public void StartWorkerServer()
        {
            try
            {
                string workerPort = ConfigurationManager.AppSettings[Constants.APPSET_WORKER_PORT];
                bool isBootStraped=BootstrapMaster(workerPort);
                if (isBootStraped)
                {                    
                    workerChannel = new TcpChannel(Int16.Parse(workerPort));
                    worker = new PADI_Worker();
                    ChannelServices.RegisterChannel(workerChannel, false);
                    RemotingServices.Marshal(worker, Constants.OBJECT_TYPE_PADI_WORKER, typeof(PADI_Worker));

                    Console.WriteLine("Worker server :" + serverName + "started");
                    Common.Logger().LogInfo("Worker server :" + serverName + "started", "Port : " + workerPort, string.Empty);
                    Console.ReadLine();
                }
                else
                    throw new Exception("Bootstrap failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker startup failed.."+ex.Message);
                Common.Logger().LogError("Worker startup failed..",ex.Message,ex.Source);
            }
        }
        
        public bool BootstrapMaster(string workerPort)
        {
            bool isBootstraped = false;
            String masterUrl = Common.GetMasterTcpUrl();
            String workerIp = Common.GetLocalIPAddress();
            PADI_Master masterObj = (PADI_Master)Activator.GetObject(typeof(PADI_Master), masterUrl);
            string serverName= masterObj.Bootstrap(workerIp, workerPort);
            if (!String.IsNullOrEmpty(serverName))
            {
                this.serverName = serverName;
                isBootstraped = true;
            }
            return isBootstraped;
        }
    }
}
