using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class PADI_Master : MarshalByRefObject
    {

        public bool Bootstrap(string ip, Int16 port)
        {
            return true;
        }
    }
}
