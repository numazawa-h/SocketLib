using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class RuntimeWorkingArea
    {
        private string _message_path;
        private string _script_path;
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
            _message_path = NetworkDefine.GetInstance().GetMessagePath(config);
            _values_def = ValuesDefine.ReadJson(_message_path);
            _message_def = CommMessageDefine.GetInstance().ReadJson(_message_path, _values_def);

            _script_path = NetworkDefine.GetInstance().GetScriptPath(config);
            _workarea = new WorkingArea();
            _workarea.ReadJson(_script_path, this);
            _scripts = ScriptDefine.ReadJson(_script_path, this);
        }
        public MessageDefine GetMessageDefine(string name) {
            if (_message_def.ContainsKey(name) == false)
            {
                return null;
            }
            return _message_def[name];  
        }
        public ValuesDefine GetValuesDefine(string name)
        {
            if (_values_def.ContainsKey(name) == false)
            {
                return null;
            }
            return _values_def[name];
        }
    }
}
