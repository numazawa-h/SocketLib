using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public delegate void CommandEventHandler(Object sender, CommandEventArgs args);
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs()
        {
        }
    }

    public abstract class Command
    {

        public static Command ReadJson(Node node)
        {
            Command cmd = null;
            switch ((string)node["cmd"].Required())
            {
                case "head":
                    cmd = new CommandHead(node);
                    break;
                case "send":
                    cmd = new CommandSend(node);
                    break;
            }

            return cmd;
        }

        public abstract Command Copy();

        public abstract void Exec(CommSocket socket, CommMessage msg = null);
    }
}
