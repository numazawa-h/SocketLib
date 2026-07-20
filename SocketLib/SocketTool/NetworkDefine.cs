using NCommonUtility;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

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

        protected Dictionary<string, (string desc, IPEndPoint local_addr, IPEndPoint remote_addr)> _listen_addr = new Dictionary<string, (string, IPEndPoint, IPEndPoint)>();
        protected Dictionary<string, (string desc, IPEndPoint local_addr, IPEndPoint remote_addr)> _connect_addr = new Dictionary<string, (string, IPEndPoint, IPEndPoint)>();
        protected List<string> _names = new List<string>();


        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);
            _listen_addr.Clear();
            _connect_addr.Clear();
            _names.Clear();
            foreach (Node node in root["listen_addr"])
            {
                string desc = node["desc"].Required();
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
                _listen_addr.Add(desc, (desc, local_addr, remote_addr));
                _names.Add(desc);
            }
            foreach (Node node in root["connect_addr"])
            {
                string desc = node["desc"].Required();
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
                _connect_addr.Add(desc, (desc, local_addr, remote_addr));
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
                (_, IPEndPoint endPoint, _) = _listen_addr[name];
                return endPoint;
            }
            if (_connect_addr.ContainsKey(name))
            {
                (_, IPEndPoint endPoint, _) = _connect_addr[name];
                return endPoint;
            }
            return null;
        }

        public IPEndPoint GetRemoteEndPoint(string name)
        {
            if (_listen_addr.ContainsKey(name))
            {
                (_, _, IPEndPoint endPoint) = _listen_addr[name];
                return endPoint;
            }
            if (_connect_addr.ContainsKey(name))
            {
                (_, _, IPEndPoint endPoint) = _connect_addr[name];
                return endPoint;
            }
            return null;
        }
    }
}
