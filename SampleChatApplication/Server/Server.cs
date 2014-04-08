using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypes;

namespace RemotingSample {

	class Server {
       
		static void Main(string[] args) 
        {
            Server server = new Server();
            server.StartServer();           
        }


        public void StartServer()
        {
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            ChatServer server = new ChatServer(channel);
            RemotingServices.Marshal(server, "ChatServer", typeof(ChatServer));

            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }


	}
}