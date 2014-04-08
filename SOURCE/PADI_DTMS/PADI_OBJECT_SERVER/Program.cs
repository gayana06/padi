using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_OBJECT_SERVER
{
    class Program
    {
        static void Main(string[] args)
        {
            Common.Logger().LogInfo("Worker started-----:", DateTime.Now.ToString(), string.Empty);
            WorkerServer workerServer = new WorkerServer();
        }
    }
}
