using NCommonUtility;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class ScriptGroupDefine
    {
        protected Dictionary<string, ScriptGroup> _script_connect = new Dictionary<string, ScriptGroup>();
        protected Dictionary<string, ScriptGroup> _script_send = new Dictionary<string, ScriptGroup>();
        protected Dictionary<string, ScriptGroup> _script_recv = new Dictionary<string, ScriptGroup>();
        protected Dictionary<string, ScriptGroupOnTimer> _script_timer = new Dictionary<string, ScriptGroupOnTimer>();
        protected List<ScriptGroup> _script_list_on_display = new List<ScriptGroup>();

        public void ReadJson(string path, Dictionary<string, Command> comands, RuntimeWorkingArea runtime)
        {
            RootNode root = JsonConfig.ReadJson(path);
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
                            script = new ScriptGroup(def, comands, runtime);
                            _script_send.Add(scrid, script);
                            break;
                        case "connect":
                            script = new ScriptGroup(def, comands, runtime);
                            _script_connect.Add(scrid, script);
                            break;
                        case "recv":
                            script = new ScriptGroup(def, comands, runtime);
                            _script_recv.Add(scrid, script);
                            break;
                        case "timer":
                            script = new ScriptGroupOnTimer(def, comands, runtime);
                            _script_timer.Add(scrid, (ScriptGroupOnTimer)script);
                            break;
                        case "disp":
                            script = new ScriptGroup(def, comands, runtime);
                            break;
                    }
                    if (script != null && script.Display == true)
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
            foreach (var pair in comands)
            {
                if (pair.Value is CommandTimer)
                {
                    (pair.Value as CommandTimer).SetTimerScript(this);
                }
                if (pair.Value is CommandTimerConditional)
                {
                    (pair.Value as CommandTimerConditional).SetTimerScript(this);
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
            foreach (var pair in _script_send)
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
