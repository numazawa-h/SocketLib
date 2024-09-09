using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NCommonUtility
{
    internal class ByteArray
    {
        private byte[] _dat;
        private string _separator_at04 = "";
        private string _separator_at08 = "";
        private string _separator_at16 = "";

        public ByteArray(string hex_strings)
        {
            // １６進文字列だけに変換（スペース等余計な文字を削除）
            Regex r = new Regex("[^0-9a-fA-F]");
            string bcd = r.Replace(hex_strings, "");

            // １６進文字列をバイナリーに変換
            _dat = ParseBcd(bcd);
        }

        public ByteArray(byte[] dat) 
        {
            _dat = new byte[dat.Length];
            Buffer.BlockCopy(dat, 0, _dat, 0, dat.Length);
        }

        public void Append(ByteArray other)
        {
            Append(other.GetData());
        }


        public void Append(byte[] other)
        {
            byte[] buf = new byte[_dat.Length + other.Length];
            Buffer.BlockCopy(_dat, 0, buf, 0, _dat.Length);
            Buffer.BlockCopy(other, 0, buf, _dat.Length, other.Length);
            _dat = buf;
        }

        public byte[] GetData()
        {
            byte[] buf = new byte[_dat.Length];
            Buffer.BlockCopy(_dat, 0, buf, 0, _dat.Length);

            return buf;
        }

        public  void SetSeparator(string sep04, string sep08=null, string sep16=null)
        {
            _separator_at04 = (sep04 == null) ? "" : sep04;
            _separator_at08 = (sep08 == null) ? "" : sep08;
            _separator_at16 = (sep16 == null) ? "" : sep16;
        }

        public override string ToString() 
        { 
            StringBuilder sb = new StringBuilder();
            int cnt = 0;
            foreach (byte b in _dat)
            {
                sb.Append($"{b:X2}");
                ++cnt;

                // 後続データがあれば区切り文字判定をする
                if (cnt < _dat.Length)
                {
                    if ((cnt % 16) == 0)
                    {
                        sb.Append(_separator_at16);
                    }
                    else if ((cnt % 8) == 0)
                    {
                        sb.Append(_separator_at08);
                    }
                    else if ((cnt % 4) == 0)
                    {
                        sb.Append(_separator_at04);
                    }
                }
            }
            return sb.ToString();
        }

        public string to_hex(int ofs=0, int len = 0)
        {
            if(len == 0)
            {
                len = _dat.Length-ofs;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++, ofs++)
            {
                sb.Append($"{_dat[ofs]:X2}");
            }

            return sb.ToString();
        }

        public string to_text(int ofs = 0, int len = 0)
        {
            if (len == 0)
            {
                len = _dat.Length - ofs;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++, ofs++)
            {
                byte b = _dat[ofs];
                if (b < 0x20 || b>0x7e)
                {
                    sb.Append(".");
                }
                else
                {
                    sb.Append((char)b);
                }
            }

            return sb.ToString();
        }

        static public byte[] ParseBcd(string bcd)
        {
            // 奇数文字ならエラー
            if ((bcd.Length % 2) == 1)
            {
                return System.Array.Empty<byte>();
            }

            int byte_size = bcd.Length / 2;
            byte[] buf = new byte[byte_size];

            int buf_idx = 0;
            for (int idx = 0; idx < bcd.Length; idx += 2)
            {
                string w = bcd.Substring(idx, 2);
                buf[buf_idx] = Convert.ToByte(w, 16);
                buf_idx++;
            }

            return buf;
        }

    }
}
