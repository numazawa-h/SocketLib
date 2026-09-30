using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandSet: Command
    {
        private CommandSet()
        {
        }

        public CommandSet(Node node, RuntimeWorkingArea runtime): base(node, runtime) 
        {
        }

        public override Command Copy()
        {
            CommandSet cmd = new CommandSet();
            base.Copy(cmd);

            return cmd;
        }

        public override void Exec(CommSocket socket, /* 未使用*/ CommMessage msg = null)
        {
            foreach (var pair in _ivalues_runtime_incriment)
            {
                int val = _runtime.Working.GetIntValueIncriment(pair.Value);
                _runtime.Working.SetIntValue(pair.Key, val);
            }
            foreach (var pair in _ivalues)
            {
                _runtime.Working.SetIntValue(pair.Key, pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                _runtime.Working.SetByteValue(pair.Key, pair.Value);
            }
        }
    }
}
