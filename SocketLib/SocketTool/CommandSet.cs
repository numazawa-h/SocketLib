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

        public CommandSet(Node node)
        {
            CommandId = node["id"].Required();
            _ivalues.Clear();
            _bvalues.Clear();
            _ivalues_runtime.Clear();
            _bvalues_runtime.Clear();
            _datetime_runtime.Clear();

            Dictionary<string, JsonValue> values = node["values"].GetPropertyValues();
            foreach (var pair in values)
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
                // ScriptDefineの values定義項目を対象とする
                ScriptDefine scdef = ScriptDefine.GetInstance();
                switch (value.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.Number:
                        if (scdef.ContainsKeyIntValue(key)==false)
                        {
                            throw new Exception($"'{CommandId}'のvalues指定('{key}')が'values'に定義されていません");
                        }
                        _ivalues.Add(key, value.GetValue<int>());
                        break;
                    case System.Text.Json.JsonValueKind.String:
                        if (scdef.ContainsKeyByteValue(key) == false)
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
            ScriptDefine scdef = ScriptDefine.GetInstance();
            foreach (var pair in _ivalues)
            {
                scdef.SetIntValue(pair.Key, pair.Value);
            }
            foreach (var pair in _bvalues)
            {
                scdef.SetByteValue(pair.Key, pair.Value);
            }
        }
    }
}
