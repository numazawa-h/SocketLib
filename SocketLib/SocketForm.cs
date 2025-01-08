﻿using NCommonUtility;
using SocketLib;
using SocketTool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

        public SocketForm(CommSocket socket, string title=null)
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
            foreach (ScriptList script in ScriptDefine.GetInstance().GetScriptList())
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
            foreach ( string id in ScriptDefine.GetInstance().GetValueMsgKeyList())
            {
                CommMessage msg= ScriptDefine.GetInstance().GetValueMsg(id);
                string disp = ScriptDefine.GetInstance().GetValueMsgDisp(id);
                cbx_MessageType.AddItem(disp, msg);
            }
            _CommMessageEditor= new CommMessageEditor(panel4, btn_001, cbx_001, lbl_001, cbx_MessageType.Width, cbx_001.Height + 4);

            // タイトル設定
            if (title != null)
            {
                this.Text = title;
            }
            else
            {
                if (socket.isServer)
                {
                    this.Text = "サーバーソケット";
                }
                if (socket.isClient)
                {
                    this.Text = "クライアントソケット";
                }
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
            try
            {
                if (isDisplay(msg))
                {
                    DisplayLog($"RECV {msg.DName}{msg.GetDescription()}");
                }
                Log.Info($"RECV {msg.DName} [{new ByteArray(msg.GetHead()).to_hex(0, 0, " ")}] [{new ByteArray(msg.GetData()).to_hex(0, 0, " ")}]");
                ScriptDefine.GetInstance().ExecOnRecv(_Socket, msg);
            }
            catch (Exception ex)
            {
                string errmsg = $"OnReceiveイベントハンドラで例外発生({msg.DName})";
                Log.Warn(errmsg, ex);
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
                ScriptDefine.GetInstance().ExecOnSend(_Socket, msg);
            }
            catch (Exception ex)
            {
                string errmsg = $"OnPreSendイベントハンドラで例外発生({msg.DName})";
                Log.Warn(errmsg, ex);
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
                Log.Info($"SEND {msg.DName} [{new ByteArray(msg.GetHead()).to_hex(0, 0, " ")}] [{new ByteArray(msg.GetData()).to_hex(0, 0, " ")}]");
            }
            catch (Exception ex)
            {
                string errmsg = $"OnSendイベントハンドラで例外発生({msg.DName})";
                Log.Warn(errmsg, ex);
                DisplayLog(errmsg);
            }
        }

        private bool isDisplay(CommMessage msg)
        {
            for (int i = 8; i < 12; i++)
            {
                CheckBox cb = this._checkBoxes[i];
                ScriptList scr = (ScriptList)cb.Tag;
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
            ScriptDefine.GetInstance().ExecOnDisconnect();
        }

        private void btn_clear_Click(object sender, EventArgs e)
        {
            this.txt_log.Clear();
        }

        private void SocketForm_Load(object sender, EventArgs e)
        {
            try
            {
                ScriptDefine.GetInstance().ExecOnConnect(_Socket);
            }
            catch (Exception ex)
            {
                string errmsg = $"SocketForm_Loadイベントハンドラで例外発生)";
                Log.Warn(errmsg, ex);
                DisplayLog(errmsg);
            }
        }


        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb =(CheckBox)sender;
            ((ScriptList)cb.Tag).Enabled = cb.Checked;
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
            int dtypelen = CommMessageDefine.GetInstance().GetMessageDefine("head").GetFldDefine("dtype").Length;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in files)
            {
                try
                {
                    string dtype = null;
                    byte[] hed = System.Array.Empty<byte>();
                    byte[] dat = System.Array.Empty<byte>();
                    string fname = System.IO.Path.GetFileName(path);

                    string file_ext = Path.GetExtension(path);
                    if (file_ext == ".txt")
                    {
                        dtype = null;
                        hed = System.Array.Empty<byte>();
                        dat = System.Array.Empty<byte>();
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(path))
                        {
                            string filetext = sr.ReadToEnd();
                            Regex reg_comment = new Regex("//.*\\n");
                            Regex reg_whitesp = new Regex("[\\r\\n\\s\\t]");
                            string txt = reg_whitesp.Replace(reg_comment.Replace(filetext, ""), "");
                            var matchs = Regex.Matches(txt, @"\[[0-9,a-f,A-F ]*\]");
                            if (matchs.Count != 2)
                            {
                                throw new Exception($"フォーマットが[head][data]の形式ではありません");
                            }
                            Regex r = new Regex("[\\[\\] \\t]");
                            string hed_bcd = r.Replace(matchs[0].Value, "");
                            string dat_bcd = r.Replace(matchs[1].Value, "");
                            if (hed_bcd.Length == (dtypelen*2))
                            {
                                dtype = hed_bcd;
                                dat = ByteArray.ParseHex(dat_bcd);
                            }
                            else
                            {
                                hed = ByteArray.ParseHex(hed_bcd);
                                dat = ByteArray.ParseHex(dat_bcd);
                            }
                        }
                        if (dtype != null && dat.Length > 0)
                        {
                            _Socket.Send(new CommMessage(dtype, dat));
                        }
                        if (hed.Length > 0 && dat.Length > 0)
                        {
                            _Socket.Send(new CommMessage(hed, dat));
                        }
                    }
                    else if (file_ext == ".bin")
                    {
                        dtype = fname.Substring(0, dtypelen * 2);
                        if (CommMessageDefine.GetInstance().Contains(dtype) == false)
                        {
                            throw new Exception($"dtype'{dtype}'が定義されていません");
                        }
                        using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            dat = new byte[fs.Length];
                            fs.Read(dat, 0, dat.Length);
                        }
                        _Socket.Send(new CommMessage(dtype, dat));
                    }
                    else if (file_ext == ".json")
                    {
                        JsonConfig.RootNode root = JsonConfig.ReadJson(path);
                        foreach (Node def in root["Commands"])
                        {
                            Command.ReadJson(def).Exec(_Socket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"ドラッグドロップ'{path}'読み込みで例外", ex);
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
            _CommMessageEditor.InitCommMessage();
        }

        private void Btn_send_Click(object sender, EventArgs e)
        {
            _CommMessageEditor.SendCommMessage(_Socket);
        }
    }
}
