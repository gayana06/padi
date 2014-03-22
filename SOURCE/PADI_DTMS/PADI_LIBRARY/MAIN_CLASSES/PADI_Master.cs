using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_LIBRARY
{
    public class PADI_Master : MarshalByRefObject
    {
        private int serverIndex = 0;
        private bool hasNotification = false;
        private const string PREFIX_WORKER_SERVER = "W_SERVER_";
        private List<ObjectServer> workerServerList;
        private Dictionary<string, DateTime> objectServerHeartBeatTimeStamp;
        private long latestWorkerServerViewTimeStamp = 0;

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

        public bool HasNotification
        {
            get { return hasNotification; }
            set { hasNotification = value; }
        }

        public PADI_Master()
        {
            workerServerList = new List<ObjectServer>();
            objectServerHeartBeatTimeStamp = new Dictionary<string, DateTime>();
        }

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






    }
}
