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

        public CommandSend(Node node)
        {
            string dtype = node["dtype"].Required();
            string tmpl = node["tmpl"];
            _msg = this.InitMessage(dtype, tmpl);
            // ひな形に対して特定のフィールドを指定された値で上書きする
            foreach (var pair in node["values"].GetValues())
            {
                string key = pair.Key;
                JsonValue val = pair.Value;
            }
        }

        public override Command Copy()
        {
            CommandSend cmd = new CommandSend();
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

        public override void Exec(CommSocket socket, CommMessage msg = null)
        {
            if (msg == null)
            {
                return;
            }
            socket.Send(msg);
        }
    }
}
