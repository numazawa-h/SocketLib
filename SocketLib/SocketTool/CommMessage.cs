﻿using NCommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NCommonUtility.JsonConfig;
using System.Xml.Linq;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class CommMessage
    {
        protected MessageDefine _head_def;
        protected MessageDefine _data_def;

        public string DName { get { return _data_def.DName; } }
        public string DType { get { return _data_def.DType; } }
        public int DLength { get { return _data_def.DLength; } }
        public int MinLength { get { return _data_def.MinLength; } }
        public int Length { get { return _data.Length; } }

        byte[] _head;
        byte[] _data;

        public CommMessage()
        {
            _data_def = null;
            _data = null;
            _head = null;
        }
        public CommMessage(CommMessage other)
        {
            _head_def = other._head_def;
            _data_def = other._data_def;
            _data = new byte[other._data.Length];
            Buffer.BlockCopy(other._data, 0, _data, 0, _data.Length);
            _head = new byte[other._head.Length];
            Buffer.BlockCopy(other._head, 0, _head, 0, _head.Length);
        }

        public CommMessage(string dtype, byte[] dat = null)
        {
            _head_def = CommMessageDefine.GetInstance().GetMessageDefine("head");
            _head = new byte[_head_def.DLength];
            SetHedValue("dtype", ByteArray.ParseHex(dtype));

            _data_def = CommMessageDefine.GetInstance().GetMessageDefine(dtype);
            InitData(dat);
            SetHedDataLength(_data.Length);
        }

        public CommMessage(byte[] hed, byte[] dat)
        {
            _head_def = CommMessageDefine.GetInstance().GetMessageDefine("head");
            _head = new byte[_head_def.DLength];
            Buffer.BlockCopy(hed, 0, _head, 0, Math.Min(_head_def.DLength, hed.Length));
            string dtype = GetHedValue("dtype").to_hex();

            _data_def = CommMessageDefine.GetInstance().GetMessageDefine(dtype);
            _data = new byte[dat.Length];
            Buffer.BlockCopy(dat, 0, _data, 0, dat.Length);
        }

        protected void InitData( byte[] dat = null)
        {
            if (dat != null)
            {
                if (_data_def.DLength < 0)
                {
                    // 可変長電文なら引数のdatで作成する
                    _data = new byte[dat.Length];
                    Buffer.BlockCopy(dat, 0, _data, 0, dat.Length);
                }
                else
                {
                    // 固定長なら定義された長さで作成する
                    _data = new byte[_data_def.DLength];
                    Buffer.BlockCopy(dat, 0, _data, 0, Math.Min(_data_def.DLength, dat.Length));
                }
            }
            else
            {
                _data = _data_def.GetDefaultValue();
            }
        }
        public void AppendData(byte[] val)
        {
            byte[] _tmp = new byte[_data.Length + val.Length];
            Buffer.BlockCopy(_data, 0, _tmp, 0, _data.Length);
            Buffer.BlockCopy(val, 0, _tmp, _data.Length, val.Length);
            _data = _tmp;
            SetHedDataLength(_data.Length);
        }

        public string GetFldDescription(string fldid)
        {
            FieldDefine fld = _data_def.GetFldDefine(fldid);
            byte[] val = GetFldValue(fldid).GetData();
            return fld.GetValueDescription(val);
        }

        public string GetDescription()
        {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (string fldid in _data_def.GetFldidList())
            {
                string desc = GetFldDescription(fldid);
                if (desc != string.Empty)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        sb.Append("(");
                    }
                    else
                    {
                        sb.Append(",");
                    }
                    sb.Append(desc);
                }
            }
            if (isFirst == false)
            {
                sb.Append(")");
            }

            return sb.ToString();
        }

        public byte[] GetHead()
        {
            int ofs = 0;
            int len = _head.Length;

            byte[] val = new byte[len];
            Buffer.BlockCopy(_head, ofs, val, 0, len);

            return val;
        }
        public byte[] GetData()
        {
            int ofs = 0;
            int len = _data.Length;

            byte[] val = new byte[len];
            Buffer.BlockCopy(_data, ofs, val, 0, len);

            return val;
        }



        public ByteArray GetHedValue(string fldid)
        {
            int ofs = _head_def.GetFldDefine(fldid).Offset;
            int len = _head_def.GetFldDefine(fldid).Length;
            return new ByteArray(_head, ofs, len);
        }
        public void SetHedValue(string fldid, ByteArray val)
        {
            SetHedValue(fldid, val.GetData());
        }
        public void SetHedValue(string fldid, Byte[] val)
        {
            int ofs = _head_def.GetFldDefine(fldid).Offset;
            int len = _head_def.GetFldDefine(fldid).Length;
            Array.Clear(_head, ofs, len);
            Buffer.BlockCopy(val, 0, _head, ofs, Math.Min(val.Length, len));
        }
        public void SetHedValue(string fldid, ulong val)
        {
            int len = _head_def.GetFldDefine(fldid).Length;
            ByteArray fldvalue;
            switch (len)
            {
                case 1:
                    fldvalue = new ByteArray((Byte)val);
                    break;
                case 2:
                    fldvalue = new ByteArray((UInt16)val);
                    break;
                case 4:
                    fldvalue = new ByteArray((UInt32)val);
                    break;
                case 8:
                    fldvalue = new ByteArray((UInt64)val);
                    break;
                default:
                    throw new Exception($"フィールド長が {len}なので整数を設定できません({fldid})");
            }
            SetHedValue(fldid, fldvalue);
        }
        public void SetHedDataLength(int len)
        {
            if (_head_def.ContainsKey("dlen"))
            {
                SetHedValue("dlen", (ulong)len);
            }
            if (_head_def.ContainsKey("plen"))
            {
                SetHedValue("plen", (ulong)(len + _head_def.DLength));
            }
        }

        public bool ContainsFldKey(string key)
        {
            return _data_def.ContainsKey(key);
        }
        public BlockDefine GetBlockDefine()
        {
            return _data_def.BlockDefine;
        }
        public string[] GetFldidList()
        {
            return _data_def.GetFldidList();
        }

        public ByteArray GetFldValue(string fldid)
        {
            int ofs = _data_def.GetFldDefine(fldid).Offset;
            int len = _data_def.GetFldDefine(fldid).Length;
            return new ByteArray(_data, ofs, len);
        }
        public void SetFldValue(string fldid, ByteArray val)
        {
            SetFldValue(fldid, val.GetData());
        }
        public void SetFldValue(string fldid, byte[] val)
        {
            int ofs = _data_def.GetFldDefine(fldid).Offset;
            int len = _data_def.GetFldDefine(fldid).Length;
            Array.Clear(_data, ofs, len);
            Buffer.BlockCopy(val, 0, _data, ofs, Math.Min(val.Length, len));
        }
        public void SetFldValue(string fldid, ulong val)
        {
            int len = _data_def.GetFldDefine(fldid).Length;
            ByteArray fldvalue;
            switch (len)
            {
                case 1:
                    fldvalue = new ByteArray((Byte)val);
                    break;
                case 2:
                    fldvalue = new ByteArray((UInt16)val);
                    break;
                case 4:
                    fldvalue = new ByteArray((UInt32)val);
                    break;
                case 8:
                    fldvalue = new ByteArray((UInt64)val);
                    break;
                default:
                    throw new Exception($"フィールド長が {len}なので整数を設定できません({fldid})");
            }
            SetFldValue(fldid, fldvalue);
        }

        public static CommMessage LoadFileBinary(string path)
        {
            CommMessage msg = null;
            string fname = System.IO.Path.GetFileName(path);
            int dtypelen = CommMessageDefine.GetInstance().GetMessageDefine("head").GetFldDefine("dtype").Length;
            string dtype = fname.Substring(0, dtypelen * 2);
            if (CommMessageDefine.GetInstance().Contains(dtype) == false)
            {
                throw new Exception($"dtype'{dtype}'が定義されていません");
            }
            using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                byte[] dat = new byte[fs.Length];
                fs.Read(dat, 0, dat.Length);
                msg = new CommMessage(dtype, dat);
            }

            return msg;
        }

        public static CommMessage LoadFileText(string path)
        {
            CommMessage msg = null;

            byte[] hed = System.Array.Empty<byte>();
            byte[] dat = System.Array.Empty<byte>();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(path))
            {
                string filetext = sr.ReadToEnd();

                // コメントと空白を削除
                Regex reg_comment = new Regex("//.*\\n");
                Regex reg_whitesp = new Regex("[\\r\\n\\s\\t]");
                string txt = reg_whitesp.Replace(reg_comment.Replace(filetext, ""), "");

                // [head][data]形式にマッチング
                var matchs = Regex.Matches(txt, @"\[[0-9,a-f,A-F ]*\]");
                if (matchs.Count != 2)
                {
                    throw new Exception($"フォーマットが[head][data]の形式ではありません");
                }

                // ヘッダとデータのHEX文字列から生成
                Regex r = new Regex("[\\[\\] \\t]");
                string hed_hex = r.Replace(matchs[0].Value, "");
                string dat_hex = r.Replace(matchs[1].Value, "");
                int dtypelen = CommMessageDefine.GetInstance().GetMessageDefine("head").GetFldDefine("dtype").Length;
                if (hed_hex.Length == (dtypelen * 2))
                {
                    // データ種別指定の時
                    string dtype = hed_hex;
                    dat = ByteArray.ParseHex(dat_hex);
                    msg = new CommMessage(dtype, dat);
                }
                else
                {
                    // ヘッダ全体指定の時
                    hed = ByteArray.ParseHex(hed_hex);
                    dat = ByteArray.ParseHex(dat_hex);
                    msg = new CommMessage(hed, dat);
                }
            }

            return msg;
        }

    }
}
