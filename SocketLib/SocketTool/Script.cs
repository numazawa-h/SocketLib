using SocketTool;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static NCommonUtility.JsonConfig;

namespace NCommonUtility
{
    public class Script
    {
        ScriptGroup _owner;
        protected HashSet<string> _dtypes = new HashSet<string>();
        protected HashSet<string> _without = new HashSet<string>();
        protected HashSet<int> _phase = new HashSet<int>();
        protected int _phase_less_than = -1;
        protected int _phase_greater_than_eq = -1;
        protected CommMessage _msg = null;
        protected List<Command> _commands = new List<Command>();

        public Script(Node def, Dictionary<string, Command> comands, ScriptGroup owner)
        {
            _owner = owner;
            _dtypes = def.GetStringValues("dtype");
            _without = def.GetStringValues("without");
            _phase = def.GetIntValues("phase");
            foreach (Node node1 in def.GetObjectValues("phase"))
            {
                int? less_than = null;
                int? greater_than = null;
                int? less_than_eq = null;
                int? greater_than_eq = null;
                foreach (var pair in node1.GetPropertyValues())
                {
                    string key = pair.Key;
                    if (pair.Value.GetValueKind() != JsonValueKind.Number)
                    {
                        throw new Exception($"phase指定の'{key}'に数値以外の値が指定されています");
                    }
                    int val = pair.Value.GetValue<int>();
                    switch (key)
                    {
                        case "<":
                            if (less_than == null || val < less_than)
                            {
                                less_than = val;
                            }
                            break;
                        case ">":
                            if (greater_than == null || greater_than < val)
                            {
                                greater_than = val;
                            }
                            break;
                        case "<=":
                            if (less_than_eq == null || val < less_than_eq)
                            {
                                less_than_eq = val;
                            }
                            break;
                        case ">=":
                            if (greater_than_eq == null || greater_than_eq < val)
                            {
                                greater_than_eq = val;
                            }
                            break;
                        default:
                            throw new Exception($"phaseの範囲指定に'<','>','<=','>='以外の'{key}'が指定されています('{node1.ToString()}')");
                    }
                }
                if(less_than == null && greater_than == null & less_than_eq == null && greater_than_eq == null)
                {
                    throw new Exception($"phaseの範囲指定に'<','>','<=','>='が指定されていません");
                }
                if(less_than != null && less_than_eq != null){
                    throw new Exception($"phaseの範囲指定に'<'と'<='の両方は指定できません");
                }
                if(greater_than != null && greater_than_eq != null){
                    throw new Exception($"phaseの範囲指定に'>'と'>='の両方は指定できません");
                }

                // less_thanとgreater_than_eqだけの条件に補正する
                if (less_than == null && less_than_eq != null)
                {
                    less_than = less_than_eq + 1;
                }
                if (greater_than_eq == null && greater_than != null)
                {
                    greater_than_eq = greater_than + 1;
                }

                if (less_than == null)
                {
                    // greater_than_eqのみの時
                    _phase_greater_than_eq = (int)greater_than_eq;
                }
                else if (greater_than_eq == null)
                {
                    // less_thanのみの時
                    _phase_less_than = (int)less_than;
                }
                else
                {
                    // 両方の時は範囲チェック
                    if (less_than <= greater_than_eq)
                    {
                        throw new Exception($"phaseの範囲指定が空集合になっています('{node1.ToString()}')");
                    }

                    // greater_than_eqからless_thanの範囲を展開する
                    foreach (int phase in Enumerable.Range((int)greater_than_eq, (int)less_than  - (int)greater_than_eq))
                    {
                        _phase.Add(phase);
                    }
                }
            }

            string msgname = def["msg"];
            if (msgname != null)
            {
                _msg = _owner.GetValueMsg(msgname);
            }
            _owner = owner;
            foreach(string cmdid in def.GetStringValues("cmd"))
            {
                if (comands.ContainsKey(cmdid) == false)
                {
                    throw new Exception($"Scriptsで使用されている'{cmdid}'は定義されていません");
                }
                Command cmd = comands[cmdid];
                if (_owner.When == "send" && cmd is CommandSend)
                {
                    throw new Exception($"送信スクリプト('{owner.ID}')に送信コマンド('{cmd.CommandId}')を含めることはできません");
                }
                _commands.Add(cmd);
            }
            if(_owner.When != "disp" && _commands.Count == 0)
            {
                throw new Exception($"Scriptsに'cmd'が定義されていません");
            }
        }
        public bool Exec(CommSocket socket, CommMessage msg=null)
        {
            string dtype = msg?.DType;
            switch (_owner.When)
            {
                case "send":
                case "recv":
                case "disp":
                    if (dtype == null)
                    {
                        return false;
                    }
                    if (_without.Contains(dtype) == true)
                    {
                        return false;
                    }
                    if (_dtypes.Count >0 &&_dtypes.Contains(dtype) == false)
                    {
                        return false;
                    }
                    break;
            }

            foreach (var command in _commands)
            {
                command.Exec(socket, (msg==null)?_msg:msg);
            }
            return true;
        }
        public bool OnPhase(int phase)
        {
            if (_phase.Count == 0 && _phase_less_than < 0 && _phase_greater_than_eq < 0)
            {
                // 条件が何も指定されなければ、常に該当phase
                return true;
            }
            if (_phase.Count > 0 &&_phase.Contains(phase))
            {
                // phase一覧に存在すれば、該当phase
                return true;
            }
            if (_phase_less_than >= 0 && phase < _phase_less_than)
            {
                // 指定されたless_thanより小さければ、該当phase
                return true;
            }
            if (_phase_greater_than_eq >= 0 && phase >= _phase_greater_than_eq)
            {
                // 指定されたgreater_than_eq以上ならば、該当phase
                return true;
            }
            return false;
        }
    }
}
