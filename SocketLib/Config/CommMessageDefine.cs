using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.UI;
using static NCommonUtility.JsonConfig;

namespace SampleMain.Config
{
    public class CommMessageDefine
    {
        // シングルトン
        static private CommMessageDefine _instance = null;
        static public CommMessageDefine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CommMessageDefine();
            }
            return _instance;
        }
        private CommMessageDefine():base()
        {
        }

        // 通信メッセージ定義
        protected Dictionary<string, MessageDefine> _message_def = new Dictionary<string, MessageDefine>();

        // データの値の説明定義
        protected Dictionary<string, ValuesDefine> _values_def = new Dictionary<string, ValuesDefine>();

        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);

            // 先に値定義を読む（フィールド定義に"name"がなければ値定義の"name"をフィールド名とするため）
            _values_def.Clear();
            foreach (Node def in root["values-def"])
            {
                _values_def.Add(def["id"], new ValuesDefine(def));
            }

            // メッセージ定義読み込み
            _message_def.Clear();
            foreach (Node def in root["message-def"])
            {
                _message_def.Add(def["id"], new MessageDefine(def) );
            }
        }

        public MessageDefine GetMessageDefine(string dtype)
        {
            if (_message_def.ContainsKey(dtype)==false)
            {
                throw new Exception($"定義されていないデータ種別({dtype})");
            }
            return new MessageDefine(_message_def[dtype]);
        }

        public ValuesDefine GetValuesDefine(string id)
        {

            return _values_def[id];
        }

        public string GetValueDescription(string fldid, string val)
        {
            string valid = fldid;
            if (valid.Contains("_"))
            {
                valid = valid.Substring(0, valid.IndexOf("_"));
            }
            if (_values_def.ContainsKey(valid))
            { 
                return _values_def[valid][val];
            }

            return "？？？";
        }

        /// <summary>
        /// 通信電文定義
        /// </summary>
        public class MessageDefine
        {
            // 電文種別
            public string DType { get; private set; }
            // 電文名
            public string DName { get; private set; }
            // データ長（可変長の時、-1）
            public int DLength {  get; private set; }
            // データ長（可変長の時の固定部の長さ）
            public int MinLength { get; private set; }

            // フィールド定義
            private Dictionary<string, FieldDefine> _fields_def = new Dictionary<string, FieldDefine>();  

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="def">Json定義</param>
            public MessageDefine(Node def)
            {
                DType = def["id"];
                DName = def["name"];
                DLength = def["len"];
                MinLength = (int?)def["minlen"] is int v ? v : 0;

                foreach (Node node in def["flds"])
                {
                    _fields_def.Add(node["id"], new FieldDefine(node));
                }
            }

            // コピーコンストラクタ
            public MessageDefine(MessageDefine other)
            {
                DType = other.DType;
                DName = other.DName;
                DLength = other.DLength;
                MinLength = other.MinLength;
                foreach (KeyValuePair<string, FieldDefine> pair in other._fields_def)
                {
                    _fields_def.Add(pair.Key, pair.Value);
                }
            }

            public string[] GetFldNames()
            {
                return _fields_def.Keys.ToArray();
            }

            public string GetFldName(string fldid)
            {
                return _fields_def[fldid].FldName;
            }

            public int GetFldOffset(string fldid)
            {
                return _fields_def[fldid].FldOffset;
            }

            public int GetFldLength(string fldid)
            {
                return _fields_def[fldid].FldLength;
            }
        }

        public class FieldDefine
        {
            public string FldId { get; private set; }
            public string FldName { get; private set; }
            public int FldLength { get; private set; }
            public int FldOffset { get; private set; }
            public bool isDispDesc { get; private set; }

            public FieldDefine(Node def)
            {
                FldId = def["id"];
                FldOffset = def["ofs"];
                FldLength = def["len"];
                isDispDesc = (bool?)def["disp"] is bool v ? v : false;

                if (def.ContainsKey("name"))
                {
                    FldName = def["name"];
                }
                else
                {
                    if (CommMessageDefine.GetInstance()._values_def.ContainsKey(FldId))
                    {
                        FldName = CommMessageDefine.GetInstance()._values_def[FldId].FldName;
                    }
                    else
                    {
                        FldName = FldId;
                    }
                }
            }

            /// <summary>
            /// データ長変更
            /// </summary>
            /// <remarks>可変長データの時、後から設定する</remarks>
            /// <param name="len"></param>
            public void SetFldLength(int len)
            {
                FldLength = len;
            }
        }


        public class ValuesDefine
        {
            public string FldId { get; private set; }

            public string FldName { get; private set; }

            Dictionary<string, string> _values_def = null;
            Node _format_def = null;

            public ValuesDefine(Node def)
            {
                FldId = def["id"];
                FldName = def["name"];

                if( def.ContainsKey("values"))
                {
                    _values_def = def["values"].GetDict();
                }
                if (def.ContainsKey("format"))
                {
                    _format_def = def["format"];
                }
            }

            /// <summary>
            /// 値に対応する説明を返却
            /// </summary>
            /// <param name="val">値（16進文字列）</param>
            /// <returns>値の説明</returns>
            public string this[string val]
            {
                get
                {
                    if (_values_def.ContainsKey(val))
                    {
                        // 値の一覧に存在すれば対応する値を返却する
                        return _values_def[val];
                    }
                    if (_format_def.ContainsKey("type"))
                    {
                        // データ種類別のフォーマッティング
                        string fmt =_format_def["fmt"];
                        switch ((string)_format_def["type"])
                        {
                            case "int":
                                int valint = new ByteArray(val).to_int();
                                int min = (int?)_format_def["minvalue"] is int v1? v1:int.MinValue;
                                int max = (int?)_format_def["maxvalue"] is int v2? v2:int.MaxValue;
                                if (valint < min || valint > max)
                                {
                                    return string.Empty;
                                }
                                return string.Format(fmt, valint);
                            case "datetime":
                                DateTime valdt = new ByteArray(val).to_dateTime();
                                return valdt.ToString(fmt);
                            case "image":
                                // ファイルパスを組み立てる
                                return string.Format(fmt, val);
                        }
                    }
                    return "？？？";
                }
            }
            public string[] Values
            {
                get { return _values_def.Keys.ToArray<string>(); }
            }
        }
    }
}
