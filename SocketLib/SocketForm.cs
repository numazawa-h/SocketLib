using NCommonUtility;
using SocketLib;
using SocketTool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;
using static System.Windows.Forms.LinkLabel;

namespace SampleMain
{
    public partial class SocketForm : Form
    {
        CommSocket _Socket;
        List<CheckBox> _checkBoxes = new List<CheckBox>();
        CommMessageEditor _CommMessageEditor;
        int _display_limit = -1;
        int _display_line = 0;
        string _logname = string.Empty;
        NLog _log = null;

        public SocketForm(CommSocket socket, string title =null)
        {
            _Socket = socket;
            _Socket.OnDisConnectEvent += OnDisConnect;
            _Socket.OnRecvCommEvent += OnReceive;
            _Socket.OnPreSendCommEvent += OnPreSend;
            _Socket.OnSendCommEvent += OnSend;

            WorkingArea working = socket.GetRuntime().Working;
            InitializeComponent();

            if (working.ContainsKeyIntValue("display_limit"))
            {
                _display_limit = working.GetIntValue("display_limit");
            }

            txt_ipAddr1.Text = socket.LocalIPAddress?.ToString();
            txt_portNo1.Text = socket.LocalPortno?.ToString();
            txt_ipAddr2.Text = socket.RemoteIPAddress.ToString();
            txt_portNo2.Text = socket.RemotePortno.ToString();

            // スクリプト実行指定チェックボックスセットアップ
            _checkBoxes.Add(checkBox1);
            _checkBoxes.Add(checkBox2);
            _checkBoxes.Add(checkBox3);
            _checkBoxes.Add(checkBox4);
            _checkBoxes.Add(checkBox5);
            _checkBoxes.Add(checkBox6);
            _checkBoxes.Add(checkBox7);
            _checkBoxes.Add(checkBox8);
            _checkBoxes.Add(checkBox9);
            _checkBoxes.Add(checkBox10);
            _checkBoxes.Add(checkBox11);
            _checkBoxes.Add(checkBox12);
            foreach (CheckBox checkBox in _checkBoxes)
            {
                checkBox.Visible = false;
                checkBox.Checked = false;
            }

            // 電文表示指定チェックボックスセットアップ
            int cmdidx =0;
            int dispidx = 8;
            foreach (ScriptGroup script in _Socket.GetRuntime().Scripts.GetScriptListOnDisplay())
            {
                if (script.Display)
                {
                    if (script.When == "disp")
                    {
                        if (dispidx < 12)
                        {
                            _checkBoxes[dispidx].Tag = script;
                            _checkBoxes[dispidx].Text = script.ID;
                            _checkBoxes[dispidx].Checked = script.Enabled;
                            _checkBoxes[dispidx].Visible = true;
                            ++dispidx;
                        }
                    }
                    else
                    {
                        if (cmdidx < 8)
                        {
                            _checkBoxes[cmdidx].Tag = script;
                            _checkBoxes[cmdidx].Text = script.ID;
                            _checkBoxes[cmdidx].Checked = script.Enabled;
                            _checkBoxes[cmdidx].Visible = true;
                            ++cmdidx;
                        }
                    }
                }
            }

            // 電文編集タブのセットアップ
            foreach ( string id in working.GetValueMsgKeyList())
            {
                CommMessage msg= working.GetValueMsg(id);
                string disp = working.GetValueMsgDisp(id);
                cbx_MessageType.AddItem(disp, msg);
            }
            _CommMessageEditor= new CommMessageEditor(pnl_commMessage, btn_001, cbx_001, lbl_001, cbx_MessageType.Width - 20, cbx_001.Height + 4);

            // タイトル設定
            IPAddress ip = null;
            int? portno = null;
            if (socket.isServer)
            {
                // listen-acceptなら相手先アドレス
                ip = socket.RemoteIPAddress;
                portno = socket.RemotePortno;
            }
            if (socket.isClient)
            {
                // connectなら自アドレス
                ip = socket.LocalIPAddress;
                portno = socket.LocalPortno;
            }
            string ipaddress = "";
            if (ip != null)
            {
                ipaddress = $"[{ip.ToString()}";
                if (portno != null)
                {
                    ipaddress += "-" + portno;
                }
                ipaddress += "]";
            }
            if (title != null)
            {
                this.Text = title + ipaddress;
            }
            else
            {
                if (socket.isServer)
                {
                    this.Text = "サーバーソケット" + ipaddress;
                }
                if (socket.isClient)
                {
                    this.Text = "クライアントソケット" + ipaddress;
                }
            }

            // SocketFormごとにログファイルを作る
            _logname = this.Text;
            _log = Log.GetLogger(_logname);
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var ver = assembly.Version;
            _log.Info($"{assembly.Name} (version{ver.Major}.{ver.Minor}.{ver.Build}) Started(SocketForm)*******************************************");
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <remarks>
        /// NLogの解放を含めるためにSocketForm.Designer.csから移動
        /// </remarks>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                Log.RemoveLogger(_logname);
            }
            base.Dispose(disposing);
        }

