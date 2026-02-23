using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    internal class CommandTimer : Command
    {
        private HashSet<string> _timer_on_name = new HashSet<string>();
        private HashSet<string> _timer_off_name = new HashSet<string>();
        private List<ScriptTimer> _timer_on = new List<ScriptTimer>();
        private List<ScriptTimer> _timer_off = new List<ScriptTimer>();

        private CommandTimer()
        {
        }

        public CommandTimer(Node node) : base(node)
        {
            if (node.ContainsKey("timer-on")==false && node.ContainsKey("timer-off")==false)
            {
                throw new Exception($"Timerコマンド'{CommandId}'には'timer-on'または'timer-off'が必要です");
            }

            if (node.ContainsKey("timer-on"))
            {
                _timer_on_name = node.GetStringValues("timer-on");
            }
            if (node.ContainsKey("timer-off"))
            {
                _timer_off_name = node.GetStringValues("timer-off");
            }
        }

        public override Command Copy()
        {
            CommandTimer cmd = new CommandTimer();
            base.Copy(cmd);
            cmd._timer_on_name = new HashSet<string>(_timer_on_name);
            cmd._timer_off_name = new HashSet<string>(_timer_off_name);
            cmd._timer_on = new List<ScriptTimer>(_timer_on);
            cmd._timer_off = new List<ScriptTimer>(_timer_off);

            return cmd;
        }

        public void SetTimerScript()
        {
            ScriptDefine scr = ScriptDefine.GetInstance();

            _timer_on.Clear();
            foreach (string name in _timer_on_name)
            {
                try
                {
                    _timer_on.Add(scr.GetScriptTimer(name));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Command('{this.CommandId}')のtimer-on指定('{name}')が不正です", ex);
                }
            }
            _timer_off.Clear();
            foreach (string name in _timer_off_name)
            {
                try
                {
                    _timer_off.Add(scr.GetScriptTimer(name));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Command('{this.CommandId}')のtimer-off指定('{name}')が不正です", ex);
                }
            }
        }

        public override void Exec(CommSocket socket, CommMessage msg = null)
        {
            foreach(ScriptTimer timer in _timer_off)
            {
                timer.Reset(false);
            }
            foreach (ScriptTimer timer in _timer_on)
            {
                timer.Reset(true);
            }
        }
    }
}
