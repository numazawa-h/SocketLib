using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandSet: Command
    {
        private CommandSet()
        {
        }

        public CommandSet(Node node, WorkingArea workarea)
        {
            _workarea = workarea;
            CommandId = node["id"].Required();
            _ivalues.Clear();
            _bvalues.Clear();
            _ivalues_runtime.Clear();   // 未使用
            _bvalues_runtime.Clear();   // 未使用
            _datetime_runtime.Clear();  // 未使用

            Dictionary<string, JsonValue> values = node["values"].GetPropertyValues();
            foreach (var pair in values)
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
                switch (value.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.Number:
                        if (_workarea.ContainsKeyIntValue(key)==false)
                        {
                            throw new Exception($"'{CommandId}'のvalues指定('{key}')が'values'に定義されていません");
                        }
                        _ivalues.Add(key, value.GetValue<int>());
                        break;
                    case System.Text.Json.JsonValueKind.String:
                        if (_workarea.ContainsKeyByteValue(key) == false)
                        {
                            throw new Exception($"'{CommandId}'のvalues指定('{key}')が'values'に定義されていません");
                        }
                        _bvalues.Add(key, ByteArray.StrToByte(value.ToString()));
                        break;
                }
            }
        }

        public override Command Copy()
        {
            CommandSet cmd = new CommandSet();
            base.Copy(cmd);

            return cmd;
        }

        public override void Exec(CommSocket socket, /* 未使用*/ CommMessage msg = null)
        {
            foreach (var pair in _ivalues)
            {
                _workarea.SetIntValue(pair.Key, pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                _workarea.SetByteValue(pair.Key, pair.Value);
            }
        }
    }
}
