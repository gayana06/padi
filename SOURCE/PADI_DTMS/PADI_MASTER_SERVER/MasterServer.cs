#region Directive Section

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

#endregion

namespace PADI_MASTER_SERVER
{
    class MasterServer
    {
        #region Initialization

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
                Thread notificationThread = new Thread(new ThreadStart(master.NotifyObjectServer));
                notificationThread.Start();
                failDetectorTimer = new System.Threading.Timer(master.DetectObjectServerFailure, null, long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_OBJ_SERVER_FAIL_DECTOR_FREQUENCY]), long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_OBJ_SERVER_FAIL_DECTOR_FREQUENCY]));

                Console.WriteLine("Master server started at port : " + masterPort);
                Common.Logger().LogInfo("Master server started", "Port : " + masterPort, string.Empty);
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

        #endregion
    }
}
