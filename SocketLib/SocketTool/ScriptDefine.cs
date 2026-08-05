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
        // シングルトン
        static private ScriptDefine _instance = null;
        static public ScriptDefine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ScriptDefine();
            }
            return _instance;
        }
        private ScriptDefine() : base()
        {
        }

        protected WorkingArea _working = null;
        protected ScriptGroupDefine _scripts = null;

        public WorkingArea Working { get { return _working; } }
        public ScriptGroupDefine Scripts { get { return _scripts; } }


        public (WorkingArea, ScriptGroupDefine) ReadJson(string path)
        {
            _working = new WorkingArea();
            _working.ReadJson(path);

            RootNode root = JsonConfig.ReadJson(path);
            Dictionary<string, Command> comands = new Dictionary<string, Command>();
            foreach (Node def in root["Commands"])
            {
                try
                {
                    comands.Add(def["id"].Required(), Command.ReadJson(def, _working));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Commandsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }

            _scripts = new ScriptGroupDefine();
            _scripts.ReadJson(path, comands);

            return (_working, _scripts);
        }
    }
}
