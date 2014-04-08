using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypes;
using System.Configuration;
using System.Net;
using System.Net.Sockets;

namespace ClientForm
{

    public partial class Form1 : Form
    {
        ChatClient client;
        TcpChannel rchannel;
        TcpChannel schannel;
        String serverUrl = ConfigurationManager.AppSettings["SERVER_URL"];
        public Form1()
        {
            InitializeComponent();
            client = new ChatClient();
            timer1.Interval = (500) * (1);              // Timer will tick evert second
            timer1.Enabled = true;                       // Enable the timer
            timer1.Start();   
        }

        public void StartReceiver()
        {
            rchannel = new TcpChannel(Int32.Parse(tbxPort.Text));
            ChannelServices.RegisterChannel(rchannel, false);
            RemotingServices.Marshal(client, "ChatClient", typeof(ChatClient));
        }


        private void btnJoin_Click(object sender, EventArgs e)
        {
            StartReceiver();
            ChatServer obj = (ChatServer)Activator.GetObject(
                typeof(ChatServer),
                serverUrl+"/ChatServer");
            obj.Join(tbxName.Text, "tcp://" + LocalIPAddress() + ":" + tbxPort.Text + "/ChatClient");
            tbxName.Enabled = false;
            tbxPort.Enabled = false;
            btnJoin.Enabled = false;
        }

        public void WriteChat()
        {
            if (client.msgList.Count > 0)
            {
                foreach (var val in client.msgList)
                    tbxChat.Text += "\r\n" + val;
                client.msgList.Clear();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            ChatServer obj = (ChatServer)Activator.GetObject(
            typeof(ChatServer),
            serverUrl+"/ChatServer");
            obj.CtoSMessage(tbxName.Text,tbxMsg.Text);
            tbxChat.Text += "\r\n" +"You sent :" +tbxMsg.Text;
            tbxMsg.Text = string.Empty;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            WriteChat();
        }

        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }



    }
}
