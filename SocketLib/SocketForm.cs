using NCommonUtility;
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
        NSocketEx _Socket;

        public SocketForm(NSocketEx socket)
        {
            _Socket = socket;
            _Socket.OnDisConnectEvent += OnDisConnect;
            _Socket.OnRecvExEvent += OnReceive;
            _Socket.OnSendExEvent += OnSend;

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

        private void OnReceive(object sender, SendRecvExEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SendRecvExEventHandler(OnReceive), new object[] { sender, args });
                return;
            }
            ByteArray dat = new ByteArray(args.Comm_data);
            DisplayLog($"RECV {dat.to_text()}");
        }

        private void OnSend(object sender, SendRecvExEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SendRecvExEventHandler(OnSend), new object[] { sender, args });
                return;
            }
            ByteArray dat = new ByteArray(args.Comm_data);
            DisplayLog($"SEND {dat.to_text()}");
        }


        private void SocketForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _Socket.Close();
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            ByteArray comm_dat = new ByteArray(txt_sendData.Text);
            ByteArray comm_hed = new ByteArray((UInt16)comm_dat.Length());
            _Socket.Send(comm_hed.GetData(), comm_dat.GetData());
        }
    }
}
