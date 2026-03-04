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
    internal class CommandSetWorkingMsgConditional : Command
    {
        private string _owner_id;
        private string _msgname;
        private List<CaseList> _caselist = new List<CaseList>();
        private List<CommandSetWorkingMsg> _commands = new List<CommandSetWorkingMsg>();

        private CommandSetWorkingMsgConditional()
        {
        }

        public CommandSetWorkingMsgConditional(Node node, string owner_id)
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
                    _commands.Add(new CommandSetWorkingMsg(node1, _msgname));
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
                            _commands.Add(new CommandSetWorkingMsg(node2, _msgname));
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
                                    _commands.Add(new CommandSetWorkingMsg(node3, _msgname));
                                }
                                else
                                {
                                    foreach (Node node4 in node3["select"])
                                    {
                                        var case4 = new CaseList(node4).Add(case3);
                                        if (node4.ContainsKey("select") == false)
                                        {
                                            _caselist.Add(case4);
                                            node4.AddValue("id", node4._name);      // Commandクラスが'id'必須なので追加しておく
                                            _commands.Add(new CommandSetWorkingMsg(node4, _msgname));
                                        }
                                        else
                                        {
                                            foreach (Node node5 in node4["select"])
                                            {
                                                var case5 = new CaseList(node5).Add(case4);
                                                if (node5.ContainsKey("select") == false)
                                                {
                                                    _caselist.Add(case5);
                                                    node5.AddValue("id", node5._name);      // Commandクラスが'id'必須なので追加しておく
                                                    _commands.Add(new CommandSetWorkingMsg(node5, _msgname));
                                                }
                                                else
                                                {
                                                    throw new Exception($"select-caseのネストは5階層までです({id})");
                                                }
                                            }
                                        }
                                    }
                                }
                            } 
                        }
                    }
                }
            }
        }

        public override Command Copy()
        {
            CommandSetWorkingMsgConditional cmd = new CommandSetWorkingMsgConditional();
            base.Copy(cmd);
            cmd._msgname = _msgname;
            cmd._commands = new List<CommandSetWorkingMsg>(_commands);
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
    }
}
