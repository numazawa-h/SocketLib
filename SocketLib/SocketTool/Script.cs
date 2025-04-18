﻿using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static NCommonUtility.JsonConfig;

namespace NCommonUtility
{
    public class ScriptList
    {
        public string ID { get; protected set; }
        public string When { get; protected set; }
        public bool Display { get; protected set; }
        public bool Enabled = false;
        protected List<Script> _scripts = new List<Script>();
        public ScriptList(Node def, Dictionary<string, Command> comands)
        {
            ID = def["id"].Required();
            When = def["when"].Required();
            if(def.ContainsKey("checked"))
            {
                Display = true;
                Enabled = def["checked"].Required();
            }
            else
            {
                Display = false;
                Enabled = true;
            }
            if (def.ContainsKey("scripts"))
            {
                foreach(Node _def in def["scripts"])
                {
                    _scripts.Add(new Script(_def, comands, this));
                }
            }
            else
            {
                _scripts.Add(new Script(def, comands, this));
            }
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

    public class ScriptTimer : ScriptList
    {
        private int _dueTime;
        private int _period;
        private Timer _timer = null;
        private CommMessage _msg = null;
        CommSocket _socket = null;

        public ScriptTimer(Node def, Dictionary<string, Command> comands): base(def, comands)
        {
            _dueTime = (int?)def["start"] is int v? v:0;
            _period = def["interval"].Required();
            string msgname = def["msg"];
            if(msgname != null)
            {
                _msg = ScriptDefine.GetInstance().GetValueMsg(msgname);
            }
        }

        public void Start(CommSocket socket)
        {
            lock (this)
            {
                Stop();
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

            foreach (var script in _scripts)
            {
                script.Exec(_socket, _msg);
            }
        }

        private static void TimerTask(object obj)
        {
            (obj as ScriptTimer).Exec();
        }
    }

    public class Script
    {
        ScriptList _owner;
        protected HashSet<string> _dtypes = new HashSet<string>();
        protected HashSet<string> _without = new HashSet<string>();
        protected List<Command> _commands = new List<Command>();

        public Script(Node def, Dictionary<string, Command> comands, ScriptList owner)
        {
            _dtypes = def.GetStringValues("dtype");
            _without = def.GetStringValues("without");
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
                command.Exec(socket, msg);
            }
            return true;
        }
    }
}
