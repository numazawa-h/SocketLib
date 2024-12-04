using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;
using System.Xml.Linq;
using System.Text.Json.Nodes;
using NCommonUtility;
using System.Security.Cryptography;

namespace SocketTool
{
    internal class CommandSetValueMsgList : Command
    {
        private string _name;
        private List<Dictionary<string, JsonValue>> _caselist = new List<Dictionary<string, JsonValue>>();
        private List<CommandSetValueMsg> _commands = new List<CommandSetValueMsg>();

        private CommandSetValueMsgList()
        {
        }

        public CommandSetValueMsgList(Node node)
        {
            _name = node["msg"];
            string id = node["id"];
            foreach (Node node1 in node["select"].Required())
            {
                _caselist.Add(node1["case"].Required().GetValues());
                node1.AddValue("id", node1._name);      // Commandクラスが'id'必須なので追加しておく
                _commands.Add(new CommandSetValueMsg(node1, _name));
            }
        }

        public override Command Copy()
        {
            CommandSetValueMsgList cmd = new CommandSetValueMsgList();
            base.Copy(cmd);
            cmd._name = _name;
            cmd._commands = new List<CommandSetValueMsg>(_commands);
            return cmd;
        }

        public override void Exec(CommSocket socket, CommMessage resmsg)
        {
            if(resmsg == null)
            {
                return;
            }
            int idx = 0;
            foreach(Dictionary<string, JsonValue> conditons in _caselist)
            {
                if (checkCondition(conditons, resmsg))
                {
                    _commands[idx].Exec(socket, resmsg);
                    break;
                }
                ++idx;
            }
        }

        private bool checkCondition(Dictionary<string, JsonValue> conditons, CommMessage resmsg)
        {
            ScriptDefine scrd = ScriptDefine.GetInstance();
            foreach (var pair in conditons)
            {
                string key = pair.Key;
                JsonValue val = pair.Value;
                if (resmsg.ContainsFldKey(key) == false)
                {
                    return false;
                }
                ByteArray fldval = resmsg.GetFldValue(key);
                switch (val.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.Number:
                        int intval = val.GetValue<int>();
                        if (fldval.to_int() != intval)
                        {
                            return false;
                        }
                        break;
                    case System.Text.Json.JsonValueKind.String:
                        string hexval = val.GetValue<string>();
                        if (fldval.to_hex() != hexval)
                        {
                            return false;
                        }
                        break;
                }
            }
            return true;
        }
    }
}
