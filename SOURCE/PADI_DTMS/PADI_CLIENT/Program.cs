using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PADI_LIBRARY;
using System.Windows.Forms;

namespace PADI_CLIENT
{
    class Program
    {
        static void Main(string[] args)
        {
            Common.Logger().LogInfo("Client started",string.Empty,string.Empty);
            ClientForm f = new ClientForm();
            Application.Run(f);
            
        }
    }
}
