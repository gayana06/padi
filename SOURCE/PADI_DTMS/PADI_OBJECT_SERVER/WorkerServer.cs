#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Configuration;
using PADI_LIBRARY;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Collections;

#endregion

namespace PADI_OBJECT_SERVER
{
    class WorkerServer
    {
        #region Initialization

        TcpChannel workerChannel;
        PADI_Worker worker;       

        public WorkerServer()
        {
            StartWorkerServer();
        }

        public void StartWorkerServer()
        {
            System.Threading.Timer timer = null;
            try
            {

                string workerPort = ConfigurationManager.AppSettings[Constants.APPSET_WORKER_PORT];
                BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
                provider.TypeFilterLevel = TypeFilterLevel.Full;
                IDictionary props = new Hashtable();
                props[Constants.STR_PORT] = Int16.Parse(workerPort);
                workerChannel = new TcpChannel(props,null,provider);
                worker = new PADI_Worker();
                ChannelServices.RegisterChannel(workerChannel, false);
                RemotingServices.Marshal(worker, Constants.OBJECT_TYPE_PADI_WORKER, typeof(PADI_Worker));
                bool isBootStraped = worker.BootstrapMaster(workerPort);
                timer = new System.Threading.Timer(worker.SendHeartBeatMessage, null, long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_HEARTBEAT_PERIOD]), long.Parse(ConfigurationManager.AppSettings[Constants.APPSET_HEARTBEAT_PERIOD]));
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker startup failed.." + ex.Message);
                Common.Logger().LogError("Worker startup failed..", ex.Message, ex.Source);
            }
            finally
            {
                if (timer != null)
                    timer.Dispose();
            }

        }

        #endregion
    }
}
