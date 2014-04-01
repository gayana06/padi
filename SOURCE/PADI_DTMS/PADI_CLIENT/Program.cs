using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PADI_LIBRARY;

namespace PADI_CLIENT
{
    class Program
    {
        static void Main(string[] args)
        {
            Common.Logger().LogInfo("Client started",string.Empty,string.Empty);
            Client client = new Client();
            client.Start();

        }
    }
}
