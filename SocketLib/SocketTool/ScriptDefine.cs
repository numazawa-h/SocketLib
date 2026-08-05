using NCommonUtility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Xml.Linq;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class ScriptDefine
    {
        static public (WorkingArea, ScriptGroupDefine) ReadJson(string path)
        {
            WorkingArea working = new WorkingArea();
            working.ReadJson(path);

            RootNode root = JsonConfig.ReadJson(path);
            Dictionary<string, Command> comands = new Dictionary<string, Command>();
            foreach (Node def in root["Commands"])
            {
                try
                {
                    comands.Add(def["id"].Required(), Command.ReadJson(def, working));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Commandsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }

            ScriptGroupDefine scripts = new ScriptGroupDefine();
            scripts.ReadJson(path, comands, working);

            return (working, scripts);
        }
    }
}
