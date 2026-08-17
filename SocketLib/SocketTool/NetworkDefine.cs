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

        protected Dictionary<string, (string script_path, string message_path)> _config = new Dictionary<string, (string, string)>();
        protected Dictionary<string, (string desc, string config, IPEndPoint local_addr, IPEndPoint remote_addr)> _listen_addr = new Dictionary<string, (string, string, IPEndPoint, IPEndPoint)>();
        protected Dictionary<string, (string desc, string config, IPEndPoint local_addr, IPEndPoint remote_addr)> _connect_addr = new Dictionary<string, (string, string, IPEndPoint, IPEndPoint)>();
        protected List<string> _names = new List<string>();


        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);
            _config.Clear();
            _listen_addr.Clear();
            _connect_addr.Clear();
            _names.Clear();

            foreach (Node node in root["config"])
            {
                string name = node["name"].Required();
                string script_path = node["script"].Required();
                string message_path = node["message"].Required();
                _config.Add(name, (script_path, message_path));
            }
            foreach (Node node in root["listen_addr"])
            {
                string desc = node["desc"].Required();
                string config = node["config"].Required();
                if (_config.ContainsKey(config) == false)
                {
                    throw new Exception($"listen_addr({desc})で指定された{config}が定義されていません({path})");
                }
                string iaddr = node["local-ip"].Required();
                int portno = node["local-port"].Required();
                IPEndPoint local_addr = NSocket.GetIPEndPoint(iaddr, portno);
                IPEndPoint remote_addr = null;
                iaddr = node["remote-ip"];
                if(iaddr != null)
                {
                    portno = node["remote-port"].Required();
                    remote_addr = NSocket.GetIPEndPoint(iaddr, portno);
                }

                _listen_addr.Add(desc, (desc, config, local_addr, remote_addr));
                _names.Add(desc);
            }
            foreach (Node node in root["connect_addr"])
            {
                string desc = node["desc"].Required();
                string config = node["config"].Required();
                if (_config.ContainsKey(config) == false)
                {
                    throw new Exception($"connect_addr({desc})で指定された{config}が定義されていません({path})");
                }
                string iaddr = node["remote-ip"].Required();
                int portno = node["remote-port"].Required();
                IPEndPoint remote_addr = NSocket.GetIPEndPoint(iaddr, portno);
                IPEndPoint local_addr = null;
                iaddr = node["local-ip"];
                if (iaddr != null)
                {
                    portno = node["local-port"].Required();
                    local_addr = NSocket.GetIPEndPoint(iaddr, portno);
                }
                _connect_addr.Add(desc, (desc, config, local_addr, remote_addr));
                _names.Add(desc);
            }
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
        public string GetConfig(string name)
        {
            if (_listen_addr.ContainsKey(name))
            {
                (_, string config, _, _) = _listen_addr[name];
                return config;
            }
            if (_connect_addr.ContainsKey(name))
            {
                (_, string config, _, _) = _connect_addr[name];
                return config;
            }
            return null;
        }

        public string GetScriptPath(string config)
        {
            if (_config.ContainsKey(config))
            {
                return _config[config].script_path;
            }
            return null;
        }

        public string GetMessagePath(string config)
        {
            if (_config.ContainsKey(config))
            {
                return _config[config].message_path;
            }
            return null;
        }
    }
}
