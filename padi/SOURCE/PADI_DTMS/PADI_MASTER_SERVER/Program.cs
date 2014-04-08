using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_MASTER_SERVER
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Master Strated");
            Common.Logger().LogInfo("Master started", string.Empty, string.Empty);
            MasterServer master = new MasterServer(); 
            Console.WriteLine("Master stopped");
            Common.Logger().LogInfo("Master stopped", string.Empty, string.Empty);            
        }
    }
}
