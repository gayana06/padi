#region Directive Section

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    public class Common
    {
        #region Initialization

        private static ILogger logger;

        #endregion

        #region Public Members

        /// <summary>
        /// Log instance initiation
        /// </summary>
        /// <returns></returns>
        public static ILogger Logger()
        {
            if (logger == null)
                logger = Log4NetLogger.GetInstance();
            return logger;
        }

        /// <summary>
        /// sample url returned => tcp://ip:port/objectName
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static string GenerateTcpUrl(string ip, string port, string objectName)
        {
            return Constants.TCP_HEADER + ip + Constants.SEP_COLON + port + Constants.SEP_SLASH + objectName;
        }

        /// <summary>
        /// Generate Master TCP url
        /// </summary>
        /// <returns></returns>
        public static string GetMasterTcpUrl()
        {
            string ip = ConfigurationManager.AppSettings[Constants.APPSET_MASTER_IP];
            string port = ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT];
            string objectName = Constants.OBJECT_TYPE_PADI_MASTER;
            return GenerateTcpUrl(ip, port, objectName);
        }

        /// <summary>
        /// Get the index of the serverlist provided where the UID belongs
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="objectServerMap"></param>
        /// <returns></returns>
        public static int GetModuloServerIndex(int uid, ObjectServer[] objectServerMap)
        {
            int index = -1;
            if (objectServerMap.Length > 0)
                index = uid % objectServerMap.Length;
            return index;
        }

        /// <summary>
        /// Get Local Ip address
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            IPHostEntry host;
            string localIP = string.Empty;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        /// <summary>
        /// Get the object server reference by the unique name provide
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="objectServerList"></param>
        /// <returns></returns>
        public static ObjectServer GetObjectServerByName(string serverName, List<ObjectServer> objectServerList)
        {
            ObjectServer server = null;
            if (objectServerList.Count > 0)
            {
                server = objectServerList.Single(s => s.ServerName == serverName);
            }
            return server;
        }

        #endregion
    }
}
