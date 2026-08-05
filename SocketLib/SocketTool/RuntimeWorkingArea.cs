using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class RuntimeWorkingArea
    {
        // 作業域定義
        protected WorkingArea _workarea = null;
        // スクリプト定義
        protected ScriptGroupDefine _scripts = null;
        // 通信メッセージ定義
        protected Dictionary<string, MessageDefine> _message_def = null;
        // 通信メッセージの値説明定義
        protected Dictionary<string, ValuesDefine> _values_def = null;

        public WorkingArea Working { get { return _workarea; } }
        public ScriptGroupDefine Scripts { get { return _scripts; } }
        public RuntimeWorkingArea(string config) { 
            string message_path = NetworkDefine.GetInstance().GetMessagePath(config);
            (_message_def, _values_def) = CommMessageDefine.GetInstance().ReadJson(message_path);

            string script_path = NetworkDefine.GetInstance().GetScriptPath(config);
            (_workarea, _scripts) = ScriptDefine.ReadJson(script_path);
        }
        public MessageDefine GetMessageDefine(string name) {
            return _message_def[name];  
        }
        public ValuesDefine GetValuesDefine(string name)
        {
            return _values_def[name];
        }
    }
}
