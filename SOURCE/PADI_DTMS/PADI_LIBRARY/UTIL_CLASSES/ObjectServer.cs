#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    [Serializable]
    public class ObjectServer
    {
        #region Initialization

        private String serverName;

        public String ServerName
        {
            get { return serverName; }
            set { serverName = value; }
        }

        private String serverIp;

        public String ServerIp
        {
            get { return serverIp; }
            set { serverIp = value; }
        }

        private String serverPort;

        public String ServerPort
        {
            get { return serverPort; }
            set { serverPort = value; }
        }
        private String tcpUrl;

        public String TcpUrl
        {
            get { return tcpUrl; }
            set { tcpUrl = value; }
        }

        private int serverIndex;

        public int ServerIndex
        {
            get { return serverIndex; }
            set { serverIndex = value; }
        }

        private string replicaServerName;

        public string ReplicaServerName
        {
            get { return replicaServerName; }
            set { replicaServerName = value; }
        }

        #endregion
    }
}
