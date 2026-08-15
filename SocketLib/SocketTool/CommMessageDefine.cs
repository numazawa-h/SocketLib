using NCommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Linq;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
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
        private CommMessageDefine() : base()
        {
        }

        // 通信メッセージ定義
        protected Dictionary<string, MessageDefine> _message_def = new Dictionary<string, MessageDefine>();

        // データの値の説明定義
        protected Dictionary<string, ValuesDefine> _values_def = new Dictionary<string, ValuesDefine>();

        public Dictionary<string, MessageDefine> ReadJson(string path, Dictionary<string, ValuesDefine> values_def)
        {
            RootNode root = JsonConfig.ReadJson(path);

            // メッセージ定義読み込み
            _message_def.Clear();
            foreach (Node def in root["message-def"])
            {
                try
                {
                    _message_def.Add(((string)def["id"]).ToLower(), new MessageDefine(def));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"CommMessageDefineで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }
            return _message_def;
        }

        public bool Contains(string dtype)
        {
            dtype = dtype.ToLower();
            return _message_def.ContainsKey(dtype);
        }

        public MessageDefine GetMessageDefine(string dtype)
        {
            dtype = dtype.ToLower();
            if (_message_def.ContainsKey(dtype) == false)
            {
                throw new Exception($"定義されていないデータ種別({dtype})");
            }
            return new MessageDefine(_message_def[dtype]);
        }

        public ValuesDefine GetValuesDefine(string id)
        {
            if ( _values_def.ContainsKey(id) == false)
            {
                return null;
            }
            return _values_def[id];
        }
    }
}
