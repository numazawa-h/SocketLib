using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NCommonUtility;
using SocketLib.NCommonUtility;

namespace SampleMain
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void DisplayLog(string message)
        {
            txt_log.Text += $"{DateTime.Now}{message}\r\n";
        }

        private void btn_listen_Click(object sender, EventArgs e)
        {
            NSocket listensocket = new NSocket();
            listensocket.OnExceptionEvent += OnException;
            listensocket.OnFailListenEvent += OnFailListen;
            listensocket.OnAcceptEvent += OnAccept;
            if (txt_ipAddr.Text.Trim() != "")
            {
                listensocket.SetSelfEndPoint(txt_ipAddr.Text.Trim(), txt_portno.Text.Trim());
            }
            DisplayLog($"listen [{listensocket.SelfIPAddress} Port#{listensocket.SelfPortno}]");
            listensocket.Listen();
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            ClientSocket socket = new ClientSocket();
            socket.OnExceptionEvent += OnException;
            socket.OnConnectEvent += OnConnect;
            socket.OnFailConnectEvent += OnFailConnect;
            socket.OnDisConnectEvent += OnDisConnect;

            socket.Connect(txt_ipAddr.Text, txt_portno.Text);

        }


        private void OnException(object sender, ThreadExceptionEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ThreadExceptionEventHandler(OnException), new object[] { sender, args });
                return;
            }

            MessageBox.Show(args.Exception.Message);
        }

        private void OnDisConnect(object sender, DisConnectEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new DisConnectEventHandler(OnDisConnect), new object[] { sender, args });
                return;
            }

            DisplayLog($"OnDisConnect {args.SocketBase.RemoteIPAddress}:{args.SocketBase.RemotePortno}");

        }

        private void OnAccept(object sender, AcceptEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new AcceptEventHandler(OnAccept), new object[] { sender, args });
                return;
            }

            NSocket listensocket = args.ListenSocket;
            ServerSocket socket = args.ServerSocket;
            socket.OnExceptionEvent += OnException;
            socket.OnDisConnectEvent += OnDisConnect;

            DisplayLog($"OnAccept {socket.RemoteIPAddress}:{socket.RemotePortno}");
        }

        private void OnFailListen(object sender, ListenEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new FailListenEventHandler(OnFailListen), new object[] { sender, args });
                return;
            }
            DisplayLog($"OnFailListen {args.NSocket.RemoteIPAddress}:{args.NSocket.RemotePortno}");
        }

        private void OnConnect(object sender, ConnectEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ConnectEventHandler(OnConnect), new object[] { sender, args });
                return;
            }
            DisplayLog($"OnConnect {args.ClientSocket.RemoteIPAddress}:{args.ClientSocket.RemotePortno}");

        }
        private void OnFailConnect(object sender, ConnectEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ConnectEventHandler(OnFailConnect), new object[] { sender, args });
                return;
            }
            DisplayLog($"OnFailConnect {args.ClientSocket.RemoteIPAddress}:{args.ClientSocket.RemotePortno}");
        }

    }
}
