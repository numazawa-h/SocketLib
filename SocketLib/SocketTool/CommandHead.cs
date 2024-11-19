using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandHead: Command
    {
        private CommandHead()
        {
        }

        public CommandHead(Node node): base(node)
        {
        }

        public override Command Copy()
        {
            CommandHead cmd = new CommandHead();
            base.Copy(cmd);

            return cmd;
        }

        public override void Exec(CommSocket socket, CommMessage msg = null)
        {
            if (msg == null)
            {
                return;
            }

            foreach (var pair in _ivalues)
            {
                msg.SetHedValue(pair.Key, (ulong)pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                msg.SetHedValue(pair.Key, pair.Value);
            }

            ScriptDefine scdef = ScriptDefine.GetInstance();
            foreach (var pair in _ivalues_runtime)
            {
                msg.SetHedValue(pair.Key, (ulong)scdef.GetIntValue(pair.Value));
            }
            foreach (var pair in _bvalues_runtime)
            {
                msg.SetHedValue(pair.Key, scdef.GetByteValue(pair.Value));
            }
            foreach (var pair in _datetime_runtime)
            {
                string hex = DateTime.Now.ToString(pair.Value);
                msg.SetHedValue(pair.Key, ByteArray.ParseHex(hex));
            }
        }

    }
}
