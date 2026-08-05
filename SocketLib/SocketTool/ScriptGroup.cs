using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    public class ScriptGroup
    {
        public string ID { get; protected set; }
        public string When { get; protected set; }
        public bool Display { get; protected set; }
        public bool Enabled = false;
        protected List<Script> _scripts = new List<Script>();
        WorkingArea _workarea;
        public ScriptGroup(Node def, Dictionary<string, Command> comands, WorkingArea workarea)
        {
            _workarea = workarea;
            ID = def["id"].Required();
            When = def["when"].Required();
            if (def.ContainsKey("checked"))
            {
                Display = true;
                Enabled = def["checked"].Required();
            }
            else
            {
                Display = false;
                if (When == "timer")
                {
                    Enabled = false;
                }
                else
                {
                    Enabled = true;
                }
            }
            if (def.ContainsKey("scripts"))
            {
                foreach (Node _def in def["scripts"])
                {
                    _scripts.Add(new Script(_def, comands, this));
                }
            }
            else
            {
                _scripts.Add(new Script(def, comands, this));
            }
        }

        public CommMessage GetValueMsg(string id)
        {
            return _workarea.GetValueMsg(id);
        }

        public bool Exec(CommSocket socket, CommMessage msg = null)
        {
            if (When == "disp")
            {
                return _scripts[0].Exec(socket, msg);
            }
            if (Enabled == false)
            {
                return false;
            }
            foreach (var script in _scripts)
            {
                script.Exec(socket, msg);
            }
            return true;
        }
    }

    public class ScriptGroupOnTimer : ScriptGroup
    {
        private int _dueTime;
        private int _period;
        private int _phaseCnt;
        private int _repeatCnt;
        private int _phase = 0;
        private int _repeat = 0;
        private Timer _timer = null;
        CommSocket _socket = null;

        public ScriptGroupOnTimer(Node def, Dictionary<string, Command> comands, WorkingArea workarea) : base(def, comands, workarea)
        {
            _dueTime = (int?)def["start"] is int v1 ? v1 : 0;
            _period = def["interval"].Required();
            _phaseCnt = (int?)def["phaseCnt"] is int v2 ? v2 : 0;
            _repeatCnt = (int?)def["repeatCnt"] is int v3 ? v3 : 0;
        }

        public void Start(CommSocket socket)
        {
            lock (this)
            {
                Stop();
                _phase = 0;
                _repeat = 0;
                _socket = socket;
                _timer = new Timer(new TimerCallback(TimerTask), this, _dueTime, _period);
            }
        }

        public void Stop()
        {
            lock (this)
            {
                _timer?.Dispose();
                _timer = null;
                _socket = null;
            }
        }

        public void Reset(bool enable)
        {
            _phase = 0;
            _repeat = 0;
            Enabled = enable;
        }

        private void Exec()
        {
            if (_socket != null && _socket.isOpen == false)
            {
                Stop();
            }
            if (Enabled == false || _socket == null)
            {
                return;
            }
            if (_repeatCnt > 0 && _repeat >= _repeatCnt)
            {
                return;
            }

            foreach (var script in _scripts)
            {
                if (script.OnPhase(_phase))
                {
                    script.Exec(_socket);
                }
            }

            if (_phase < _phaseCnt)
            {
                ++_phase;
                if (_phase == _phaseCnt)
                {
                    _phase = 0;
                    if (_repeatCnt > 0)
                    {
                        ++_repeat;
                    }
                }
            }
        }

        private static void TimerTask(object obj)
        {
            (obj as ScriptGroupOnTimer).Exec();
        }
    }
}

