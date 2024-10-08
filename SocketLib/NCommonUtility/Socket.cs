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
using static SocketLib.Program;

namespace SocketLib.NCommonUtility
{
    /// <summary>
    /// NSocketクラス
    /// </summary>
    public class NSocket
    {
        protected Socket _soc = null;
        protected IPEndPoint _self_EndPoint = null;
        protected IPEndPoint _remote_EndPoint = null;
        protected byte[] _dat = new byte[1024];
        public int DataSize { 
            get { return _dat.Length; } 
            set { _dat = new byte[value];  } 
        }

        public IPAddress SelfIPAddress { get { return _self_EndPoint?.Address; } }
        public int? SelfPortno { get { return _self_EndPoint?.Port; } }

        public IPAddress RemoteIPAddress { get { return _remote_EndPoint?.Address; } }
        public int? RemotePortno { get { return _remote_EndPoint?.Port; } }

        public event ThreadExceptionEventHandler OnExceptionEvent;
        public event DisConnectEventHandler OnDisConnectEvent;
        public event FailListenEventHandler OnFailListenEvent;
        public event AcceptEventHandler OnAcceptEvent;
        public event SendRecvEventHandler OnRecvEvent;
        public event SendRecvEventHandler OnSendEvent;




        protected void OnException(Exception e)
        {
            if (OnExceptionEvent == null)
            {
                throw e;
            }
            OnExceptionEvent.Invoke(this, new ThreadExceptionEventArgs(e));
        }

        protected void OnDisConnect()
        {
            lock (this)
            {
                _soc?.Close();
                _soc = null;
            }
            OnDisConnectEvent?.Invoke(this, new DisConnectEventArgs(this));
        }

        protected void OnAccept(ServerSocket serverSocket)
        {
            OnAcceptEvent?.Invoke(this, new AcceptEventArgs(this, serverSocket));

            // 受信スレッド起動
            serverSocket.StartRecvThread();

            // Acceptの処理が終わったら引き続きlisten処理を実行する
            Listen();
        }

        protected void OnFailListen()
        {
            lock (this)
            {
                _soc?.Close();
                _soc = null;
            }
            OnFailListenEvent?.Invoke(this, new ListenEventArgs(this));
        }

        protected void OnRecv()
        {
            int len =_soc.Receive(_dat);
            byte[] buf = new byte[len];
            Buffer.BlockCopy(_dat, 0, buf, 0, len);
            OnRecvEvent?.Invoke(this, new SendRecvEventArgs(this, buf));
        }

        protected void OnSend(byte[] buf)
        {
            OnSendEvent?.Invoke(this, new SendRecvEventArgs(this, buf));
        }

        public void SetSelfEndPoint(string iaddr, string portno)
        {
            SetSelfEndPoint(IPAddress.Parse(iaddr), int.Parse(portno));
        }
        public void SetSelfEndPoint(string iaddr, int portno = 0)
        {
            SetSelfEndPoint(IPAddress.Parse(iaddr), portno);
        }
        public void SetSelfEndPoint(IPAddress iaddr, int portno = 0)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            if (!ipHostInfo.AddressList.Contains<IPAddress>(iaddr))
            {
                throw new Exception("指定されたIPアドレスがホストに存在しません");
            }
            _self_EndPoint = new IPEndPoint(iaddr, portno);
        }

        public void Close()
        {
            lock (this)
            {
                _soc?.Close();
                _soc = null;
            }
        }


