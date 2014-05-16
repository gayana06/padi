#region Directive Section

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace PADI_LIBRARY
{
    public class PADI_Master : MarshalByRefObject
    {
        #region Initialization

        private int serverIndex = 0;
        private bool hasNotification = false;
        private const string PREFIX_WORKER_SERVER = "W_SERVER_";
        private List<ObjectServer> workerServerList;
        private Dictionary<string, DateTime> objectServerHeartBeatTimeStamp;
        private long latestWorkerServerViewTimeStamp = 0;
        private bool groupMembershipChange = false;

        public long LatestWorkerServerViewTimeStamp
        {
            get { return latestWorkerServerViewTimeStamp; }
            set { latestWorkerServerViewTimeStamp = value; }
        }

        public List<ObjectServer> WorkerServerList
        {
            get { return workerServerList; }
            set { workerServerList = value; }
        }  

        public Dictionary<string, DateTime> ObjectServerHeartBeatTimeStamp
        {
            get { return objectServerHeartBeatTimeStamp; }
            set { objectServerHeartBeatTimeStamp = value; }
        }

        public bool ViewUpdating
        {
            get { return hasNotification; }
            set { hasNotification = value; }
        }

        public PADI_Master()
        {
            workerServerList = new List<ObjectServer>();
            objectServerHeartBeatTimeStamp = new Dictionary<string, DateTime>();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// Create a WorkerServer and update details.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns>unique server name</returns>
        public ObjectServer Bootstrap(string ip, string port)
        {
            lock (this)
            {
                ObjectServer wserver = new ObjectServer();
                wserver.ServerName = PREFIX_WORKER_SERVER + (++serverIndex);
                wserver.ServerIp = ip;
                wserver.ServerPort = port;
                wserver.TcpUrl = Common.GenerateTcpUrl(ip, port, Constants.OBJECT_TYPE_PADI_WORKER);
                wserver.ServerIndex = serverIndex;
                //TODO:  set replicaServerName : This should be calculated by a function which will consider the position of the record in the workerServerList

                workerServerList.Add(wserver);
                hasNotification = true;
                LatestWorkerServerViewTimeStamp = DateTime.Now.Ticks;
                Monitor.PulseAll(this);
                return wserver;
            }
        }

        public void UpdateReplicaServerNames()
        {
            for (int i = 0; i < WorkerServerList.Count; i++)
            {
                WorkerServerList[i].ServerIndex = ++i;
            }
            for (int j = 0; j < WorkerServerList.Count; j++)
            {
                if (workerServerList[j].ServerIndex == workerServerList.Count)
                    workerServerList[j].ReplicaServerName = workerServerList[0].ServerName;
                else
                    WorkerServerList[j].ReplicaServerName = WorkerServerList[j + 1].ServerName;
            }
        }
        
        /// <summary>
        /// When object servers ping periodically, this method is called
        /// </summary>
        /// <param name="serverName"></param>
        public void HeartBeatReceiver(string serverName)
        {
            lock (this)
            {
                ObjectServer server = Common.GetObjectServerByName(serverName, workerServerList);
                if (server != null)
                {
                    if (objectServerHeartBeatTimeStamp.ContainsKey(serverName))
                        objectServerHeartBeatTimeStamp[serverName] = DateTime.Now;
                    else
                        objectServerHeartBeatTimeStamp.Add(serverName, DateTime.Now);
                    Console.WriteLine("Heartbeat Received, Server :" + server.ServerName + " Ip :" + server.ServerIp + " Port :" + server.ServerPort);
                    Common.Logger().LogInfo("Heartbeat Received, Server :" + server.ServerName + " Ip :" + server.ServerIp + " Port :" + server.ServerPort, string.Empty, string.Empty);
                }
                else
                {
                    Console.WriteLine("No server available with name " + serverName + " in master node");
                    Common.Logger().LogError("No server available with name " + serverName + " in master node", string.Empty, string.Empty);
                }
            }
        }
       
        /// <summary>
        /// Ask object servers to dump the status to their consoles
        /// </summary>
        public void DumpObjectServerStatus()
        {
            PADI_Worker worker;
            foreach (var server in WorkerServerList)
            {
                try
                {
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(server.ServerIp, server.ServerPort, Constants.OBJECT_TYPE_PADI_WORKER));
                    worker.DumpStatus();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Worker server has left but not yet detected by the master. "+ex.Message);
                }
            }
        }
        
        /// <summary>
        /// Handling group membership updates (aka view synchrony)
        /// If any new server arrived this method sequentially notifies the object servers.
        /// </summary>
        public void ViewChangeHandler()
        {
            lock (this)
            {
                while (true)
                {
                    if (ViewUpdating)
                    {
                        PADI_Worker worker;
                        UpdateReplicaServerNames();
                            foreach (var server in WorkerServerList)
                            {
                                try
                                {
                                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), 
                                        Common.GenerateTcpUrl(server.ServerIp, server.ServerPort, 
                                        Constants.OBJECT_TYPE_PADI_WORKER));
                                    worker.UpdateServerList(WorkerServerList.ToArray());
                                    worker.PrintReplicaServerName(WorkerServerList.ToArray());
                                }
                                catch (Exception ex)
                                {
                                    //TODO: implement a retry mechanism if failed later if required. 
                                    Console.WriteLine(ex.Message);
                                    Common.Logger().LogError(ex.Message, "NotifyObjectServer() in PADI_MASTER", string.Empty);
                                }
                            }
                        ViewUpdating = false;
                    }
                    else
                    {
                        Monitor.Wait(this);
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
            lock (this)
            {
                string failedServer = string.Empty;
                foreach (var timeStamp in ObjectServerHeartBeatTimeStamp)
                {
                    if ((DateTime.Now.Subtract(timeStamp.Value).Seconds) * 1000 > int.Parse(ConfigurationManager.AppSettings[Constants.APPSET_OBJ_SERVER_FAIL_TIME]))
                    {
                        //TODO: failure detected what to do now
                        failedServer = timeStamp.Key;
                        Console.WriteLine("Failure detected server :" + timeStamp.Key);
                        Common.Logger().LogInfo("Failure detected server :" + timeStamp.Key, string.Empty, string.Empty);
                        break;
                    }
                }
                if (!String.IsNullOrEmpty(failedServer))
                {
                    ObjectServerHeartBeatTimeStamp.Remove(failedServer);
                    workerServerList.Remove(Common.GetObjectServerByName(failedServer,workerServerList));
                }
            }
        }

        public bool getViewStatus()
        {
            return ViewUpdating;
        }

        #endregion
    }
}
