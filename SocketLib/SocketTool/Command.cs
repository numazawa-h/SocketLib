using NCommonUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public delegate void CommandEventHandler(Object sender, CommandEventArgs args);
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs()
        {
        }
    }

    public abstract class Command
    {
        public string CommandId { get; protected set; }
        protected Dictionary<string, int> _ivalues = new Dictionary<string, int>();
        protected Dictionary<string, byte[]> _bvalues = new Dictionary<string, byte[]>();
        protected Dictionary<string, string> _ivalues_runtime = new Dictionary<string, string>();
        protected Dictionary<string, string> _bvalues_runtime = new Dictionary<string, string>();
        protected Dictionary<string, string> _datetime_runtime = new Dictionary<string, string>();
        protected Dictionary<string, string> _msgcopy_runtime = new Dictionary<string, string>();
        protected Dictionary<string, JsonValue> _ivariable = new Dictionary<string, JsonValue>();
        protected string[] _reqcopy;


        public abstract Command Copy();
        public abstract void Exec(CommSocket socket, CommMessage msg = null);

        protected Command Copy(Command other)
        {
            other.CommandId = CommandId;
            other._ivalues = _ivalues;
            other._bvalues = _bvalues;
            other._ivalues_runtime = _ivalues_runtime;
            other._bvalues_runtime = _bvalues_runtime;
            other._datetime_runtime = _datetime_runtime;
            other._msgcopy_runtime = _msgcopy_runtime;
            other._msgcopy_runtime = _msgcopy_runtime;
            other._ivariable = _ivariable;
            return other;
        }

        protected Command()
        {

        }

        protected Command(Node node)
        {
            CommandId = node["id"].Required();
            _ivalues.Clear();
            _bvalues.Clear();
            _ivalues_runtime.Clear();
            _bvalues_runtime.Clear();
            _datetime_runtime.Clear();
            _ivariable.Clear();
            _reqcopy = node.GetStringValues("reqcopy").ToArray();

            foreach (var pair in node["var"].GetValues())
            {
                string key = pair.Key;
                JsonValue value = pair.Value;

                if (key.StartsWith("#"))
                {
                    _ivariable.Add(key, value);
                }

            }

            foreach (var pair in node["values"].GetValues())
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
                switch (value.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.Number:
                        int ival = value.GetValue<int>();
                        _ivalues.Add(key, ival);
                        break;
                    case System.Text.Json.JsonValueKind.String:
                        string sval = value.ToString();
                        CommMessageDefine.Format fmtdef = CommMessageDefine.GetInstance().GetValuesDefine(key)?.FormatDef;
                        string fmt = fmtdef?.GetValueFormat();
                        if (fmtdef is CommMessageDefine.FormatDateTime)
                        {
                            if (fmt == null)
                            {
                                throw new Exception($"日時型フィールドに値を設定するためには値フォーマット('valfmt')の指定が必要です");
                            }
                            // 日時型の時は、現在日時設定処理をサポート
                            if (sval == "now")
                            {
                                _datetime_runtime.Add(key, fmt);
                            }
                            else
                            {
                                SetDateTimeValue(key, sval, fmt);
                            }
                        }
                        else
                        {
                            // ScriptDefineの Working-area定義項目の時の処理
                            ScriptDefine scdef = ScriptDefine.GetInstance();
                            if (scdef.ContainsKeyIntValue(sval))
                            {
                                _ivalues_runtime.Add(key, sval);
                            }
                            else if (scdef.ContainsKeyByteValue(sval))
                            {
                                _bvalues_runtime.Add(key, sval);
                            }
                            else
                            {
                                _bvalues.Add(key, ByteArray.StrToByte(sval));
                            }
                        }
                        break;
                }
            }

            _msgcopy_runtime.Clear();
            foreach (var pair in node["msgcopy"].GetValues())
            {
                string key = pair.Key;
                JsonValue value = pair.Value;
                switch (value.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.String:
                        _msgcopy_runtime.Add(key, value.ToString());
                        break;
                }
            }
        }

        protected void SetDateTimeValue(string key, string sval, string fmt)
        {
            int yyyy, MM, dd;
            int HH = 0;
            int mm = 0;
            int ss =0;

            switch (fmt)
            {
                case "yyyyMMddHHmmss":
                case "yyyyMMddHHmm":
                case "yyyyMMddHH":
                case "yyyyMMdd":
                case "yyMMddHHmmss":
                case "yyMMddHHmm":
                case "yyMMddHH":
                case "yyMMdd":
                    break;
                default:
                    throw new Exception($"'日時型フィールド({key}')のformatの指定に不正な valfmt('{fmt}')が使われています");
            }
            if (fmt.Length != sval.Length)
            {
                throw new Exception($"日時型のフィールド'{key}'に対する設定値'{sval}'が不正です");
            }
            foreach (char s in sval)
            {
                if (s < '0' || s > '9') 
                {
                    throw new Exception($"日時型のフィールド'{key}'に対する設定値'{sval}'が不正です");
                }
            }

            int ofs = 0;
            if (fmt.Substring(0, 4) == "yyyy")
            {
                yyyy = int.Parse(sval.Substring(ofs, 4));
                ofs += 4;
            }
            else if (fmt.Substring(0, 2) == "yy")
            {
                yyyy = int.Parse(sval.Substring(0, 2)) + 2000;
                ofs += 2;
            }
            MM = int.Parse(sval.Substring(ofs, 2));
            ofs += 2;
            dd = int.Parse(sval.Substring(ofs, 2));
            ofs += 2;
            if (ofs < sval.Length)
            {
                HH = int.Parse(sval.Substring(ofs, 2));
                ofs += 2;
            }
            if (ofs < sval.Length)
            {
                mm = int.Parse(sval.Substring(ofs, 2));
                ofs += 2;
            }
            if (ofs < sval.Length)
            {
                ss = int.Parse(sval.Substring(ofs, 2));
                ofs += 2;
            }
            if (MM < 1 || MM >12 || dd < 1 || dd > 31 || HH > 23 || mm > 59 || ss > 59)
            {
                throw new Exception($"日時型のフィールド'{key}'に対する設定値'{sval}'が不正です");
            }
            _bvalues.Add(key, ByteArray.ParseHex(sval));
        }

        public static Command ReadJson(Node node)
        {
            Command cmd = null;
            string cmdid = node["id"].Required();
            string cmdtype = node["cmd"].Required();
            switch (cmdtype)
            {
                case "head":
                    cmd = new CommandHead(node);
                    break;
                case "set":
                    if (node.ContainsKey("msg"))
                    {
                        if (node.ContainsKey("select"))
                        {
                            cmd = new CommandSetValueMsgList(node, cmdid);
                        }
                        else
                        {
                            cmd = new CommandSetValueMsg(node);
                        }
                    }
                    else
                    {
                        cmd = new CommandSet(node);
                    }
                    break;
                case "send":
                    if (node.ContainsKey("msg"))
                    {
                        cmd = new CommandSendValueMsg(node);
                    }
                    else
                    {
                        cmd = new CommandSend(node);
                    }
                    break;
                default:
                    throw new Exception($"Commandsの'{cmdid}'でcmd指定('{cmdtype}')が不正です");
            }

            return cmd;
        }
    }
}
