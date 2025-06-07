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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace SocketTool
{
    internal class CommandSetValueMsgList : Command
    {
        private string _owner_id;
        private string _msgname;
        private List<CaseList> _caselist = new List<CaseList>();
        private List<CommandSetValueMsg> _commands = new List<CommandSetValueMsg>();

        private CommandSetValueMsgList()
        {
        }

        public CommandSetValueMsgList(Node node, string owner_id)
        {
            _owner_id = owner_id;
            _msgname = node["msg"];
            string id = node["id"];
            foreach (Node node1 in node["select"].Required())
            {
                var case1 = new CaseList(node1);
                if (node1.ContainsKey("select") == false)
                {
                    _caselist.Add(case1);
                    node1.AddValue("id", node1._name);      // Commandクラスが'id'必須なので追加しておく
                    _commands.Add(new CommandSetValueMsg(node1, _msgname));
                }
                else
                {
                    foreach (Node node2 in node1["select"])
                    {
                        var case2 = new CaseList(node2).Add(case1);
                        if (node2.ContainsKey("select") == false)
                        {
                            _caselist.Add(case2);
                            node2.AddValue("id", node2._name);      // Commandクラスが'id'必須なので追加しておく
                            _commands.Add(new CommandSetValueMsg(node2, _msgname));
                        }
                        else
                        {
                            foreach (Node node3 in node2["select"])
                            {
                                var case3 = new CaseList(node3).Add(case2);
                                if (node3.ContainsKey("select")==false)
                                {
                                    _caselist.Add(case3);
                                    node3.AddValue("id", node3._name);      // Commandクラスが'id'必須なので追加しておく
                                    _commands.Add(new CommandSetValueMsg(node3, _msgname));
                                }
                                else
                                {
                                    throw new Exception($"selectのネストは３階層までです({id})");
                                }
                            }
                        }
                    }
                }
            }
        }

        public override Command Copy()
        {
            CommandSetValueMsgList cmd = new CommandSetValueMsgList();
            base.Copy(cmd);
            cmd._msgname = _msgname;
            cmd._commands = new List<CommandSetValueMsg>(_commands);
            return cmd;
        }

        public override void Exec(CommSocket socket, CommMessage resmsg)
        {
            if (resmsg == null)
            {
                Log.Warn($"select-caseで条件判定するための電文が指定されていないのでこのコマンドは実行されません。({_owner_id})");
                return;
            }
            int idx = 0;
            foreach (CaseList caselist in _caselist)
            {
                if (caselist.isTarget(resmsg))
                {
                    _commands[idx].Exec(socket, resmsg);
                    break;
                }
                ++idx;
            }
        }


        private class CaseList
        {
            Dictionary<string, JsonValue> _values = new Dictionary<string, JsonValue>();
            Dictionary<string, JsonValue[]> _array_values = new Dictionary<string, JsonValue[]>();

            public CaseList(Node node)
            {
                _values = node["case"].Required().GetPropertyValues();
                _array_values = node["case"].Required().GetPropertyArrayValue();
            }

            public CaseList Add(CaseList other)
            {
                foreach (var pair in other._values)
                {
                    this._values.Add(pair.Key, pair.Value);
                }
                foreach (var pair in other._array_values)
                {
                    this._array_values.Add(pair.Key, pair.Value);
                }
                return this;
            }

            public bool isTarget(CommMessage resmsg)
            {
                foreach (var pair in _values)
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
                            string hexval = val.GetValue<string>().ToUpper();
                            if (fldval.to_hex() != hexval)
                            {
                                return false;
                            }
                            break;
                    }
                }
                foreach (var pair in _array_values)
                {
                    string key = pair.Key;
                    JsonValue[] vals = pair.Value;
                    if (resmsg.ContainsFldKey(key) == false)
                    {
                        return false;
                    }
                    ByteArray fldval = resmsg.GetFldValue(key);
                    bool or = false;
                    foreach(JsonValue val in vals)
                    {
                        switch (val.GetValueKind())
                        {
                            case System.Text.Json.JsonValueKind.Number:
                                int intval = val.GetValue<int>();
                                if (fldval.to_int() == intval)
                                {
                                    or = true;
                                }
                                break;
                            case System.Text.Json.JsonValueKind.String:
                                string hexval = val.GetValue<string>().ToUpper();
                                if (fldval.to_hex() == hexval)
                                {
                                    or = true;
                                }
                                break;
                        }
                        if(or == false)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
