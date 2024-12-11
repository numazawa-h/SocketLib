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
                msg.SetFldValue(pair.Key, (ulong)pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                msg.SetFldValue(pair.Key, pair.Value);
            }
            foreach (var pair in _ivalues_runtime)
            {
                msg.SetFldValue(pair.Key, (ulong)scdef.GetIntValue(pair.Value));
            }
            foreach (var pair in _bvalues_runtime)
            {
                msg.SetFldValue(pair.Key, scdef.GetByteValue(pair.Value));
            }
            foreach (var pair in _datetime_runtime)
            {
                string hex = DateTime.Now.ToString(pair.Value);
                msg.SetFldValue(pair.Key, ByteArray.ParseHex(hex));
            }

            // 受信電文からのコピー処理
            if (resmsg != null)
            {
                foreach (string key in _reqcopy)
                {
                    msg.SetFldValue(key, resmsg.GetFldValue(key));
                }
                foreach (var pair in _msgcopy_runtime)
                {
                    string key = pair.Key;
                    string val = pair.Value;
                    msg.SetFldValue(key, resmsg.GetFldValue(val));
                }
            }
        }
    }
}
