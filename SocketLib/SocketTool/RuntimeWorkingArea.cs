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
            _message_def = CommMessageDefine.ReadJson(_message_path, this);

            _script_path = NetworkDefine.GetInstance().GetScriptPath(config);
            _workarea = WorkingArea.ReadJson(_script_path, this);
            _scripts = ScriptDefine.ReadJson(_script_path, this);
        }
        public bool ContainsMessageDefine(string name)
        {
            return _message_def.ContainsKey(name);
        }
        public MessageDefine GetMessageDefine(string dtype) {
            dtype = dtype.ToLower();
            if (_message_def.ContainsKey(dtype) == false)
            {
                throw new Exception($"定義されていないデータ種別({dtype})");
            }
            return new MessageDefine(_message_def[dtype]);
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
