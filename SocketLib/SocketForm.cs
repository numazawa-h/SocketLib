using NCommonUtility;
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
        List<NComboBox> _comboBoxes = new List<NComboBox>();
        List<Label> _fldNameLabels = new List<Label>();
        int _comboBoxes_width_narrow;
        int _comboBoxes_width_wide;
        int _comboBoxes_left;

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

            foreach (CommMessage msg in ScriptDefine.GetInstance().GetValueMsgList())
            {
                cbx_MessageType.AddItem(msg.DName, msg);
            }

            _comboBoxes_left = cbx_001.Left;
            _comboBoxes_width_narrow = lbl_001.Left - cbx_001.Left;
            _comboBoxes_width_wide = cbx_MessageType.Width - _comboBoxes_left;
            _comboBoxes.Add(cbx_001);
            _comboBoxes.Add(cbx_002);
            _comboBoxes.Add(cbx_003);
            _comboBoxes.Add(cbx_004);
            _comboBoxes.Add(cbx_005);
            _comboBoxes.Add(cbx_006);
            _comboBoxes.Add(cbx_007);
            _comboBoxes.Add(cbx_008);
            _comboBoxes.Add(cbx_009);
            _comboBoxes.Add(cbx_010);
            _comboBoxes.Add(cbx_011);
            _comboBoxes.Add(cbx_012);
            _comboBoxes.Add(cbx_013);
            _comboBoxes.Add(cbx_014);
            _comboBoxes.Add(cbx_015);
            _comboBoxes.Add(cbx_016);
            _comboBoxes.Add(cbx_017);
            _comboBoxes.Add(cbx_018);
            _comboBoxes.Add(cbx_019);
            _comboBoxes.Add(cbx_020);
            foreach (ComboBox cbx in _comboBoxes)
            {
              //  cbx.SelectedIndexChanged += Cbx_SelectedIndexChanged;
                cbx.DropDownClosed += Cbx_SelectedIndexChanged;
                cbx.TextUpdate += Cbx_TextUpdate;
                cbx.TextChanged += Cbx_TextChanged;
            }

            _fldNameLabels.Add(lbl_001);
            _fldNameLabels.Add(lbl_002);
            _fldNameLabels.Add(lbl_003);
            _fldNameLabels.Add(lbl_004);
            _fldNameLabels.Add(lbl_005);
            _fldNameLabels.Add(lbl_006);
            _fldNameLabels.Add(lbl_007);
            _fldNameLabels.Add(lbl_008);
            _fldNameLabels.Add(lbl_009);
            _fldNameLabels.Add(lbl_010);
            _fldNameLabels.Add(lbl_011);
            _fldNameLabels.Add(lbl_012);
            _fldNameLabels.Add(lbl_013);
            _fldNameLabels.Add(lbl_014);
            _fldNameLabels.Add(lbl_015);
            _fldNameLabels.Add(lbl_016);
            _fldNameLabels.Add(lbl_017);
            _fldNameLabels.Add(lbl_018);
            _fldNameLabels.Add(lbl_019);
            _fldNameLabels.Add(lbl_020);
            initComboBoxesLabels();

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

        private void initComboBoxesLabels()
        {
            foreach (ComboBox cbx in _comboBoxes)
            {
                cbx.Visible = false;
                cbx.Tag = null;
            }

            foreach (Label lbl in _fldNameLabels)
            {
                lbl.Visible = false;
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

        private void Cbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            NComboBox cbx = (NComboBox)sender;
            int idx = _comboBoxes.IndexOf(cbx);
            CommMessage msg = (CommMessage)cbx_MessageType.SelectedValue;

            Object tag = cbx.Tag;
            if (tag is FieldDefine)
            {
                // 選択されたフィールド値をWorkingAreaのメッセージに設定
                string val = (string)cbx.SelectedValue;
                if (val != null)
                {
                    FieldDefine fld = (FieldDefine)tag;
                    msg.SetFldValue(fld.FldId, ByteArray.ParseHex(val));
                }
            }
            else
            {
                if (isExpanding == false)
                {
                    // ブロックのインデックスを選択
                    BlockDefine block = (BlockDefine)cbx.SelectedValue;
                    expandBlock(block, msg);
                }
            }
        }

        private void Cbx_TextUpdate(object sender, EventArgs e)
        {
            NComboBox cbx = (NComboBox)sender;
            Object tag = cbx.Tag;
            if (tag is FieldDefine)
            {
                if (cbx.SelectedIndex >= 0)
                {
                    // 選択状態からユーザ入力があったら一旦選択値を表示する
                    cbx.Text = (string)cbx.SelectedValue;
                    cbx.SelectionStart = cbx.Text.Length;
                }
            }
        }

        private void Cbx_TextChanged(object sender, EventArgs e)
        {
            NComboBox cbx = (NComboBox)sender;
            int idx = _comboBoxes.IndexOf(cbx);

            Object tag = cbx.Tag;
            if (tag is FieldDefine)
            {
                if (cbx.SelectedIndex < 0)
                {
                    // 未選択の状態からユーザ入力があったら補正する
                    CommMessage msg = (CommMessage)cbx_MessageType.SelectedValue;
                    FieldDefine fld = (FieldDefine)tag;
                    int pos = cbx.SelectionStart;
                    int len = cbx.Text.Length;
                    // 16進文字列以外は削除
                    cbx.Text = new Regex("[^0-9a-fA-F]+").Replace(cbx.Text, "");
                    if (len > cbx.Text.Length)
                    {
                        // 削除した文字列分を補正
                        --pos;
                        len = cbx.Text.Length;
                    }
                    // フィールドの長さに合わせて補正
                    if (len > (fld.Length * 2))
                    {
                        if (pos >= (fld.Length * 2))
                        {
                            cbx.Text = cbx.Text.Substring(0, (fld.Length * 2));
                        }
                        else
                        {
                            cbx.Text = cbx.Text.Substring(0, pos) + cbx.Text.Substring(pos + 1, (fld.Length * 2) - pos);
                        }
                    }
                    cbx.SelectionStart = pos;

                    // 入力された値をWorkingAreaのメッセージに設定
                    string txt = cbx.Text;
                    if (txt.Length < (fld.Length * 2))
                    {
                        // 長さが足りなければ0埋め
                        txt = (new String('0', (fld.Length * 2)) + txt).Substring(txt.Length);
                    }
                    msg.SetFldValue(fld.FldId, ByteArray.ParseHex(txt));
                }
            }
        }


        private void Cbx_MessageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommMessage msg = (CommMessage)cbx_MessageType.SelectedValue;
            BlockDefine block = msg.GetBlockDefine();
            initComboBoxesLabels();
            expandBlock(block, msg);
        }

        private bool isExpanding = false;
        private void expandBlock(BlockDefine block, CommMessage msg)
        {
            isExpanding = true;
            // cbxidxを0から順番に処理していく
            int cbxidx = 0;
            BlockDefine blk = null;
            string group = string.Empty;
            int nest = 0;
            NComboBox cbx;
            while (_comboBoxes[cbxidx].Visible == true)
            {
                cbx = _comboBoxes[cbxidx];
                Object tag = cbx.Tag;

                if(tag is FieldDefine)
                {
                    if (block.GrpId.Contains(group))
                    {
                        // 展開するグループに属するならそのまま
                        cbxidx++;
                    }
                    else
                    {
                        if (nest == 0)
                        {
                            // トップレベルならそのまま
                            cbxidx++;
                        }
                        else
                        {
                            // そうでなければ閉じる
                            deleteLine(cbxidx);
                        }
                    }
                }
                else
                {
                    (blk, group) = ((BlockDefine, string))tag;
                    nest = blk.NestingLevel;
                    if (group == block.GrpId)
                    {
                        // 展開するグループなら展開処理へ進む
                        cbxidx++;
                        break;
                    }
                    if (group.Contains(block.OwnerGrpId))
                    {
                        // 展開するグループに属するならそのまま
                        cbxidx++;
                    }
                    else
                    {
                        if ( nest == 0)
                        {
                            // トップレベルならそのまま
                            cbxidx++;
                        }
                        else
                        {
                            // そうでなければ閉じる
                            deleteLine(cbxidx);
                        }
                    }
                }
            }
            // 展開するグループが展開済なら一旦閉じる
            while (true)
            {
                cbx = _comboBoxes[cbxidx];
                Object tag = cbx.Tag;
                if(tag == null)
                {
                    // 展開してなければ展開処理へ進む
                    break;
                }
                else
                {
                    if (tag is FieldDefine)
                    {
                        FieldDefine fld = (FieldDefine)tag;
                        if (fld.OwnerBlock.GrpId != block.GrpId)
                        {
                            // 展開するグループに属さなければ展開処理へ進む
                            break;
                        }
                        // そうでなければ閉じる
                        deleteLine(cbxidx);
                    }
                    else
                    {
                        (blk, group) = ((BlockDefine, string))tag;
                        if (blk.GrpId != block.GrpId)
                        {
                            // 展開するグループに属さなければ展開処理へ進む
                            break;
                        }
                        // そうでなければ閉じる
                        deleteLine(cbxidx);
                    }
                }
            }

            int level = block.NestingLevel;
            int shift = level * 20;
            // フィールドの展開
            foreach (FieldDefine fld in block.GetFields())
            {
                insertLine(cbxidx);
                string val = msg.GetFldValue(fld.FldId).to_hex();
                putField(_comboBoxes[cbxidx], _fldNameLabels[cbxidx], fld, val, _comboBoxes_left+shift);
                cbxidx++;
            }

            // ブロックの展開
            foreach (string blkid in block.GetBlockIdList())
            {
                insertLine(cbxidx);
                putBlock(_comboBoxes[cbxidx], _fldNameLabels[cbxidx], block, blkid, _comboBoxes_left+shift);
                cbxidx++;
            }

            // 未使用を非表示
            for (; cbxidx < _comboBoxes.Count; ++cbxidx)
            {
                cbx = _comboBoxes[cbxidx];
                Object tag = cbx.Tag;
                if (tag == null)
                {
                    cbx.Visible = false;
                    _fldNameLabels[cbxidx].Visible = false;
                }
                else
                {
                    if (tag is FieldDefine)
                    {
                        FieldDefine fld = (FieldDefine)tag;
                        BlockDefine ownerblk = fld.OwnerBlock;
                        if (ownerblk != null)
                        {
                            cbx.Visible = false;
                            _fldNameLabels[cbxidx].Visible = false;
                        }
                    }
                    else
                    {
                        (blk, group) = ((BlockDefine, string))tag;
                        if (blk.NestingLevel >= level)
                        {
                            cbx.Visible = false;
                            _fldNameLabels[cbxidx].Visible = false;
                        }
                    }
                }
            }
            isExpanding = false;
        }

        private void putField(NComboBox cbx, Label lbl, FieldDefine fld, string val, int left)
        {
            int ofs = left - _comboBoxes_left;
            cbx.Visible = true;
            cbx.AllowEdit = true;
            cbx.Left = left;
            cbx.Width = _comboBoxes_width_narrow - ofs;
            cbx.Tag = fld;

            // フィールドの値を設定
            cbx.ClearItems();
            CommMessageDefine comdef = CommMessageDefine.GetInstance();
            foreach(var pair in comdef.GetFldDescription(fld.FldId))
            {
                string vals = (string)pair.Key;
                string desc = (string)pair.Value;
                cbx.AddItem(desc, vals);
            }
            cbx.Text = val;

            lbl.Visible = true;
            lbl.Left = left + _comboBoxes_width_narrow - ofs;
            lbl.Text = (fld.Name != null) ? fld.Name : fld.FldId;
        }
        private void putBlock(NComboBox cbx, Label lbl, BlockDefine block, string blkid, int left)
        {
            int ofs = left - _comboBoxes_left;
            cbx.Visible = true;
            cbx.AllowEdit = false;
            cbx.Left = left;
            cbx.Width = _comboBoxes_width_wide -ofs;
            cbx.Tag = (block, blkid);
            cbx.ClearItems();
            foreach (BlockDefine blk in block.GetBlocks(blkid))
            {
                cbx.AddItem(blk.Name, blk);
            }
            cbx.SelectedIndex = 0;
            lbl.Visible = false;
        }

        private void insertLine(int idx)
        {
            for(int i = _comboBoxes.Count-1; i>idx; --i)
            {
                NComboBox dst = (NComboBox) _comboBoxes[i];
                NComboBox src = (NComboBox) _comboBoxes[i - 1];
                Label lbl = _fldNameLabels[i];
                if (src.Visible == true)
                {
                    if(src.Tag is FieldDefine)
                    {
                        putField(dst, lbl, (FieldDefine)src.Tag, src.Text, src.Left);
                    }
                    else
                    {
                        var (block, blkid) = ((BlockDefine, string))src.Tag;
                        putBlock(dst,lbl, block, blkid, src.Left);
                    }
                }
            }
        }
        private void deleteLine(int idx)
        {
            for (int i = idx; i < (_comboBoxes.Count -1); ++i)
            {
                NComboBox dst = (NComboBox)_comboBoxes[i];
                NComboBox src = (NComboBox)_comboBoxes[i + 1];
                Label lbl = _fldNameLabels[i];
                if (src.Visible == true)
                {
                    if (src.Tag is FieldDefine)
                    {
                        putField(dst, lbl, (FieldDefine)src.Tag, src.Text, src.Left);
                    }
                    else
                    {
                        var (block, blkid) = ((BlockDefine, string))src.Tag;
                        putBlock(dst, lbl, block, blkid, src.Left);
                    }
                }
                else
                {
                    dst.Visible = false;
                    dst.Tag = null;
                    lbl.Visible = false;
                }
            }
            _comboBoxes[_comboBoxes.Count - 1].Visible = false;
            _comboBoxes[_comboBoxes.Count - 1].Tag = null;
            _fldNameLabels[_comboBoxes.Count - 1].Visible = false;
        }

    }
}
