using PADI_LIBRARY;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;

namespace PADI_MASTER_SERVER
{
    class MasterServer
    {
        TcpChannel masterChannel;
        PADI_Master master;
        PADI_Coordinator coordinator;

        public MasterServer()
        {
            StartMasterServer();
        }

        public void StartMasterServer()
        {
            System.Threading.Timer failDetectorTimer = null;
            try
            {
                string masterPort = ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT];
                BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
                provider.TypeFilterLevel = TypeFilterLevel.Full;
                IDictionary props = new Hashtable();
                props[Constants.STR_PORT] = Int16.Parse(masterPort);
                masterChannel = new TcpChannel(props, null, provider);
                master = new PADI_Master();
                coordinator = new PADI_Coordinator(master);
                ChannelServices.RegisterChannel(masterChannel, false);
                RemotingServices.Marshal(master, Constants.OBJECT_TYPE_PADI_MASTER, typeof(PADI_Master));
                RemotingServices.Marshal(coordinator,Constants.OBJECT_TYPE_PADI_COORDINATOR,typeof(PADI_Coordinator));
                Thread notificationThread = new Thread(new ThreadStart(NotifyObjectServer));
                notificationThread.Start();
                failDetectorTimer = new System.Threading.Timer(DetectObjectServerFailure, null, long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_OBJ_SERVER_FAIL_DECTOR_FREQUENCY]), long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_OBJ_SERVER_FAIL_DECTOR_FREQUENCY]));

                Console.WriteLine("Master server started at port : " + masterPort);
                Common.Logger().LogInfo("Worker server started", "Port : " + masterPort, string.Empty);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Master startup failed.." + ex.Message);
                Common.Logger().LogError("Master startup failed..", ex.Message, ex.Source);
            }
            finally
            {
                if (failDetectorTimer != null)
                    failDetectorTimer.Dispose();
            }
        }

        /// <summary>
        /// If any new server arrived this method should notify all the object servers.
        /// </summary>
        public void NotifyObjectServer()
        {
            lock (master)
            {
                while (true)
                {
                    if (master.HasNotification)
                    {
                        PADI_Worker worker;
                        foreach (var server in master.WorkerServerList)
                        {
                            try
                            {
                                worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(server.ServerIp, server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                                worker.ReceiveObjectServerList(master.WorkerServerList.ToArray());
                            }
                            catch (Exception ex)
                            {
                                //TODO: implement a retry mechanism if failed later if required. 
                                Console.WriteLine(ex.Message);
                                Common.Logger().LogError(ex.Message, "NotifyObjectServer() in PADI_MASTER", string.Empty);
                            }
                        }
                        master.HasNotification = false;
                    }
                    else
                    {
                        Monitor.Wait(master);
                    }
                }
            }
        }



        /// <summary>
        /// Detect a object server failure
        /// </summary>
        /// <param name="state"></param>
        public void DetectObjectServerFailure(object state)
        {
            lock (master)
            {
                string failedServer=string.Empty;
                foreach (var timeStamp in master.ObjectServerHeartBeatTimeStamp)
                {                    
                    if ((DateTime.Now.Subtract(timeStamp.Value).Seconds)*1000 > int.Parse(ConfigurationManager.AppSettings[Constants.APPSET_OBJ_SERVER_FAIL_TIME]))
                    {
                        //TODO: failure detected what to do now
                        failedServer = timeStamp.Key;
                        Console.WriteLine("Failure detected server :"+timeStamp.Key);
                        Common.Logger().LogInfo("Failure detected server :" + timeStamp.Key, string.Empty, string.Empty);
                        break;
                    }
                }
                if(!String.IsNullOrEmpty(failedServer))
                    master.ObjectServerHeartBeatTimeStamp.Remove(failedServer);
            }
        }


    }
}
