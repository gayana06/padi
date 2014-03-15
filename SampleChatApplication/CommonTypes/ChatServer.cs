using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace CommonTypes
{
    public class ChatServer : MarshalByRefObject
    {
        private Dictionary<string, string> clientList;
        TcpChannel channel;
        public ChatServer(TcpChannel channel)
        {
            clientList = new Dictionary<string, string>();
            this.channel = channel;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Join(String name, String url)
        {
            if (!clientList.ContainsKey(name.ToLower()))
            {
                clientList.Add(name.ToLower(), url);
            }
        }

        public void CtoSMessage(String name, String message)
        {
            foreach (var val in clientList)
            {
                if (val.Key.ToLower() != name.ToLower())
                {
                    ChatClient obj = (ChatClient)Activator.GetObject(
                        typeof(ChatClient),
                        val.Value);
                    obj.StoCMessage(name+" sent : "+message);
                }
            }
        }
    }
}
