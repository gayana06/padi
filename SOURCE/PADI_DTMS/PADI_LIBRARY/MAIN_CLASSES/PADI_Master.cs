using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class PADI_Master : MarshalByRefObject
    {
        private List<ObjectServer> workerServerList;
        private int serverIndex = 0;
        private const string PREFIX_WORKER_SERVER = "W_SERVER_";

        public PADI_Master()
        {
            workerServerList = new List<ObjectServer>();
        }

        /// <summary>
        /// Create a WorkerServer and update details.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns>unique server name</returns>
        public string Bootstrap(string ip, string port)
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

                return wserver.ServerName;
            }
        }


    }
}
