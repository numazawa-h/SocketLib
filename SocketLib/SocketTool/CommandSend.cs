using NCommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandSend: Command
    {
        private CommMessage _msg;
        public string Dtype => _msg.DType;

        private CommandSend()
        {
        }

        public CommandSend(Node node) : base(node)
        {
            // ひな形読み込み
            string dtype = node["dtype"].Required();
            string tmpl = node["tmpl"];
            _msg = this.InitMessage(dtype, tmpl);

            foreach (var pair in _ivalues)
            {
                _msg.SetFldValue(pair.Key, (ulong)pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                _msg.SetFldValue(pair.Key, pair.Value);
            }
        }

        public override Command Copy()
        {
            CommandSend cmd = new CommandSend();
            base.Copy(cmd);
            cmd._msg = new CommMessage(_msg);
            return cmd;
        }

        private CommMessage InitMessage(string dtype, string tmpl)
        {
            string wcd = System.AppDomain.CurrentDomain.BaseDirectory;
            string path;
            if (tmpl == null)
            {
                path = $"{wcd}SendDataTemplate\\{dtype}.bin";
            }
            else
            {
                path = $"{wcd}SendDataTemplate\\{tmpl}";
                if (File.Exists(path) == false)
                {
                    path = $"{wcd}SendDataTemplate\\{dtype}.bin";
                }
            }

            if (File.Exists(path))
            {
                return new CommMessage(dtype, File.ReadAllBytes(path));
            }
            else
            {
                return new CommMessage(dtype);
            }
        }

        public CommMessage GetMessage()
        {
            return new CommMessage(_msg);
        }

        public override void Exec(CommSocket socket, CommMessage resmsg = null)
        {
            CommMessage msg = new CommMessage(_msg);
            ScriptDefine scdef = ScriptDefine.GetInstance();
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
