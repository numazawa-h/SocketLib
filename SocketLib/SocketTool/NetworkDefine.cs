using NCommonUtility;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class NetworkDefine
    {
        // シングルトン
        static private NetworkDefine _instance = null;
        static public NetworkDefine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new NetworkDefine();
            }
            return _instance;
        }
        private NetworkDefine() : base()
        {
        }

        protected Dictionary<string, (string script_path, string message_path)> _working_define = new Dictionary<string, (string, string)>();
        protected Dictionary<string, (string desc, RuntimeWorkingArea working, IPEndPoint local_addr, IPEndPoint remote_addr)> _listen_addr = new Dictionary<string, (string, RuntimeWorkingArea, IPEndPoint, IPEndPoint)>();
        protected Dictionary<string, (string desc, RuntimeWorkingArea working, IPEndPoint local_addr, IPEndPoint remote_addr)> _connect_addr = new Dictionary<string, (string, RuntimeWorkingArea, IPEndPoint, IPEndPoint)>();
        protected List<string> _names = new List<string>();


        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);
            _working_define.Clear();
            _listen_addr.Clear();
            _connect_addr.Clear();
            _names.Clear();

            foreach (Node node in root["working"])
            {
                string name = node["name"].Required();
                string script_path = node["script"].Required();
                string message_path = node["message"].Required();
                _working_define.Add(name, (script_path, message_path));
            }
            foreach (Node node in root["listen_addr"])
            {
                string desc = node["desc"].Required();
                string working = node["working"].Required();
                if (_working_define.ContainsKey(working) == false)
                {
                    throw new Exception($"listen_addr({desc})で指定された{working}が定義されていません({path})");
                }
                RuntimeWorkingArea runtime = GetRuntimeWorkingArea(working);
                string iaddr = node["ip"].Required();
                int portno = node["port"].Required();
                IPEndPoint local_addr = NSocket.GetIPEndPoint(iaddr, portno);
                IPEndPoint remote_addr = null;
                iaddr = node["remote-ip"];
                if(iaddr != null)
                {
                    portno = node["remote-port"].Required();
                    remote_addr = NSocket.GetIPEndPoint(iaddr, portno);
                }

                _listen_addr.Add(desc, (desc, runtime, local_addr, remote_addr));
                _names.Add(desc);
            }
            foreach (Node node in root["connect_addr"])
            {
                string desc = node["desc"].Required();
                string working = node["working"].Required();
                if (_working_define.ContainsKey(working) == false)
                {
                    throw new Exception($"connect_addr({desc})で指定された{working}が定義されていません({path})");
                }
                RuntimeWorkingArea runtime = GetRuntimeWorkingArea(working);
                string iaddr = node["ip"].Required();
                int portno = node["port"].Required();
                IPEndPoint remote_addr = NSocket.GetIPEndPoint(iaddr, portno);
                IPEndPoint local_addr = null;
                iaddr = node["local-ip"];
                if (iaddr != null)
                {
                    portno = node["local-port"].Required();
                    local_addr = NSocket.GetIPEndPoint(iaddr, portno);
                }
                _connect_addr.Add(desc, (desc, runtime, local_addr, remote_addr));
                _names.Add(desc);
            }
        }

        private RuntimeWorkingArea GetRuntimeWorkingArea(string working)
        {
            string message_path = _working_define[working].message_path;
            Dictionary<string, MessageDefine> messages;
            Dictionary<string, ValuesDefine> values;
            (messages, values) = CommMessageDefine.GetInstance().ReadJson(message_path);

            string script_path = _working_define[working].script_path;
            WorkingArea workarea;
            ScriptGroupDefine scripts;
            (workarea,scripts) = ScriptDefine.ReadJson(script_path);

            return new RuntimeWorkingArea(workarea, scripts, messages, values);
        }

        public string[] GetNames()
        {
            return _names.ToArray();
        }

        public bool isListenAddr(string name)
        {
            return _listen_addr.ContainsKey(name);
        }

        public bool isConnectAddr(string name)
        {
            return _connect_addr.ContainsKey(name);
        }

        public IPEndPoint GetLocalEndPoint(string name)
        {
            if (_listen_addr.ContainsKey(name)){
                (_, _, IPEndPoint endPoint, _) = _listen_addr[name];
                return endPoint;
            }
            if (_connect_addr.ContainsKey(name))
            {
                (_, _, IPEndPoint endPoint, _) = _connect_addr[name];
                return endPoint;
            }
            return null;
        }

        public IPEndPoint GetRemoteEndPoint(string name)
        {
            if (_listen_addr.ContainsKey(name))
            {
                (_, _, _, IPEndPoint endPoint) = _listen_addr[name];
                return endPoint;
            }
            if (_connect_addr.ContainsKey(name))
            {
                (_, _, _, IPEndPoint endPoint) = _connect_addr[name];
                return endPoint;
            }
            return null;
        }

        public RuntimeWorkingArea GetConnectRuntime(string name)
        {
            if (_connect_addr.ContainsKey(name))
            {
                (_, RuntimeWorkingArea runtime, _, _) = _connect_addr[name];
                return runtime;
            }
            return null;
        }

        public RuntimeWorkingArea GetListenRuntime(string name)
        {
            if (_listen_addr.ContainsKey(name))
            {
                (_, RuntimeWorkingArea runtime, _, _) = _listen_addr[name];
                return runtime;
            }
            return null;
        }
    }
}
