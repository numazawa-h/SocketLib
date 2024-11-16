using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace NCommonUtility
{
    public abstract class Script
    {
        protected HashSet<string> _dtypes = new HashSet<string>();
        protected HashSet<string> _without = new HashSet<string>();
        protected List<Command> _commands = new List<Command>();
        public bool Enable = false;

        protected Script(Node def, Command[] cmds)
        {
            foreach (var cmd in cmds)
            {
                _commands.Add(cmd.Copy());
            }
            _dtypes = def.GetStringValues("dtype");
            _without = def.GetStringValues("without");
            Enable = (bool?)def["enable"] is bool v ? v : true;  // 省略値は、true
        }
        public abstract void Exec(CommSocket socket, CommMessage msg=null);
    }

    public class ScriptOnSend: Script
    {
        public ScriptOnSend(Node def, Command[] cmds):base(def, cmds)
        {
        }

        public override void Exec(CommSocket socket, CommMessage msg = null)
        {
            if (Enable == false)
            {
                return;
            }
            string dtype = msg.DType;
            if (_without.Contains(dtype) == false || _dtypes.Contains(dtype) == true)
            {
                foreach (var command in _commands)
                {
                    command.Exec(socket, msg);
                }
            }
        }
    }
    public class ScriptOnConnect : Script
    {
        public ScriptOnConnect(Node def, Command[] cmds) : base(def, cmds)
        {
        }

        public override void Exec(CommSocket socket, CommMessage msg = null)
        {
            if (Enable == false)
            {
                return;
            }
            foreach (var command in _commands)
            {
                command.Exec(socket, msg);
            }
        }
    }
}
