using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;
using System.Xml.Linq;
using System.Text.Json.Nodes;

namespace SocketTool
{
    public class CommandSetValueMsg : Command
    {
        private string _msgname;

        private CommandSetValueMsg()
        {
        }

        public CommandSetValueMsg(Node node):base(node) 
        {
            _msgname = node["msg"];
        }

        public CommandSetValueMsg(Node node, string name) : base(node)
        {
            _msgname = name;
        }

        public override Command Copy()
        {
            CommandSetValueMsg cmd = new CommandSetValueMsg();
            base.Copy(cmd);
            cmd._msgname = _msgname;
            return cmd;
        }

        public override void Exec(CommSocket socket, CommMessage resmsg = null)
        {
            ScriptDefine scdef = ScriptDefine.GetInstance();
            CommMessage msg = scdef.GetValueMsg(_msgname);

            foreach (var pair in _ivalues)
            {
                string key = pair.Key;
                if (key.Contains("#") == true)
                {
                    key = replaceVar(key, resmsg);
                }
                msg.SetFldValue(key, (ulong)pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                string key = pair.Key;
                if (key.Contains("#") == true)
                {
                    key = replaceVar(key, resmsg);
                }
                msg.SetFldValue(key, pair.Value);
            }
            foreach (var pair in _ivalues_runtime)
            {
                string key = pair.Key;
                if (key.Contains("#") == true)
                {
                    key = replaceVar(key, resmsg);
                }
                msg.SetFldValue(key, (ulong)scdef.GetIntValue(pair.Value));
            }
            foreach (var pair in _bvalues_runtime)
            {
                string key = pair.Key;
                if (key.Contains("#") == true)
                {
                    key = replaceVar(key, resmsg);
                }
                msg.SetFldValue(key, scdef.GetByteValue(pair.Value));
            }
            foreach (var pair in _datetime_runtime)
            {
                string key = pair.Key;
                if (key.Contains("#") == true)
                {
                    key = replaceVar(key, resmsg);
                }
                string hex = DateTime.Now.ToString(pair.Value);
                msg.SetFldValue(key, ByteArray.ParseHex(hex));
            }

            // 受信電文からのコピー処理
            if (resmsg != null)
            {
                foreach (string key in _reqcopy)
                {
                    string k = key;
                    if (key.Contains("#") == true)
                    {
                        k = replaceVar(key, resmsg);
                    }
                    msg.SetFldValue(k, resmsg.GetFldValue(k));
                }
                foreach (var pair in _msgcopy_runtime)
                {
                    string key = pair.Key;
                    string val = pair.Value;
                    if (key.Contains("#") == true)
                    {
                        key = replaceVar(key, resmsg);
                    }
                    if (val.Contains("#") == true)
                    {
                        val = replaceVar(val, resmsg);
                    }
                    msg.SetFldValue(key, resmsg.GetFldValue(val));
                }
            }
        }

        private string replaceVar(string str, CommMessage resmsg)
        {
            int? ival = null;
            string key = null;
            foreach (var pair in _ivariable)
            {
                key = pair.Key.ToString();
                JsonValue val = pair.Value;
                if (str.Contains(key))
                {
                    switch (val.GetValueKind())
                    {
                        case System.Text.Json.JsonValueKind.Number:
                            ival = val.GetValue<int>();
                            break;
                        case System.Text.Json.JsonValueKind.String:
                            ival = resmsg.GetFldValue(val.ToString()).to_int();
                            break;
                    }
                    break;
                }
            }
            if (ival == null)
            {
                return str;
            }
            else 
            {
                return str.Replace(key, ival.ToString());
            }
        }
    }
}
