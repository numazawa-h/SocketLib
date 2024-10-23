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
    public class CommDataDefine
    {
        // シングルトン
        static private CommDataDefine _instance = null;
        static public CommDataDefine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CommDataDefine();
            }
            return _instance;
        }
        private CommDataDefine():base()
        {
        }

        // 通信メッセージ定義
        protected Dictionary<string, CommMessageDefine> _message_def = new Dictionary<string, CommMessageDefine>();

        // データの値の説明定義
        protected Dictionary<string, ValuesDefine> _values_def = new Dictionary<string, ValuesDefine>();

        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);

            _values_def.Clear();
            foreach (Node def in root["values-def"])
            {
                _values_def.Add(def["id"].ToString(), new ValuesDefine(def));
            }

            _message_def.Clear();
            foreach (Node node in root["message-def"])
            {
                _message_def.Add(node["id"].ToString(), new CommMessageDefine(node) );
            }
        }

        public CommMessageDefine GetMessageDefine(string dtype)
        {
            if (_message_def.ContainsKey(dtype)==false)
            {
                throw new Exception($"定義されていないデータ種別({dtype})");
            }
            return new CommMessageDefine(_message_def[dtype]);
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

        public ValuesDefine GetValues(string id)
        {
            return _values_def[id];
        }

        public class CommMessageDefine
        {
            string _dtype;
            string _name;
            int _data_len;
            int _data_minlen;

            public string DType { get { return _dtype; } }
            public string Name { get { return _name; } }
            public int Length {  get { return _data_len; } }
            public int MinLength { get { return _data_minlen; } }

            Dictionary<string, FieldDefine> _fld_def_list = new Dictionary<string, FieldDefine>();
            public Dictionary<string, FieldDefine> Fld_List { get {  return _fld_def_list; } }  

            public CommMessageDefine(Node def)
            {
                _dtype = def["id"];
                _name = def["name"];
                _data_len = def["len"];
                _data_minlen = (int?)def["minlen"] is int v ? v : 0;

                foreach (Node node in def["flds"])
                {
                    _fld_def_list.Add(node["id"], new FieldDefine(node));
                }
            }


            // コピーコンストラクタ
            public CommMessageDefine(CommMessageDefine other)
            {
                _dtype = other._dtype;
                _name = other._name;
                _data_len = other._data_len;
                _data_minlen = other._data_minlen;
                foreach (KeyValuePair<string, FieldDefine> pair in other._fld_def_list)
                {
                    _fld_def_list.Add(pair.Key, pair.Value);
                }
            }

            public void AddFldDefine(string id, FieldDefine fld)
            {
                _fld_def_list.Add(id, fld);
            }


            public string GetFldName(string fldid)
            {
                return _fld_def_list[fldid].FldName;
            }

            public int GetFldOffset(string fldid)
            {
                return _fld_def_list[fldid].FldOffset;
            }

            public int GetFldLength(string fldid)
            {
                return _fld_def_list[fldid].FldLength;
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
                    if (CommDataDefine.GetInstance()._values_def.ContainsKey(FldId))
                    {
                        FldName = CommDataDefine.GetInstance()._values_def[FldId].FldName;
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
