using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class CommandHead: Command
    {
        Dictionary<string, JsonValue> _values_def = null;

        private CommandHead()
        {
        }

        public CommandHead(Node node)
        {
            _values_def = node["values"].GetValues();
        }

        public override Command Copy()
        {
            CommandHead cmd = new CommandHead();
            cmd._values_def = _values_def;
            return cmd;
        }


        public override void Exec(CommMessage msg = null)
        {
            if (msg == null)
            {
                return;
            }
            foreach (var pair in _values_def)
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
                ByteArray dat = null;

                switch (value.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.String:
                        string val = value.ToString();
                        CommMessageDefine.Format fmtdef =CommMessageDefine.GetInstance().GetValuesDefine(key).FormatDef;
                        string fmt = fmtdef.GetFormat();
                        if (fmtdef is CommMessageDefine.FormatDateTime)
                        {
                            if(val == "now")
                            {
                                dat = new ByteArray(DateTime.Now, fmt);
                            }
                            else
                            {
                                dat = new ByteArray(val);
                            }
                        }
                        break;
                    case System.Text.Json.JsonValueKind.Number:
                        dat = new ByteArray(value.GetValue<ulong>());
                        break;
                }
                if (dat != null)
                {
                    msg.SetHedValue(key, dat);
                }
            }

        }

    }
}
