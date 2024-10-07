using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketLib.NCommonUtility
{
    /// <summary>
    /// NSocketクラス
    /// </summary>
    public class NSocket
    {
        protected Socket _Socket = null;
        protected IPEndPoint _self_EndPoint = null;
        protected IPEndPoint _remote_EndPoint = null;

        public event ThreadExceptionEventHandler OnExceptionEvent;
        public event DisConnectEventHandler OnDisConnectEvent;
        public event FailListenEventHandler OnFailListenEvent;
        public event AcceptEventHandler OnAcceptEvent;

        

        public void OnException(Exception e)
        {
            if (OnExceptionEvent == null)
            {
                throw e;
            }
            OnExceptionEvent.Invoke(this, new ThreadExceptionEventArgs(e));
        }
        public void OnDisConnect()
        {
            lock (this)
            {
                _Socket?.Close();
                _Socket = null;
            }
            OnDisConnectEvent?.Invoke(this, new DisConnectEventArgs(this));
        }
        protected void OnAccept(ServerSocket serverSocket)
        {
            OnAcceptEvent?.Invoke(this, new AcceptEventArgs(this, serverSocket));
            lock (this)
            {
                _Socket?.BeginAccept(new AsyncCallback(AcceptCallback), this);
            }
            lock (serverSocket)
            {
                if (_Socket != null)
                {
                    //TODO:受信スレッド開始
                }
            }
        }

        public void OnFailListen()
        {
            lock (this)
            {
                _Socket?.Close();
                _Socket = null;
            }
            OnFailListenEvent?.Invoke(this, new ListenEventArgs(this));
        }

        public void SetSelfEndPoint(IPAddress iaddr, int portno = 0)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            if (!ipHostInfo.AddressList.Contains<IPAddress>(iaddr))
            {
                new Exception("指定されたIPアドレスがホストに存在しません");
            }
            _self_EndPoint = new IPEndPoint(iaddr, portno);
        }

        public void Listen()
        {
            if (_self_EndPoint == null)
            {
                new Exception("EndPointが指定されていません");
            }
            try
            {
                lock (this)
                {
                    if (_Socket == null)
                    {
                        _Socket = new Socket(_self_EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        _Socket.Bind(_self_EndPoint);
                        _Socket.Listen(1);
                        _Socket.BeginAccept(new AsyncCallback(AcceptCallback), this);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFailListen();
                OnException(ex);
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            NSocket listensocket = (NSocket)ar.AsyncState;
            lock (listensocket)
            {
                if (listensocket._Socket == null)
                {
                    return;
                }
                try
                {
                    Socket soc = listensocket._Socket.EndAccept(ar);
                    OnAccept(new ServerSocket(soc));
                }
                catch (System.ObjectDisposedException ex)
                {
                    OnException(ex);
                }
            }
        }
    }

    /// <summary>
    /// サーバーSocket
    /// </summary>
    /// <remarks>Acceptで生成されるソケットクラスです。</remarks>
    public class ServerSocket : NSocket
    {
        public ServerSocket(Socket soc)
        {
            _Socket = soc;
            _remote_EndPoint = (IPEndPoint)soc.RemoteEndPoint;
        }
    }

    /// <summary>
    /// クライアントSocket
    /// </summary>
    /// <remarks>Connectで生成されるソケットを管理するクラスです。</remarks>
    public class ClientSocket : NSocket
    {
        public event ConnectEventHandler OnConnectEvent;
        public event ConnectEventHandler OnFailConnectEvent;

        protected void OnConnect()
        {
            OnConnectEvent?.Invoke(this, new ConnectEventArgs(this));
            lock (this)
            {
                if (_Socket != null)
                {
                    //TODO:受信スレッド開始
                }
            }
        }

        public void OnFailConnect()
        {
            OnFailConnectEvent?.Invoke(this, new ConnectEventArgs(this));
        }

        public void Connect(string iaddr, string portno)
        {
            try
            {
                Connect(IPAddress.Parse(iaddr), int.Parse(portno));
            }
            catch (Exception ex)
            {
                OnFailConnect();
                OnException(ex);
            }
        }
        public void Connect(string iaddr, int portno)
        {
            try
            {
                Connect(IPAddress.Parse(iaddr), portno);
            }
            catch (Exception ex)
            {
                OnFailConnect();
                OnException(ex);
            }
        }
        public void Connect(IPAddress iaddr, int portno)
        {
            try
            {
                if(_Socket != null)
                {
                    //Todo: 
                }
                _remote_EndPoint = new IPEndPoint(iaddr, portno);
                _Socket = new Socket(_remote_EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (_self_EndPoint != null)
                {
                    _Socket.Bind(_self_EndPoint);
                }
                _Socket.BeginConnect(_remote_EndPoint, new AsyncCallback(ConnectCallback), this);
            }
            catch (Exception ex)
            {
                OnFailConnect();
                OnException(ex);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            ClientSocket socket = (ClientSocket)ar.AsyncState;
            lock (socket)
            {
                if (socket._Socket == null)
                {
                    //TODO:
                    return;
                }
                try
                {
                    socket._Socket.EndConnect(ar);
                    OnConnect();
                }
                catch (Exception ex)
                {
                    OnFailConnect();
                    OnException(ex);
                }
            }
        }
    }



    #region EventHandler定義

    public delegate void FailListenEventHandler(Object sender, ListenEventArgs args);
    public class ListenEventArgs : EventArgs
    {
        public NSocket NSocket { get; private set; }
        public ListenEventArgs(NSocket socket)
        {
            NSocket = socket;
        }
    }

    public delegate void AcceptEventHandler(Object sender, AcceptEventArgs args);
    public class AcceptEventArgs : EventArgs
    {
        public NSocket ListenSocket { get; private set; }
        public ServerSocket ServerSocket { get; private set; }
        public AcceptEventArgs(NSocket listensocket, ServerSocket serverSocket)
        {
            ListenSocket = listensocket;
            ServerSocket = serverSocket;
        }
    }

    public delegate void ConnectEventHandler(Object sender, ConnectEventArgs args);
    public class ConnectEventArgs : EventArgs
    {
        public ClientSocket ClientSocket {  get; private set; }
        public ConnectEventArgs(ClientSocket socket)
        {
            ClientSocket = socket;
        }
    }

    public delegate void DisConnectEventHandler(Object sender, DisConnectEventArgs args);
    public class DisConnectEventArgs : EventArgs
    {
        public NSocket SocketBase { get; private set; }
        public DisConnectEventArgs(NSocket socket)
        {
            SocketBase = socket;
        }
    }

    #endregion
}
