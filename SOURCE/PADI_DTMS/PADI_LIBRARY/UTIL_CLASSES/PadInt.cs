using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class PadInt
    {
        int uID;

        public int UID
        {
            get { return uID; }
            set { uID = value; }
        }
        
        ServerPadInt svrPadInt;
        PADI_Client client;

        public ServerPadInt SvrPadInt
        {
            get { return svrPadInt; }
            set { svrPadInt = value; }
        }

        public PadInt(int uID, PADI_Client client)
        {
            this.UID = uID;
            this.client = client;
        }

        public int Read()
        {            
            return svrPadInt.Read(client.TransactionId);
        }

        public void Write(int value)
        {
            svrPadInt.Write(client.TransactionId, value);
        }

    }
}
