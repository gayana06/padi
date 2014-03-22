using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class PADI_Worker : MarshalByRefObject
    {
        private ObjectServer[] objectServerList;

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void ReceiveObjectServerList(ObjectServer[] objectServerList)
        {
            this.objectServerList=objectServerList;
            Console.WriteLine("New Object Server List received");
            Common.Logger().LogInfo("New Object Server List received",string.Empty,string.Empty);
        }

    }
}
