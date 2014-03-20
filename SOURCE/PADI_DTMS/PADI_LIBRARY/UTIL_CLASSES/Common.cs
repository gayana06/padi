using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PADI_LIBRARY
{
    public class Common
    {
        private static ILogger logger;

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
        public static string GenerateTcpUrl(string ip, string port,string objectName)
        {
            return Constants.TCP_HEADER + ip + Constants.SEP_COLON + port + Constants.SEP_SLASH + objectName;
        }

        /// <summary>
        /// Generate Master TCP url
        /// </summary>
        /// <returns></returns>
        public static string GetMasterTcpUrl()
        {
            string ip=ConfigurationManager.AppSettings[Constants.APPSET_MASTER_IP];
            string port = ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT];
            string objectName = ConfigurationManager.AppSettings[Constants.OBJECT_TYPE_PADI_MASTER];
            return GenerateTcpUrl(ip, port, objectName);
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
    }
}
