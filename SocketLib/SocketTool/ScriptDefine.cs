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
        protected Dictionary<string, Command> _comands = new Dictionary<string, Command>();
        protected Dictionary<string, ScriptGroup> _script_connect = new Dictionary<string, ScriptGroup>();
        protected Dictionary<string, ScriptGroup> _script_send = new Dictionary<string, ScriptGroup>();
        protected Dictionary<string, ScriptGroup> _script_recv = new Dictionary<string, ScriptGroup>();
        protected Dictionary<string, ScriptGroupOnTimer> _script_timer = new Dictionary<string, ScriptGroupOnTimer>();
        protected List<ScriptGroup> _script_list_on_display = new List<ScriptGroup>();

        public WorkingArea Working { get { return _working; } }


        public void ReadJson(string path)
        {
            _working = new WorkingArea();
            _working.ReadJson(path);

            RootNode root = JsonConfig.ReadJson(path);
            _comands.Clear();
            foreach (Node def in root["Commands"])
            {
                try
                {
                    _comands.Add(def["id"].Required(), Command.ReadJson(def));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Commandsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }

            _script_connect.Clear();
            _script_send.Clear();
            _script_recv.Clear();
            _script_timer.Clear();
            _script_list_on_display.Clear();
            foreach (Node def in root["Scripts"])
            {
                try
                {
                    string scrid = def["id"].Required();
                    ScriptGroup script = null;
                    switch ((string)def["when"].Required())
                    {
                        case "send":
                            script = new ScriptGroup(def, _comands);
                            _script_send.Add(scrid, script);
                            break;
                        case "connect":
                            script = new ScriptGroup(def, _comands);
                            _script_connect.Add(scrid, script);
                            break;
                        case "recv":
                            script = new ScriptGroup(def, _comands);
                            _script_recv.Add(scrid, script);
                            break;
                        case "timer":
                            script = new ScriptGroupOnTimer(def, _comands);
                            _script_timer.Add(scrid, (ScriptGroupOnTimer)script);
                            break;
                        case "disp":
                            script = new ScriptGroup(def, _comands);
                            break;
                    }
                    if(script !=null && script.Display == true)
                    {
                        _script_list_on_display.Add(script);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Scriptsで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }

            // CommandTimerから呼び出すScriptをScript定義読み込み後に設定する
            foreach (var pair in _comands)
            {
                if(pair.Value is CommandTimer)
                {
                    (pair.Value as CommandTimer).SetTimerScript();
                }
                if (pair.Value is CommandTimerConditional)
                {
                    (pair.Value as CommandTimerConditional).SetTimerScript();
                }
            }
        }

        /// <summary>
        /// 画面に表示するスクリプト一覧を取得
        /// </summary>
        /// <returns></returns>
        public ScriptGroup[] GetScriptListOnDisplay()
        {
            return _script_list_on_display.ToArray();
        }

        public ScriptGroupOnTimer GetScriptTimer(string name)
        {
            return _script_timer[name];
        }

        public void ExecOnConnect(CommSocket socket)
        {
            foreach (var pair in _script_timer)
            {
                string key = pair.Key;
                ScriptGroupOnTimer script = pair.Value;
                script.Start(socket);
            }
            foreach (var pair in _script_connect)
            {
                string key = pair.Key;
                ScriptGroup script = pair.Value;
                script.Exec(socket);
            }
        }
        public void ExecOnDisconnect()
        {
            foreach (var pair in _script_timer)
            {
                string key = pair.Key;
                ScriptGroupOnTimer script = pair.Value;
                script.Stop();
            }
        }

        public void ExecOnSend(CommSocket socket, CommMessage msg)
        {
            foreach ( var pair in _script_send)
            {
                string key = pair.Key;
                ScriptGroup script = pair.Value;
                script.Exec(socket, msg);
            }
        }
        public void ExecOnRecv(CommSocket socket, CommMessage msg)
        {
            foreach (var pair in _script_recv)
            {
                string key = pair.Key;
                ScriptGroup script = pair.Value;
                script.Exec(socket, msg);
            }
        }
    }
}
