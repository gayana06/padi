using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;

namespace PADI_MASTER_SERVER
{
    class MasterServer
    {
        TcpChannel masterChannel;
        PADI_Master master;

        public MasterServer()
        {
            StartMasterServer();
        }

        public void StartMasterServer()
        {
            try
            {
                string masterPort = ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT];
                masterChannel = new TcpChannel(Int16.Parse(masterPort));
                master = new PADI_Master();
                ChannelServices.RegisterChannel(masterChannel, false);
                RemotingServices.Marshal(master, Constants.OBJECT_TYPE_PADI_MASTER, typeof(PADI_Master));

                Console.WriteLine("Master server started at port : " + masterPort);
                Common.Logger().LogInfo("Worker server started", "Port : " + masterPort, string.Empty);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Master startup failed.." + ex.Message);
                Common.Logger().LogError("Master startup failed..", ex.Message, ex.Source);
            }
        }
    }
}
