using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Configuration;
using PADI_LIBRARY;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Collections;

namespace PADI_OBJECT_SERVER
{
    class WorkerServer
    {
        TcpChannel workerChannel;
        PADI_Worker worker;
        ObjectServer thisServer;

        public WorkerServer()
        {
            StartWorkerServer();
        }

        public void StartWorkerServer()
        {
            System.Threading.Timer timer = null;
            try
            {

                string workerPort = ConfigurationManager.AppSettings[Constants.APPSET_WORKER_PORT];
                BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
                provider.TypeFilterLevel = TypeFilterLevel.Full;
                IDictionary props = new Hashtable();
                props[Constants.STR_PORT] = Int16.Parse(workerPort);
                workerChannel = new TcpChannel(props,null,provider);
                worker = new PADI_Worker();
                ChannelServices.RegisterChannel(workerChannel, false);
                RemotingServices.Marshal(worker, Constants.OBJECT_TYPE_PADI_WORKER, typeof(PADI_Worker));
                bool isBootStraped = BootstrapMaster(workerPort);
                timer = new System.Threading.Timer(SendHeartBeatMessage, null, long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_HEARTBEAT_PERIOD]), long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_HEARTBEAT_PERIOD]));
                Console.WriteLine("Worker server :" + thisServer.ServerName + "started. Bootstrap status:"+isBootStraped);
                Common.Logger().LogInfo("Worker server :" + thisServer.ServerName + " started", "Port : " + workerPort, "Bootstrap status:"+isBootStraped);
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker startup failed.." + ex.Message);
                Common.Logger().LogError("Worker startup failed..", ex.Message, ex.Source);
            }
            finally
            {
                if (timer != null)
                    timer.Dispose();
            }

        }
        
        public bool BootstrapMaster(string workerPort)
        {
            bool isBootstraped = false;
            String masterUrl = Common.GetMasterTcpUrl();
            String workerIp = Common.GetLocalIPAddress();
            PADI_Master masterObj = (PADI_Master)Activator.GetObject(typeof(PADI_Master), masterUrl);
            thisServer=masterObj.Bootstrap(workerIp, workerPort);
            if (thisServer!=null)
            {
                isBootstraped = true;
            }
            return isBootstraped;
        }

        public void SendHeartBeatMessage(object state)
        {
            PADI_Master master = (PADI_Master)Activator.GetObject(typeof(PADI_Master), Common.GetMasterTcpUrl());
            master.HeartBeatReceiver(thisServer.ServerName);
        }


    }
}