        public void Listen()
        {
            try
            {
                if (_self_EndPoint == null)
                {
                    throw new Exception("EndPointが指定されていません");
                }
                lock (this)
                {
                    if (_soc == null)
                    {
                        _soc = new Socket(_self_EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        _soc.Bind(_self_EndPoint);
                        _soc.Listen(1);     // 一度に一個だけ受け付ける
                        _soc.BeginAccept(new AsyncCallback(AcceptCallback), this);
                    }
                    else
                    {
                        // 2回目以降
                        _soc.BeginAccept(new AsyncCallback(AcceptCallback), this);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFailListen();
                OnException(ex);
            }
        }

        protected virtual void AcceptCallback(IAsyncResult ar)
        {
            NSocket socket = (NSocket)ar.AsyncState;
            lock (socket)
            {
                if (socket._soc == null)
                {
                    return;
                }
                try
                {
                    Socket soc = socket._soc.EndAccept(ar);
                    OnAccept(new ServerSocket(soc, socket._self_EndPoint));
                }
                catch (System.ObjectDisposedException ex)
                {
                    OnException(ex);
                }
            }
        }

        protected void StartRecvThread()
        {
            Task.Run(() => recvThread(this));
        }

        /// <summary>
        /// 受信スレッド
        /// </summary>
        /// <param name="_this"></param>
        static private void recvThread(NSocket _this)
        {
            try
            {
                while (true)
                {
                    if (_this._soc ==null ||_this._soc.Connected == false)
                    {
                        break;
                    }
                    _this.OnRecv();
                }
            }
            catch (Exception ex)
            {
                _this.OnException(ex);
            }
            finally
            {
                _this.OnDisConnect();
            }
        }

        public void Send(byte[] dat)
        {
            try
            {
                byte[] buf = new byte[dat.Length];
                Buffer.BlockCopy(dat, 0, buf, 0, dat.Length);

                _soc.BeginSend(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(SendCallback), (this, buf));
            }
            catch (Exception e)
            {
                OnDisConnect();
                OnException(new Exception("送信中に例外発生", e));
            }
        }

        protected void SendCallback(IAsyncResult ar)
        {
            var (socket, buf) = ((NSocket, byte[]))ar.AsyncState;
            lock (socket)
            {
                if (socket._soc == null)
                {
                    return;
                }
                try
                {
                    socket._soc.EndSend(ar);
                    OnSend(buf);
                }
                catch (System.ObjectDisposedException ex)
                {
                    OnDisConnect();
                    OnException(new Exception("ソケット送信エラー", ex));
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
        public ServerSocket(Socket soc, IPEndPoint self_EndPoint=null)
        {
            _soc = soc;
            _remote_EndPoint = (IPEndPoint)soc.RemoteEndPoint;
            _self_EndPoint = self_EndPoint;
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
                if (_soc != null)
                {
                    StartRecvThread();
                }
            }
        }

        protected void OnFailConnect()
        {
            OnFailConnectEvent?.Invoke(this, new ConnectEventArgs(this));
        }

        public void Connect(string iaddr, string portno)
        {
            Connect(IPAddress.Parse(iaddr), int.Parse(portno));
        }
        public void Connect(string iaddr, int portno)
        {
            Connect(IPAddress.Parse(iaddr), portno);
        }
        public void Connect(IPAddress iaddr, int portno)
        {
            try
            {
                if(_soc != null)
                {
                    //Todo: 
                }
                _remote_EndPoint = new IPEndPoint(iaddr, portno);
                _soc = new Socket(_remote_EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (_self_EndPoint != null)
                {
                    _soc.Bind(_self_EndPoint);
                }
                _soc.BeginConnect(_remote_EndPoint, new AsyncCallback(ConnectCallback), this);
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
                if (socket._soc == null)
                {
                    return;
                }
                if (socket._soc != this._soc)
                {
                    return;
                }
                try
                {
                    socket._soc.EndConnect(ar);
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

    public delegate void SendRecvEventHandler(Object sender, SendRecvEventArgs args);
    public class SendRecvEventArgs : EventArgs
    {
        public NSocket socket { get; private set; }
        public byte[] dat { get; private set; }

        public SendRecvEventArgs(NSocket socket, byte[] dat)
        {
            this.socket = socket;
            this.dat = dat;
        }
    }

    #endregion
}
