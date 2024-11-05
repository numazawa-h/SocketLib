﻿using NCommonUtility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
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
        protected Dictionary<string, Command> _comands = new Dictionary<string, Command>();
        protected Dictionary<string, ScriptOnSend> _script_send = new Dictionary<string, ScriptOnSend>();
        protected Dictionary<string, Command> _script_recv = new Dictionary<string, Command>();



        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);
            Dictionary<string, JsonValue> values = root["values"].GetValues();

            _ivalues.Clear();
            _bvalues.Clear();
            _incriment_values.Clear();
            foreach (var pair in values)
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
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
                        throw new Exception($"ScriptDefineのvaluesに数値と文字列以外が指定されました('{key}')");
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

            _script_send.Clear();
            foreach (Node def in root["Scripts"])
            {
                try
                {
                    string cmdid;
                    switch ((string)def["when"].Required())
                    {
                        case "send":
                            if (def.ContainsKey("cmd"))
                            {
                                cmdid = def["cmd"];
                                if (_comands.ContainsKey(cmdid) == false)
                                {
                                    throw new Exception($"Scriptsで使用されている'{cmdid}'は定義されていません");
                                }
                                _script_send.Add(cmdid, new ScriptOnSend(def, _comands[cmdid]));
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Scriptsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }
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



        public void ExecOnSend(CommSocket socket, CommMessage msg)
        {
            foreach ( var pair in _script_send)
            {
                string key = pair.Key;
                ScriptOnSend script = pair.Value;
                script.Exec(socket, msg);
            }
        }

        protected class ScriptOnSend
        {
            HashSet<string> _dtypes = new  HashSet<string>();
            HashSet<string> _without = new HashSet<string>();
            Command _command;

            public ScriptOnSend(Node def, Command cmd)
            {
                _command = cmd.Copy();
                _dtypes = def.GetStringValues("dtype");
                _without = def.GetStringValues("without");
            }

            public void Exec(CommSocket socket, CommMessage msg)
            {
                string dtype = msg.DType;
                if (_without.Contains(dtype)==false || _dtypes.Contains(dtype)==true)
                {
                    _command.Exec(socket, msg);
                }
            }
        }
    }
}
