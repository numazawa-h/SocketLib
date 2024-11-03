using NCommonUtility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
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

        protected Dictionary<string, JsonValue> _values = new Dictionary<string, JsonValue>();
        protected Dictionary<string, Command> _comands = new Dictionary<string, Command>();
        protected Dictionary<string, ScriptOnSend> _script_send = new Dictionary<string, ScriptOnSend>();
        protected Dictionary<string, Command> _script_recv = new Dictionary<string, Command>();


        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);

            _values =root["values"].GetValues();

            _comands.Clear();
            foreach (Node def in root["Commands"])
            {
                _comands.Add(def["id"], Command.ReadJson(def));
            }

            _script_send.Clear();
            foreach (Node def in root["Scripts"])
            {
                string cmdid;
                switch ((string)def["when"].Required())
                {
                    case "send":
                        cmdid = def["cmd"];
                        if (cmdid != null && _comands.ContainsKey(cmdid)) {
                            _script_send.Add(cmdid, new ScriptOnSend(def, _comands[cmdid]));
                        }
                        break;
                }
            }
        }

        public void ExecOnSend(CommMessage msg)
        {
            foreach ( var pair in _script_send)
            {
                string key = pair.Key;
                ScriptOnSend script = pair.Value;
                script.Exec(msg);
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

            public void Exec(CommMessage msg)
            {
                string dtype = msg.DType;
                if (_without.Contains(dtype)==false || _dtypes.Contains(dtype)==true)
                {
                    _command.Exec(msg);
                }
            }
        }
    }
}
