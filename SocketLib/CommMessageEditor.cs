using NCommonUtility;
using SocketLib.Properties;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;
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
        List<Button> _buttons = new List<Button>();
        List<NComboBox> _comboBoxes = new List<NComboBox>();
        List<Label> _labels = new List<Label>();
        int _width;
        int _hight;
        int _next_y;
        int _next_tabidx;

        protected CommMessage _commMsg;
        protected bool bInnerUpdate = false;
        protected int cbx_left { get; private set; }
        protected int cbx_width { get; private set; }
        protected int cbx_wide_width { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pnl">パネル</param>
        /// <param name="btn">ひな型となる（最初の1個の）展開ボタン</param>
        /// <param name="cbx">ひな型となる（最初の1個の）NComboBox</param>
        /// <param name="lbl">ひな型となる（最初の1個の）項目名ラベル</param>
        /// <param name="width">領域の幅</param>
        /// <param name="hight">１行の高さ</param>
        public CommMessageEditor(Panel pnl, Button btn, NComboBox cbx, Label lbl, int width, int hight)
        {
            _panel = pnl;
            _buttons.Add(btn);
            _comboBoxes.Add(cbx);
            _labels.Add(lbl);
            btn.Click += Btn_Clicked;
            cbx.SelectedIndexChanged += Cbx_SelectedIndexChanged;
            cbx.TextUpdate += Cbx_TextUpdate;
            cbx.TextChanged += Cbx_TextChanged;

            _width = width;
            _hight = hight;
            _next_y = cbx.Top + _hight;
            _next_tabidx = cbx.TabIndex + 3;

            cbx_left = _comboBoxes[0].Left;
            cbx_width = _comboBoxes[0].Width;
            cbx_wide_width = _width - _comboBoxes[0].Left;
        }

        private void AddLine()
        {
            int no = _comboBoxes.Count +1;

            Button btn = new Button();
            int btn_y = _next_y + (_buttons[0].Top - _comboBoxes[0].Top);
            btn.Visible = false;
            btn.Location = new System.Drawing.Point(_buttons[0].Left, btn_y);
            btn.Name = string.Format($"btn_{no:D4}");
            btn.Size = new System.Drawing.Size(_buttons[0].Width, _buttons[0].Height);
            btn.Font = _buttons[0].Font;
            btn.ForeColor = _buttons[0].ForeColor;
            btn.BackColor = _buttons[0].BackColor;
            btn.FlatStyle = _buttons[0].FlatStyle;
            btn.FlatAppearance.BorderSize = _buttons[0].FlatAppearance.BorderSize;
            btn.FlatAppearance.BorderColor = _buttons[0].FlatAppearance.BorderColor;
            btn.FlatAppearance.CheckedBackColor = _buttons[0].FlatAppearance.CheckedBackColor;
            btn.FlatAppearance.MouseOverBackColor = _buttons[0].FlatAppearance.MouseOverBackColor;
            btn.FlatAppearance.MouseDownBackColor = _buttons[0].FlatAppearance.MouseDownBackColor;
            btn.TabIndex = _next_tabidx++;
            btn.Text = "+";
            btn.Click += Btn_Clicked;
            _buttons.Add(btn);
            _panel.Controls.Add(btn);

            NComboBox cbx = new NCommonUtility.NComboBox();
            cbx.Visible = false;
            cbx.Tag = null;
            cbx.AllowEdit = true;
            cbx.FormattingEnabled = true;
            cbx.Location = new System.Drawing.Point(_comboBoxes[0].Left, _next_y);
            cbx.Name = string.Format($"cbx_{no:D4}");
            cbx.Size = new System.Drawing.Size(_comboBoxes[0].Width, _comboBoxes[0].Height);
            cbx.TabIndex = _next_tabidx++;
            cbx.SelectedIndexChanged += Cbx_SelectedIndexChanged;
            cbx.TextUpdate += Cbx_TextUpdate;
            cbx.TextChanged += Cbx_TextChanged;
            _comboBoxes.Add(cbx);
            _panel.Controls.Add(cbx);

            Label lbl = new System.Windows.Forms.Label();
            int label_y = _next_y + (_labels[0].Top - _comboBoxes[0].Top);
            lbl.Visible = false;
            lbl.AutoSize = true;
            lbl.Location = new System.Drawing.Point(_labels[0].Left, label_y);
            lbl.Name = string.Format($"lbl_{no:D4}");
            lbl.Size = new System.Drawing.Size(100, _labels[0].Height);
            lbl.TabIndex = _next_tabidx++;
            lbl.Text = lbl.Name;
            _labels.Add(lbl);
            _panel.Controls.Add(lbl);

            _next_y = _next_y + _hight;
        }

        public void SetCommMessage(CommMessage msg)
        {
            foreach (Button btn in _buttons)
            {
                btn.Visible = false;
                btn.Text = "+";
            }
            foreach (ComboBox cbx in _comboBoxes)
            {
                cbx.Visible = false;
                cbx.Tag = null;
            }
            foreach (Label lbl in _labels)
            {
                lbl.Visible = false;
            }

            _commMsg = msg;
            expandBlock(msg.GetBlockDefine(), -1, -1);
        }

        private void expandBlock(BlockDefine blk, int idx, int level)
        {
            bool bInsert = false;
            if (idx < 0)
            {
                bInsert = true;
            }
            else if (_buttons[idx].Text == "+")
            {
                bInsert = true;
                _buttons[idx].Text = "-";
            }
            foreach (FieldDefine fld in blk.GetFields())
            {
                ++idx;
                FieldObject fldobj = new FieldObject(this, level + 1, fld);
                if (bInsert)
                {
                    InsertLine(idx);
                }
                _comboBoxes[idx].Tag = fld;
                fldobj.SetValues(_buttons[idx], _comboBoxes[idx], _labels[idx]);
                _comboBoxes[idx].Text = _commMsg.GetFldValue(fld.FldId).to_hex();
            }

            foreach (string grpid in blk.GetGroupIdList())
            {
                ++idx;
                BlockObject blkobj = new BlockObject(this, level + 1, blk, grpid);
                if (bInsert)
                {
                    InsertLine(idx);
                }
                else
                {
                    ShrinkBlock(idx);
                }
                _comboBoxes[idx].Tag = blk;
                blkobj.SetValues(_buttons[idx], _comboBoxes[idx], _labels[idx]);
            }
        }

        private void ShrinkBlock(int idx)
        {
            if (_buttons[idx].Text == "+")
            {
                return;
            }
            _buttons[idx].Text = "+";
            int lev = ((FieldAndBlock)_comboBoxes[idx++].Tag).level;
            while(idx< _comboBoxes.Count)
            {
                FieldAndBlock fabobj = (FieldAndBlock)_comboBoxes[idx].Tag;
                if (fabobj == null)
                {
                    break;
                }
                else if (lev < fabobj.level)
                {
                    DeleteLine(idx);
                }
                else
                {
                    break;
                }
            }
        }

        private void InsertLine(int idx)
        {
            while ((_comboBoxes.Count - 1) <= idx)
            {
                AddLine();
            }
            for (int i = _comboBoxes.Count - 1; i > idx; --i)
            {
                int dst = i;
                int src =i - 1;
                if (_comboBoxes[src].Visible == true)
                {
                    FieldAndBlock obj = (FieldAndBlock)_comboBoxes[src].Tag;
                    _comboBoxes[dst].Tag = obj;
                    obj.SetValues(_buttons[dst], _comboBoxes[dst], _labels[dst]);
                    _buttons[dst].Text = _buttons[src].Text;
                }
            }
        }
        private void DeleteLine(int idx)
        {
            for (int i = idx; i < (_comboBoxes.Count - 1); ++i)
            {
                int dst = i;
                int src = i + 1;
                if (_comboBoxes[src].Visible == true)
                {
                    FieldAndBlock obj = (FieldAndBlock)_comboBoxes[src].Tag;
                    _comboBoxes[dst].Tag = obj;
                    obj.SetValues(_buttons[dst], _comboBoxes[dst], _labels[dst]);
                    _buttons[dst].Text = _buttons[src].Text;
                }
                else
                {
                    InitLine(dst);
                }
            }
            InitLine(_comboBoxes.Count - 1);
        }

        private void InitLine(int idx)
        {
            _buttons[idx].Visible = false;
            _buttons[idx].Text = "+";
            _comboBoxes[idx].Visible = false;
            _comboBoxes[idx].Tag = null;
            _labels[idx].Visible = false;
        }

        private void Btn_Clicked(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int idx = _buttons.IndexOf(btn);

            switch (_comboBoxes[idx].Tag)
            {
                case BlockObject blkobj:
                    if(btn.Text == "+")
                    {
                        BlockDefine blk = (BlockDefine)_comboBoxes[idx].SelectedValue;
                        expandBlock(blk, idx, blkobj.level);
                    }
                    else
                    {
                        ShrinkBlock(idx);
                    }
                    break;
            }
        }

        private void Cbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            NComboBox cbx = (NComboBox)sender;
            int idx = _comboBoxes.IndexOf(cbx);
            switch ((FieldAndBlock)cbx.Tag)
            {
                case FieldObject fldobj:
                    // 選択されたフィールド値をWorkingAreaのメッセージに設定
                    string val = (string)cbx.SelectedValue;
                    if (val != null)
                    {
                        fldobj.SetFldValue(val);
                    }
                    break;
                case BlockObject blkobj:
                    // 展開中でなければ選択されたブロックを展開する
                    if (bInnerUpdate == false)
                    {
                        BlockDefine blk = (BlockDefine)cbx.SelectedValue;
                        expandBlock(blk, idx, blkobj.level);
                    }
                    break;
            }
        }

        private void Cbx_TextUpdate(object sender, EventArgs e)
        {
            NComboBox cbx = (NComboBox)sender;
            switch ((FieldAndBlock)cbx.Tag)
            {
                case FieldObject fldobj:
                    if (cbx.SelectedIndex >= 0)
                    {
                        // 選択状態からユーザ入力があったら一旦選択値を表示する
                        cbx.Text = (string)cbx.SelectedValue;
                        cbx.SelectionStart = cbx.Text.Length;
                    }
                    break;
            }
        }

        private void Cbx_TextChanged(object sender, EventArgs e)
        {
            if(bInnerUpdate)
            {
                return;
            }
            NComboBox cbx = (NComboBox)sender;
            int idx = _comboBoxes.IndexOf(cbx);
            switch ((FieldAndBlock)cbx.Tag)
            {
                case FieldObject fldobj:
                    if (cbx.SelectedIndex < 0)
                    {
                        // 未選択の状態からユーザ入力があったら補正する
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
                        FieldDefine fld = fldobj.fld;
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
                    break;
            }
        }

        protected abstract class FieldAndBlock
        {
            public CommMessageEditor owner { get; private set; }
            public int level {  get; private set; }

            public FieldAndBlock(CommMessageEditor owner, int level)
            {
                this.owner = owner;
                this.level = level;
            }
            public abstract void SetValues(Button btn, NComboBox cbx, Label lbl);
        }

        protected class FieldObject : FieldAndBlock
        {
            public FieldDefine fld { get; private set; }
            public FieldObject(CommMessageEditor owner, int level, FieldDefine fld): base(owner, level)
            {
                this.fld = fld;
            }

            public override void SetValues(Button btn, NComboBox cbx, Label lbl)
            {
                btn.Visible = false;

                cbx.Visible = true;
                cbx.AllowEdit = true;
                cbx.Left = owner.cbx_left + (level * 20);
                cbx.Width = owner.cbx_width - (level * 20);
                cbx.Tag = this;

                // フィールドの値を設定
                owner.bInnerUpdate = true;
                cbx.ClearItems();
                CommMessageDefine comdef = CommMessageDefine.GetInstance();
                foreach (var pair in comdef.GetFldDescription(fld.FldId))
                {
                    string vals = (string)pair.Key;
                    string desc = (string)pair.Value;
                    cbx.AddItem(desc, vals);
                }
                owner.bInnerUpdate = false;
                cbx.Text = owner._commMsg.GetFldValue(fld.FldId).to_hex();

                lbl.Visible = true;
                lbl.Text = (fld.Name != null) ? fld.Name : fld.FldId;
            }
            public void SetFldValue(string val)
            {
                owner._commMsg.SetFldValue(fld.FldId, ByteArray.ParseHex(val));
            }
        }
        protected class BlockObject : FieldAndBlock
        {
            public BlockDefine block { get; private set; }
            public string grpid { get; private set; }

            public BlockObject(CommMessageEditor owner, int level, BlockDefine blk, string grpid) : base(owner, level)
            {
                this.block = blk;
                this.grpid = grpid;
            }

            public override void SetValues(Button btn, NComboBox cbx, Label lbl)
            {
                btn.Visible = true;

                cbx.Visible = true;
                cbx.AllowEdit = false;
                cbx.Left = owner.cbx_left + (level * 20);
                cbx.Width = owner.cbx_wide_width - (level * 20);
                cbx.Tag = this;
                owner.bInnerUpdate = true;
                cbx.ClearItems();
                foreach (BlockDefine blk in block.GetBlocks(grpid))
                {
                    cbx.AddItem(blk.Name, blk);
                }
                cbx.SelectedIndex = 0;
                owner.bInnerUpdate = false;
                lbl.Visible = false;
            }
        }
    }
}
