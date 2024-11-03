using NCommonUtility;
using SocketTool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleMain
{
    public partial class SocketForm : Form
    {
        CommSocket _Socket;

        public SocketForm(CommSocket socket)
        {
            _Socket = socket;
            _Socket.OnDisConnectEvent += OnDisConnect;
            _Socket.OnRecvCommEvent += OnReceive;
            _Socket.OnPreSendCommEvent += OnPreSend;
            _Socket.OnSendCommEvent += OnSend;

            InitializeComponent();

            txt_ipAddr1.Text = socket.LocalIPAddress?.ToString();
            txt_portNo1.Text = socket.LocalPortno?.ToString();
            txt_ipAddr2.Text = socket.RemoteIPAddress.ToString();
            txt_portNo2.Text = socket.RemotePortno.ToString();

            if (socket.isServer)
            {
                this.Text = "サーバーソケット";
            }
            if (socket.isClient)
            {
                this.Text = "クライアントソケット";
            }
        }

        private void DisplayLog(string message)
        {
            txt_log.Text += $"{DateTime.Now} {message}\r\n";
        }

        private void OnDisConnect(object sender, NSocketEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new NSocketEventHandler(OnDisConnect), new object[] { sender, args });
                return;
            }
            this.Close();
        }

        private void OnReceive(object sender, CommMessageEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new CommMessageEventHandler(OnReceive), new object[] { sender, args });
                return;
            }
            CommMessage msg = (args.CommMsg);
            DisplayLog($"RECV {msg.DName}{msg.GetDescription()}");
        }

        private void OnPreSend(object sender, CommMessageEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new CommMessageEventHandler(OnPreSend), new object[] { sender, args });
                return;
            }
            CommMessage msg = (args.CommMsg);
            ScriptDefine.GetInstance().ExecOnSend(msg);
        }
        private void OnSend(object sender, CommMessageEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new CommMessageEventHandler(OnSend), new object[] { sender, args });
                return;
            }
            CommMessage msg = (args.CommMsg);
            DisplayLog($"SEND {msg.DName}{msg.GetDescription()}");
            Log.Info($"SEND {msg.DName} [{new ByteArray(msg.GetHead()).to_hex(0,0," ")}] [{new ByteArray(msg.GetData()).to_hex(0, 0, " ")}]");
        }


        private void SocketForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _Socket.Close();
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            CommMessage msg = new CommMessage("22");
            msg.SetHedValue("hdatm", new ByteArray(DateTime.Now, "yyyyMMddHHmmss"));
            _Socket.Send(msg);
            CommMessage msg2 = new CommMessage("30");
            msg2.SetFldValue("control-type", ByteArray.ParseHex("0001"));
            System.Threading.Thread.Sleep(1000);
            _Socket.Send(msg2);
            msg2.SetFldValue("control-type", ByteArray.ParseHex("0002"));
            System.Threading.Thread.Sleep(1000);
            _Socket.Send(msg2);
            msg2.SetFldValue("control-type", ByteArray.ParseHex("0003"));
            System.Threading.Thread.Sleep(1000);
            _Socket.Send(msg2);
        }
    }
}
