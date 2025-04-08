using NCommonUtility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class ScriptDefine
    {
        // シングルトン
        static private ScriptDefine _instance = null;
        static public ScriptDefine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ScriptDefine();
            }
            return _instance;
        }
        private ScriptDefine() : base()
        {
        }

        protected Dictionary<string, int> _ivalues = new Dictionary<string, int>();
        protected Dictionary<string, byte[]> _bvalues = new Dictionary<string, byte[]>();
        protected HashSet<string> _incriment_values = new HashSet<string>();
        protected Dictionary<string, CommMessage> _commMessages = new Dictionary<string, CommMessage>();
        protected Dictionary<string, CommMessage> _commMessagesInit = new Dictionary<string, CommMessage>();
        protected Dictionary<string, string> _commMessagesDisp = new Dictionary<string, string>();
        protected Dictionary<string, Command> _comands = new Dictionary<string, Command>();
        protected Dictionary<string, ScriptList> _script_connect = new Dictionary<string, ScriptList>();
        protected Dictionary<string, ScriptList> _script_send = new Dictionary<string, ScriptList>();
        protected Dictionary<string, ScriptList> _script_recv = new Dictionary<string, ScriptList>();
        protected Dictionary<string, ScriptTimer> _script_timer = new Dictionary<string, ScriptTimer>();
        protected List<ScriptList> _script_select = new List<ScriptList>();
        protected List<(string desc, IPEndPoint epoint)> _local_addr = new List<(string, IPEndPoint)>();
        protected List<(string desc, IPEndPoint epoint)> _remote_addr = new List<(string, IPEndPoint)>();

        protected Dictionary<string,CommandSet> _local_set = new Dictionary<string, CommandSet>();
        protected Dictionary<string, CommandSet> _remote_set = new Dictionary<string, CommandSet>();


        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);

            _ivalues.Clear();
            _bvalues.Clear();
            _incriment_values.Clear();
            foreach (var pair in root["Working-area"].GetValues())
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
            foreach (Node node in root["Working-area"].GetObjects())
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

            _local_addr.Clear();
            _remote_addr.Clear();
            foreach (Node node in root["Working-area"].GetArrayObject())
            {
                string name = node._name.Split('[')[0];
                try
                {
                    string desc = node["desc"].Required();
                    string iaddr = node["ip"].Required();
                    int portno = node["port"].Required();
                    IPEndPoint endPoint = NSocket.GetIPEndPoint(iaddr, portno);
                    switch (name)
                    {
                        case "local_addr":
                            _local_addr.Add((desc, endPoint));
                            node.AddValue("id", desc);      // Commandクラスが'id'必須なので追加しておく
                            _local_set.Add(desc, new CommandSet(node));
                            break;
                        case "remote_addr":
                            _remote_addr.Add((desc, endPoint));
                            node.AddValue("id", desc);      // Commandクラスが'id'必須なので追加しておく
                            _remote_set.Add(desc, new CommandSet(node));
                            break;
                        default:
                            throw new Exception($"配列項目で指定できるのは'local_addr'と'remote_addr'だけです");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ScriptDefineのvalues('{name}')で読み込みエラー in {path}", ex);
                }
            }

            _comands.Clear();
            foreach (Node def in root["Commands"])
            {
                try
                {
                    _comands.Add(def["id"].Required(), Command.ReadJson(def));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Commandsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }

            _script_connect.Clear();
            _script_send.Clear();
            _script_recv.Clear();
            _script_timer.Clear();
            _script_select.Clear();
            foreach (Node def in root["Scripts"])
            {
                try
                {
                    string scrid = def["id"].Required();
                    ScriptList script = null;
                    switch ((string)def["when"].Required())
                    {
                        case "send":
                            script = new ScriptList(def, _comands);
                            _script_send.Add(scrid, script);
                            break;
                        case "connect":
                            script = new ScriptList(def, _comands);
                            _script_connect.Add(scrid, script);
                            break;
                        case "recv":
                            script = new ScriptList(def, _comands);
                            _script_recv.Add(scrid, script);
                            break;
                        case "timer":
                            script = new ScriptTimer(def, _comands);
                            _script_timer.Add(scrid, (ScriptTimer)script);
                            break;
                        case "disp":
                            script = new ScriptList(def, _comands);
                            break;
                    }
                    if(script !=null && script.Display == true)
                    {
                        _script_select.Add(script);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Scriptsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }
        }

        /// <summary>
        /// 画面に表示するスクリプト一覧を取得
        /// </summary>
        /// <returns></returns>
        public ScriptList[] GetScriptList()
        {
            return _script_select.ToArray();
        }

        public (string desc, IPEndPoint epoint)[] GetLocalAddr()
        {
            return _local_addr.ToArray();
        }
        public (string desc, IPEndPoint epoint)[] GetRemoteAddr()
        {
            return _remote_addr.ToArray();
        }

        public void OnSelectLocal(string desc)
        {
            _local_set[desc].Exec(null);
        }
        public void OnSelectRemote(string desc)
        {
            _remote_set[desc].Exec(null);
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
            string disp= string.Empty;
            foreach(var pair in _commMessagesDisp)
            {
                if(pair.Value == id)
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

        public void ExecOnConnect(CommSocket socket)
        {
            foreach (var pair in _script_timer)
            {
                string key = pair.Key;
                ScriptTimer script = pair.Value;
                script.Start(socket);
            }
            foreach (var pair in _script_connect)
            {
                string key = pair.Key;
                ScriptList script = pair.Value;
                script.Exec(socket);
            }
        }
        public void ExecOnDisconnect()
        {
            foreach (var pair in _script_timer)
            {
                string key = pair.Key;
                ScriptTimer script = pair.Value;
                script.Stop();
            }
        }

        public void ExecOnSend(CommSocket socket, CommMessage msg)
        {
            foreach ( var pair in _script_send)
            {
                string key = pair.Key;
                ScriptList script = pair.Value;
                script.Exec(socket, msg);
            }
        }
        public void ExecOnRecv(CommSocket socket, CommMessage msg)
        {
            foreach (var pair in _script_recv)
            {
                string key = pair.Key;
                ScriptList script = pair.Value;
                script.Exec(socket, msg);
            }
        }
    }
}
