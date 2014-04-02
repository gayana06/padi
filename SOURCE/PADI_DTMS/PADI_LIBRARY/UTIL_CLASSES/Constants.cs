using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class Constants
    {
        public const string TCP_HEADER = "tcp://";
        public const string SEP_COMMA = ",";
        public const string SEP_COLON = ":";
        public const string SEP_SLASH = "/";
        public const string STR_PORT = "port";

        public const char SEP_CHAR_COMMA = ',';
        public const char SEP_CHAR_COLON = ':';
        

        public const string APPSET_MASTER_IP="MASTER_IP";
        public const string APPSET_WORKER_IP = "WORKER_IP";
        public const string APPSET_MASTER_PORT = "MASTER_PORT";
        public const string APPSET_WORKER_PORT = "WORKER_PORT";
        public const string APPSET_HEARTBEAT_PERIOD = "HEARTBEAT_PERIOD";
        public const string APPSET_OBJ_SERVER_FAIL_DECTOR_FREQUENCY = "OBJ_SERVER_FAIL_DECTOR_FREQUENCY";
        public const string APPSET_OBJ_SERVER_FAIL_TIME = "OBJ_SERVER_FAIL_TIME";

        public const string OBJECT_TYPE_PADI_WORKER = "PADI_Worker";
        public const string OBJECT_TYPE_PADI_MASTER = "PADI_Master";
        public const string OBJECT_TYPE_PADI_CLIENT="PADI_Client";
        public const string OBJECT_TYPE_PADI_COORDINATOR = "PADI_Coordinator";
        public const string OBJECT_TYPE_PADINT = "ServerPadInt";
        
    }
}
