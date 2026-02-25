using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    internal class CommandTimerConditional : Command
    {
        private string _owner_id;
        private List<CaseList> _caselist = new List<CaseList>();
        private List<CommandTimer> _commands = new List<CommandTimer>();
        private CommandTimerConditional()
        {
        }

        public CommandTimerConditional(Node node, string owner_id)
        {
            _owner_id = owner_id;
            string id = node["id"];
            foreach (Node node1 in node["select"].Required())
            {
                var case1 = new CaseList(node1);
                if (node1.ContainsKey("select") == false)
                {
                    _caselist.Add(case1);
                    node1.AddValue("id", node1._name);      // Commandクラスが'id'必須なので追加しておく
                    _commands.Add(new CommandTimer(node1));
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
                            _commands.Add(new CommandTimer(node2));
                        }
                        else
                        {
                            foreach (Node node3 in node2["select"])
                            {
                                var case3 = new CaseList(node3).Add(case2);
                                if (node3.ContainsKey("select") == false)
                                {
                                    _caselist.Add(case3);
                                    node3.AddValue("id", node3._name);      // Commandクラスが'id'必須なので追加しておく
                                    _commands.Add(new CommandTimer(node3));
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
            CommandTimerConditional cmd = new CommandTimerConditional();
            base.Copy(cmd);
            cmd._owner_id = _owner_id;
            cmd._commands = new List<CommandTimer>(_commands);
            cmd._caselist = new List<CaseList>(_caselist);
            return cmd;
        }

        public void SetTimerScript()
        {
            foreach (CommandTimer cmd in _commands)
            {
                cmd.SetTimerScript();
            }
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
