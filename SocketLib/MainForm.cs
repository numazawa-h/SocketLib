using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NCommonUtility;
using SocketTool;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static SocketLib.Program;

namespace SampleMain
{
    public partial class MainForm : Form
    {
        CommSocket listensocket = new CommSocket();
        Point SocketFormLocation = new Point(-1, -1);

        public MainForm()
        {
            InitializeComponent();
            listensocket.OnExceptionEvent += OnException;
            listensocket.OnAcceptEvent += OnAccept;
            listensocket.OnDisConnectEvent += OnDisConnect;

            foreach (var item in NetworkDefine.GetInstance().GetNames())
            {
               this.cbx_NetworkName.Items.Add(item);
            }
        }

        private void DisplayLog(string message)
        {
            txt_log.Text += $"{DateTime.Now}  {message}\r\n";
        }

        private void btn_listen_Click(object sender, EventArgs e)
        {
            string iaddr1 = txt_ipAddr1.Text.Trim();
            string portno1 = txt_portNo1.Text.Trim();
            DisplayLog($"listen [{iaddr1} Port#{portno1}]");
                listensocket.Listen(iaddr1, portno1);
            }
        private void btn_stopListen_Click(object sender, EventArgs e)
        {
            if (listensocket.isOpen == false)
            {
                DisplayLog($"listen is NOT started");
                return;
            }
            string iaddr1 = listensocket.LocalIPAddress.ToString();
            string portno1 = listensocket.LocalPortno.ToString();
            DisplayLog($"listen STOP [{iaddr1} Port#{portno1}]");
            listensocket.Close();
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            CommSocket socket = new CommSocket();
            socket.OnExceptionEvent += OnException;
            socket.OnConnectEvent += OnConnect;
            socket.OnDisConnectEvent += OnDisConnect;

            string iaddr1 = txt_ipAddr1.Text.Trim();
            string portno1 = txt_portNo1.Text.Trim();
            if (iaddr1.Length > 0)
            {
                try
                {
                    socket.SetSelfEndPoint(iaddr1, portno1);
                }
                catch (Exception ex)
                {
                    OnException(this, new ThreadExceptionEventArgs(new Exception("Local ipアドレスの指定が不正です。", ex)));
                    socket.Close();
                    return;
                }
            }
            string iaddr2 = txt_ipAddr2.Text.Trim();
            string portno2 = txt_portNo2.Text.Trim();
                socket.Connect(iaddr2, portno2);
            }


        private void OnException(object sender, ThreadExceptionEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ThreadExceptionEventHandler(OnException), new object[] { sender, args });
                return;
            }

            DisplayLog(args.Exception.Message);
            if(args.Exception.InnerException != null)
            {
                DisplayLog(args.Exception.InnerException.Message);
            }
            Log.Warn(args.Exception.Message, args.Exception.InnerException);
        }

        private void OnDisConnect(object sender, NSocketEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new NSocketEventHandler(OnDisConnect), new object[] { sender, args });
                return;
            }
            DisplayLog($"OnDisConnect {args.Socket.RemoteIPAddress}:{args.Socket.RemotePortno}");
        }

        private void OnAccept(object sender, NSocketEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new NSocketEventHandler(OnAccept), new object[] { sender, args });
                return;
            }

            CommSocket socket = (CommSocket)args.Socket;
            socket.OnExceptionEvent += OnException;
            socket.OnDisConnectEvent += OnDisConnect;

            DisplayLog($"OnAccept {socket.RemoteIPAddress}:{socket.RemotePortno}");
            string title = "サーバー";
            if (cbx_NetworkName.SelectedIndex >= 0)
            {
                title = cbx_NetworkName.Text;
            }
            var frm = new SocketForm(socket, title);
            if (SocketFormLocation.X < 0)
            {
                SocketFormLocation = new Point(this.Location.X, this.Location.Y);
            }
            SocketFormLocation.Offset(50, 30);
            frm.StartPosition = FormStartPosition.Manual;
            frm.Location = SocketFormLocation;
            frm.Show();
        }

        private void OnFailListen(object sender, NSocketEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new NSocketEventHandler(OnFailListen), new object[] { sender, args });
                return;
            }
            DisplayLog($"OnFailListen {args.Socket.LocalIPAddress}:{args.Socket.LocalPortno}");
        }

        private void OnConnect(object sender, NSocketEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new NSocketEventHandler(OnConnect), new object[] { sender, args });
                return;
            }
            DisplayLog($"OnConnect {args.Socket.RemoteIPAddress}:{args.Socket.RemotePortno}");
            string title = "クライアント";
            if (cbx_NetworkName.SelectedIndex >= 0)
            {
                title = cbx_NetworkName.Text;
            }
            var frm = new SocketForm((CommSocket)args.Socket, title);
            if (SocketFormLocation.X < 0)
            {
                SocketFormLocation = new Point(this.Location.X, this.Location.Y);
            }
            SocketFormLocation.Offset(50, 30);
            frm.StartPosition = FormStartPosition.Manual;
            frm.Location = SocketFormLocation;
            frm.Show();
        }

        private void Cbx_NetworkName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cbx_NetworkName.SelectedIndex < 0)
            {
                btn_listen.Enabled = true;
                btn_stopListen.Enabled = true;
                btn_connect.Enabled = true;
                return;
            }

            string name = this.cbx_NetworkName.Text;
            IPEndPoint local = NetworkDefine.GetInstance().GetLocalEndPoint(name);
            if (local == null)
            {
                this.txt_ipAddr1.Text = "";
                this.txt_portNo1.Text = "";
            }
            else
            {
                this.txt_ipAddr1.Text = local.Address.ToString();
                this.txt_portNo1.Text = local.Port.ToString();
            }

            IPEndPoint remote = NetworkDefine.GetInstance().GetRemoteEndPoint(name);
            if (remote == null)
            {
                this.txt_ipAddr2.Text = "";
                this.txt_portNo2.Text = "";
            }
            else
            {
                this.txt_ipAddr2.Text = remote.Address.ToString();
                this.txt_portNo2.Text = remote.Port.ToString();
            }

            if (NetworkDefine.GetInstance().isListenAddr(name))
            {
                btn_listen.Enabled = true;
                btn_stopListen.Enabled = true;
                btn_connect.Enabled = false;
            }
            else if (NetworkDefine.GetInstance().isConnectAddr(name))
            {
                btn_listen.Enabled = false;
                btn_stopListen.Enabled = false;
                btn_connect.Enabled = true;
            }
            else
            {
                btn_listen.Enabled = true;
                btn_stopListen.Enabled = true;
                btn_connect.Enabled = true;
            }
        }

        private void Cbx_NetworkName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                cbx_NetworkName.SelectedIndex = -1;
                cbx_NetworkName.Text = "";
            }
        }
    }
}
