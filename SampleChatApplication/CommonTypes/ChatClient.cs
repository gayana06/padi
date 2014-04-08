using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CommonTypes
{
    public class ChatClient : MarshalByRefObject
    {
        public List<string> msgList;

        public ChatClient()
        {
            msgList = new List<string>();
        }

        public void StoCMessage(String message)
        {
            msgList.Add(message);
        }
    }
}
