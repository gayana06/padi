﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class WorkerServer : MarshalByRefObject
    {
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


    }
}
