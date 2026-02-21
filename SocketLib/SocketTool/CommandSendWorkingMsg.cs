using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandSendWorkingMsg: Command
    {
        private string _name;

        private CommandSendWorkingMsg()
        {
        }

        public CommandSendWorkingMsg(Node node) : base(node)
        {
            _name = node["msg"];
        }

        public override Command Copy()
        {
            CommandSendWorkingMsg cmd = new CommandSendWorkingMsg();
            base.Copy(cmd);
            cmd._name = _name;
            return cmd;
        }

        public override void Exec(CommSocket socket, CommMessage resmsg = null)
        {
            ScriptDefine scdef = ScriptDefine.GetInstance();
            CommMessage msg = scdef.GetValueMsg(_name);

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

            socket.Send(msg);
        }
    }
}
