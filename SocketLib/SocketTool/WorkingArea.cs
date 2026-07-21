using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class WorkingArea
    {
        protected Dictionary<string, int> _ivalues = new Dictionary<string, int>();
        protected Dictionary<string, byte[]> _bvalues = new Dictionary<string, byte[]>();
        protected HashSet<string> _incriment_values = new HashSet<string>();
        protected Dictionary<string, CommMessage> _commMessages = new Dictionary<string, CommMessage>();
        protected Dictionary<string, CommMessage> _commMessagesInit = new Dictionary<string, CommMessage>();
        protected Dictionary<string, string> _commMessagesDisp = new Dictionary<string, string>();

        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);

            _ivalues.Clear();
            _bvalues.Clear();
            _incriment_values.Clear();
            foreach (var pair in root["Working-area"].GetPropertyValues())
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
                try
                {
                    switch (value.GetValueKind())
                    {
                        case JsonValueKind.String:
                            string sval = value.ToString();
                            _bvalues.Add(key, ByteArray.StrToByte(sval));
                            break;
                        case JsonValueKind.Number:
                            int ival = value.GetValue<int>();
                            if (key.Substring(0, 2) == "++")
                            {
                                // インクリメント処理サポート(取得するたびにカウントアップする)
                                key = key.Substring(2);
                                _incriment_values.Add(key);
                            }
                            _ivalues.Add(key, ival);
                            break;
                        default:
                            throw new Exception($"数値と文字列以外が指定されました");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ScriptDefineのvalues('{key}')で読み込みエラー in {path}", ex);
                }
            }

            _commMessages.Clear();
            _commMessagesInit.Clear();
            _commMessagesDisp.Clear();
            foreach (Node node in root["Working-area"].GetPropertyObjects())
            {
                string id = node._name;
                try
                {
                    node.AddValue("id", id);      // Commandクラスが'id'必須なので追加しておく
                    CommandSend cmd = new CommandSend(node);
                    CommMessage msg = cmd.GetMessage();
                    _commMessages.Add(id, msg);
                    _commMessagesInit.Add(id, new CommMessage(msg));
                    string display = node["name"];
                    if (display == null)
                    {
                        display = msg.DName;
                    }
                    _commMessagesDisp.Add(display, id);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ScriptDefineのvalues('{id}')で読み込みエラー in {path}", ex);
                }
            }
        }

        public string[] GetValueMsgKeyList()
        {
            return _commMessages.Keys.ToArray();
        }
        public CommMessage GetValueMsg(string id)
        {
            return _commMessages[id];
        }
        public string GetValueMsgDisp(string id)
        {
            string disp = string.Empty;
            foreach (var pair in _commMessagesDisp)
            {
                if (pair.Value == id)
                {
                    disp = pair.Key;
                    break;
                }
            }
            return disp;
        }
        public bool ContainsKeyIntValue(string name)
        {
            return _ivalues.ContainsKey(name);
        }
        public int GetIntValue(string name)
        {
            if (_ivalues.ContainsKey(name) == false)
            {
                throw new Exception($"ScriptDefineに定義されていないvalues('{name}')を参照しました");
            }
            if (_incriment_values.Contains(name))
            {
                _ivalues[name] = _ivalues[name] + 1;
            }
            return _ivalues[name];
        }
        public void SetIntValue(string name, int val)
        {
            _ivalues[name] = val;
        }

        public bool ContainsKeyByteValue(string name)
        {
            return _bvalues.ContainsKey(name);
        }
        public byte[] GetByteValue(string name)
        {
            if (_bvalues.ContainsKey(name) == false)
            {
                throw new Exception($"ScriptDefineに定義されていないvalues('{name}')を参照しました");
            }
            return _bvalues[name];
        }
        public void SetByteValue(string name, string val)
        {
            _bvalues[name] = ByteArray.StrToByte(val);
        }
        public void SetByteValue(string name, byte[] val)
        {
            _bvalues[name] = val;
        }

        public CommMessage InitMessage(string disp)
        {
            string id = _commMessagesDisp[disp];
            _commMessages[id] = new CommMessage(_commMessagesInit[id]);
            return _commMessages[id];
        }

        public CommMessage LoadMessage(string disp, CommMessage msg)
        {
            string id = _commMessagesDisp[disp];
            _commMessages[id] = msg;
            return msg;
        }
    }
}
