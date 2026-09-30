using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandCollection : Command
    {
        string _cmdtype;
        string _msg;

        public CommandCollection()
        {
        }

        public CommandCollection(Node node, RuntimeWorkingArea runtime, string cmdtype)
        {
            _runtime = runtime;
            CommandId = node["id"].Required();

            _cmdtype = cmdtype;
            _msg = node["msg"].Required();
        }

        public override Command Copy()
        {
            CommandCollection cmd = new CommandCollection();
            base.Copy(cmd);
            cmd._cmdtype = _cmdtype;
            cmd._msg = _msg;

            return cmd;
        }

        public override void Exec(CommSocket socket, /*未使用*/ CommMessage msg = null)
        {
            switch (_cmdtype)
            {
                case "enqueue":
                    _runtime.Working.EnqueuMessage(_msg);
                    break;
                case "dequeue":
                    _runtime.Working.DequeuMessage(_msg);
                    break;
            }
        }
    }
}
