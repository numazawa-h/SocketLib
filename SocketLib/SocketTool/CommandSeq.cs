using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandSeq: Command
    {
        private List<Command> _cmds = new List<Command>();

        public CommandSeq()
        {
        }

        public CommandSeq(Node node, RuntimeWorkingArea runtime)
        {
            int idx = 0;
            string ownerid = $"{node["id"]}";
            foreach(Node cmd in node.GetObjectValues("cmds"))
            {
                string id = $"{ownerid}[{idx}]";
                cmd.AddValue("id", id);
                _cmds.Add(Command.ReadJson(cmd, runtime));
            }
        }

        public override Command Copy()
        {
            CommandSeq cmd = new CommandSeq();
            cmd._cmds = this._cmds;
            return cmd;
        }

        public override void Exec(CommSocket socket, CommMessage msg = null)
        {
            foreach (Command cmd in _cmds)
            {
                cmd.Exec(socket, msg);
            }
        }
    }
}