        private void DisplayLog(string message)
        {
            ++_display_line;
            if (_display_limit>=0 && _display_line > _display_limit)
            {
                // 指定の表示行数を超えていたら表示しない
                return;
            }
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
            try
            {
                if (isDisplay(msg))
                {
                    DisplayLog($"RECV {msg.DName}{msg.GetDescription()}");
                }
                _log.Info($"RECV {msg.DName} [{new ByteArray(msg.GetHead()).to_hex(0, 0, " ")}] [{new ByteArray(msg.GetData()).to_hex(0, 0, " ")}]");
                _Socket.GetRuntime().Scripts.ExecOnRecv(_Socket, msg);
                _CommMessageEditor.refresh();
            }
            catch (Exception ex)
            {
                string errmsg = $"OnReceiveイベントハンドラで例外発生({msg.DName})";
                _log.Warn(errmsg, ex);
                DisplayLog(errmsg);
            }
        }

        private void OnPreSend(object sender, CommMessageEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new CommMessageEventHandler(OnPreSend), new object[] { sender, args });
                return;
            }
            CommMessage msg = (args.CommMsg);
            try
            {
                _Socket.GetRuntime().Scripts.ExecOnSend(_Socket, msg);
            }
            catch (Exception ex)
            {
                string errmsg = $"OnPreSendイベントハンドラで例外発生({msg.DName})";
                _log.Warn(errmsg, ex);
                DisplayLog(errmsg);
            }
        }
        private void OnSend(object sender, CommMessageEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new CommMessageEventHandler(OnSend), new object[] { sender, args });
                return;
            }
            CommMessage msg = (args.CommMsg);
            try
            {
                if (isDisplay(msg))
                {
                    DisplayLog($"SEND {msg.DName}{msg.GetDescription()}");
                }
                _log.Info($"SEND {msg.DName} [{new ByteArray(msg.GetHead()).to_hex(0, 0, " ")}] [{new ByteArray(msg.GetData()).to_hex(0, 0, " ")}]");
            }
            catch (Exception ex)
            {
                string errmsg = $"OnSendイベントハンドラで例外発生({msg.DName})";
                _log.Warn(errmsg, ex);
                DisplayLog(errmsg);
            }
        }

        private bool isDisplay(CommMessage msg)
        {
            for (int i = 8; i < 12; i++)
            {
                CheckBox cb = this._checkBoxes[i];
                ScriptGroup scr = (ScriptGroup)cb.Tag;
                if(scr!=null && scr.Exec(_Socket, msg) == true)
                {
                    return cb.Checked;
                }
            }
            return true;
        }
        private void SocketForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _Socket.Close();
            _Socket.GetRuntime().Scripts.ExecOnDisconnect();
        }

        private void btn_clear_Click(object sender, EventArgs e)
        {
            _display_line = 0;
            this.txt_log.Clear();
        }

        private void SocketForm_Load(object sender, EventArgs e)
        {
            try
            {
                _Socket.GetRuntime().Scripts.ExecOnConnect(_Socket);
            }
            catch (Exception ex)
            {
                string errmsg = $"SocketForm_Loadイベントハンドラで例外発生)";
                _log.Warn(errmsg, ex);
                DisplayLog(errmsg);
            }
        }


        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb =(CheckBox)sender;
            ((ScriptGroup)cb.Tag).Enabled = cb.Checked;
        }


        public void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドラッグ中のファイルやディレクトリの取得
                string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string d in drags)
                {
                    if (!System.IO.File.Exists(d))
                    {
                        // ファイル以外であればイベント・ハンドラを抜ける
                        return;
                    }
                }
                e.Effect = DragDropEffects.Copy;
            }
        }

        public void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in files)
            {
                try
                {
                    string file_ext = Path.GetExtension(path);
                    if (file_ext == ".txt")
                    {
                        CommMessage msg = CommMessage.LoadFileText(_Socket.GetRuntime(), path);
                        _Socket.Send(msg);
                    }
                    else if (file_ext == ".bin")
                    {
                        CommMessage msg = CommMessage.LoadFileBinary(_Socket.GetRuntime(), path);
                        _Socket.Send(msg);
                    }
                    else if (file_ext == ".json")
                    {
                        JsonConfig.RootNode root = JsonConfig.ReadJson(path);
                        foreach (Node def in root["Commands"])
                        {
                            Command.ReadJson(def, _Socket.GetRuntime()).Exec(_Socket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"ドラッグドロップ'{path}'読み込みで例外", ex);
                }
            }
        }

        private void Cbx_MessageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommMessage msg = (CommMessage)cbx_MessageType.SelectedValue;
            string name = cbx_MessageType.GetSelectedDisplay();
            _CommMessageEditor.SetCommMessage(msg,name);
        }

        private void Btn_init_Click(object sender, EventArgs e)
        {
            _CommMessageEditor.InitCommMessage(_Socket.GetRuntime().Working);
        }

        private void Btn_send_Click(object sender, EventArgs e)
        {
            _CommMessageEditor.SendCommMessage(_Socket);
        }

        private void Btn_save_Click(object sender, EventArgs e)
        {
            _CommMessageEditor.Save();
        }

        private void Btn_load_Click(object sender, EventArgs e)
        {
            _CommMessageEditor.Load(_Socket.GetRuntime());
        }
    }
}
