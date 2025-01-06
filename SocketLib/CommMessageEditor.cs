using NCommonUtility;
using SocketLib.Properties;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SocketTool.CommMessageDefine;

namespace SocketLib
{
    /// <summary>
    /// 通信メッセージを画面上で編集するためのクラス
    /// </summary>
    internal class CommMessageEditor
    {
        Panel _panel;
        List<NComboBox> _comboBoxes = new List<NComboBox>();
        List<Label> _labels = new List<Label>();
        int _comboBoxes_left;
        int _comboBoxes_hight;
        int _comboBoxes_width_narrow;
        int _comboBoxes_width_wide;
        int _label_left;
        int _label_hight;
        int _hight;
        int _next_y;
        int _label_y;
        int _next_tabidx;

        CommMessage _commMsg;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pnl">パネル</param>
        /// <param name="cbx">ひな型となる（最初の1個の）NComboBox</param>
        /// <param name="lbl">ひな型となる（最初の1個の）Label</param>
        /// <param name="width">領域の幅</param>
        /// <param name="hight">１行の高さ</param>
        public CommMessageEditor(Panel pnl, NComboBox cbx, Label lbl, int width, int hight)
        {
            _panel = pnl;
            _comboBoxes.Add(cbx);
            _labels.Add(lbl);

            _comboBoxes_left = cbx.Left;
            _comboBoxes_hight = cbx.Height;
            _comboBoxes_width_narrow = cbx.Width;
            _comboBoxes_width_wide = width - _comboBoxes_left;
            _label_left = lbl.Left;
            _label_hight = lbl.Height;
            _hight = hight;
            _next_y = cbx.Top + _hight;
            _label_y = lbl.Top - cbx.Top;
            _next_tabidx = cbx.TabIndex + 2;
        }

        private void AddLine()
        {
            int no = _comboBoxes.Count +1;

            NComboBox cbx= new NCommonUtility.NComboBox();
            cbx.Visible = false;
            cbx.Tag = null;
            cbx.AllowEdit = true;
            cbx.FormattingEnabled = true;
            cbx.Location = new System.Drawing.Point(_comboBoxes_left, _next_y);
            cbx.Name = string.Format($"cbx_{no:D4}");
            cbx.Size = new System.Drawing.Size(_comboBoxes_width_narrow, _comboBoxes_hight);
            cbx.TabIndex = _next_tabidx++;
            //cbx.SelectedIndexChanged += Cbx_SelectedIndexChanged;
            cbx.DropDownClosed += Cbx_SelectedIndexChanged;
            cbx.TextUpdate += Cbx_TextUpdate;
            cbx.TextChanged += Cbx_TextChanged;

            _comboBoxes.Add(cbx);
            _panel.Controls.Add(cbx);

            Label lbl = new System.Windows.Forms.Label();
            lbl.Visible = false;
            lbl.AutoSize = true;
            lbl.Location = new System.Drawing.Point(_label_left, _next_y + _label_y);
            lbl.Name = string.Format($"lbl_{no:D4}");;
            lbl.Size = new System.Drawing.Size(100, _label_hight);
            lbl.TabIndex = _next_tabidx++;
            lbl.Text = lbl.Name;
            _labels.Add(lbl);
            _panel.Controls.Add(lbl);

            _next_y = _next_y + _hight;
        }

        public void SetCommMessage(CommMessage msg)
        {
            _commMsg = msg;
            foreach (ComboBox cbx in _comboBoxes)
            {
                cbx.Visible = false;
                cbx.Tag = null;
            }

            foreach (Label lbl in _labels)
            {
                lbl.Visible = false;
            }
            expandBlock(msg.GetBlockDefine());
        }

        private void Cbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            NComboBox cbx = (NComboBox)sender;
            int idx = _comboBoxes.IndexOf(cbx);

            Object tag = cbx.Tag;
            if (tag is FieldDefine)
            {
                // 選択されたフィールド値をWorkingAreaのメッセージに設定
                string val = (string)cbx.SelectedValue;
                if (val != null)
                {
                    FieldDefine fld = (FieldDefine)tag;
                    _commMsg.SetFldValue(fld.FldId, ByteArray.ParseHex(val));
                }
            }
            else
            {
                if (isExpanding == false)
                {
                    // ブロックのインデックスを選択
                    BlockDefine block = (BlockDefine)cbx.SelectedValue;
                    expandBlock(block);
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
                    _commMsg.SetFldValue(fld.FldId, ByteArray.ParseHex(txt));
                }
            }
        }
        private bool isExpanding = false;
        private void expandBlock(BlockDefine block)
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

                if (tag is FieldDefine)
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
            }
            // 展開するグループが展開済なら一旦閉じる
            while (true)
            {
                cbx = _comboBoxes[cbxidx];
                Object tag = cbx.Tag;
                if (tag == null)
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
                string val = _commMsg.GetFldValue(fld.FldId).to_hex();
                putField(_comboBoxes[cbxidx], _labels[cbxidx], fld, val, _comboBoxes_left + shift);
                cbxidx++;
            }

            // ブロックの展開
            foreach (string blkid in block.GetBlockIdList())
            {
                insertLine(cbxidx);
                putBlock(_comboBoxes[cbxidx], _labels[cbxidx], block, blkid, _comboBoxes_left + shift);
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
                    _labels[cbxidx].Visible = false;
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
                            _labels[cbxidx].Visible = false;
                        }
                    }
                    else
                    {
                        (blk, group) = ((BlockDefine, string))tag;
                        if (blk.NestingLevel >= level)
                        {
                            cbx.Visible = false;
                            _labels[cbxidx].Visible = false;
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
            foreach (var pair in comdef.GetFldDescription(fld.FldId))
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
            cbx.Width = _comboBoxes_width_wide - ofs;
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
            while ((_comboBoxes.Count - 1) <= idx)
            {
                AddLine();
            }
            for (int i = _comboBoxes.Count - 1; i > idx; --i)
            {
                NComboBox dst = (NComboBox)_comboBoxes[i];
                NComboBox src = (NComboBox)_comboBoxes[i - 1];
                Label lbl = _labels[i];
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
            }
        }
        private void deleteLine(int idx)
        {
            for (int i = idx; i < (_comboBoxes.Count - 1); ++i)
            {
                NComboBox dst = (NComboBox)_comboBoxes[i];
                NComboBox src = (NComboBox)_comboBoxes[i + 1];
                Label lbl = _labels[i];
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
            _labels[_comboBoxes.Count - 1].Visible = false;
        }


    }
}
