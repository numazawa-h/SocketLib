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

        public ByteArray() 
        {
            _dat = System.Array.Empty<byte>();
        }

        public ByteArray(byte[] dat)
        {
            _dat = new byte[dat.Length];
            Buffer.BlockCopy(dat, 0, _dat, 0, dat.Length);
        }

        /// <summary>
        /// コピーコンストラクタ
        /// </summary>
        /// <remarks>
        /// データの長さに0を指定した場合、コピー元全体が対象になります。
        /// データの長さにマイナスを指定した場合、オフセットからの相対データが対象となります。
        /// データの長さにマイナスを指定した場合、オフセットが0なら末尾データが対象となります。
        /// コピー元データのオフセットにマイナスを指定した場合、コピー先のオフセットがずれます。
        /// </remarks>
        /// <param name="othher">コピー元オブジェクト</param>
        /// <param name="ofs">コピー元データのオフセット</param>
        /// <param name="len">データの長さ</param>
        public ByteArray(ByteArray othher, int ofs=0, int len=0)
        {
            if (len < 0)
            {
                len = -len;
                if (ofs == 0)
                {
                    // 長さがマイナスでオフセットが0なら末尾のデータを対象とする
                    ofs = othher._dat.Length - len;
                }
                else
                {
                    // 長さがマイナスでオフセットが0以外ならofsより前のデータを対象とする
                    ofs -= len;
                }
            }
            if (len == 0)
            {
                len = othher._dat.Length - ofs;
            }
            _dat = new byte[len];

            int dst = 0;
            if (ofs < 0)
            {
                // オフセットがマイナスならコピー先の先頭をずらす
                dst = -ofs;
                len = len - dst;
                ofs = 0;
            }
            if ((ofs+len) > othher._dat.Length)
            {
                // コピー元のデータの長さが指定の長さより短ければコピー元の長さに合わせる
                // （残りの部分は0x00で初期化される）
                len = othher._dat.Length - ofs;
            }
            if(len > 0)
            {
                // 最終的にコピーすべきデータがあればコピーする
                Buffer.BlockCopy(othher._dat, ofs, _dat, dst, len);
            }
        }

        public ByteArray(string text, Encoding enc=null )
        {
            if(enc==null)
            {
                // C#の文字列は、UTF16(リトルエンディアン)なので、デフォルトはUnicode
                enc = Encoding.Unicode;
            }
            _dat = enc.GetBytes(text);
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

        public override string ToString() 
        { 
            return to_hex();
        }

        /// <summary>
        /// 16進文字列への変換
        /// </summary>
        /// <param name="ofs">offset</param>
        /// <param name="len">length</param>
        /// <param name="sep">separator</param>
        /// <returns><16進文字列/returns>
        public string to_hex(int ofs=0, int len = 0, string sep=null)
        {
            StringBuilder sb = new StringBuilder();
            if (len == 0)
            {
                len = _dat.Length-ofs;
            }
            bool bFirst = true;
            for (int i = 0; i < len; i++, ofs++)
            {
                if (sep != null)
                {
                    if (bFirst)
                    {
                        bFirst = false;
                    }
                    else
                    {
                        sb.Append(sep);
                    }
                }
                sb.Append($"{_dat[ofs]:X2}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// ASCII文字列への変換
        /// </summary>
        /// <param name="ofs">offset</param>
        /// <param name="len">length</param>
        /// <param name="sep">separator</param>
        /// <returns>表示できない文字はピリオド(".")に変換</returns>
        public string to_text_ascii(int ofs = 0, int len = 0, string sep = null)
        {
            StringBuilder sb = new StringBuilder();
            if (len == 0)
            {
                len = _dat.Length - ofs;
            }
            bool bFirst = true;
            for (int i = 0; i < len; i++, ofs++)
            {
                if (sep != null)
                {
                    if (bFirst)
                    {
                        bFirst = false;
                    }
                    else
                    {
                        sb.Append(sep);
                    }
                }
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

        public string to_text_sjis(int ofs = 0, int len = 0)
        {
            return to_text(Encoding.GetEncoding("Shift_JIS"), ofs, len);
        }

        public string to_text_utf8(int ofs = 0, int len = 0)
        {
            return to_text(Encoding.UTF8, ofs, len);
        }

        public string to_text_unicode(int ofs = 0, int len = 0)
        {
            return to_text(Encoding.Unicode, ofs, len);
        }

        public string to_text(int ofs = 0, int len = 0)
        {
            // C#の文字列は、UTF16(リトルエンディアン)なので、デフォルトはUnicode
            return to_text(Encoding.Unicode, ofs, len);
        }

        /// <summary>
        /// 指定された文字コードで文字列に変換
        /// </summary>
        /// <param name="enc">encoding</param>
        /// <param name="ofs">offset</param>
        /// <param name="len">length</param>
        /// <returns>文字列</returns>
        public string to_text(Encoding enc, int ofs = 0, int len = 0)
        {
            if (len == 0)
            {
                len = _dat.Length - ofs;
            }
            byte[] b = new byte[len];
            Buffer.BlockCopy(_dat, ofs, b, 0, len);
            return enc.GetString(b);
        }

        static public byte[] ParseHex(string hex)
        {
            // １６進表記の文字以外を削除（スペース等）
            Regex r = new Regex("[^0-9a-fA-F]");
            string h = r.Replace(hex, "");

            // 奇数文字ならエラー
            if ((h.Length % 2) == 1)
            {
                return System.Array.Empty<byte>();
            }

            int byte_size = h.Length / 2;
            byte[] buf = new byte[byte_size];

            int buf_idx = 0;
            for (int idx = 0; idx < h.Length; idx += 2)
            {
                string w = h.Substring(idx, 2);
                buf[buf_idx] = Convert.ToByte(w, 16);
                buf_idx++;
            }

            return buf;
        }

    }
}
