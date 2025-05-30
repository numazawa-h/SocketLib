﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NCommonUtility
{
    public delegate void SendRecvExEventHandler(Object sender, SendRecvExEventArgs args);
    public class SendRecvExEventArgs : EventArgs
    {
        public NSocketEx Socket { get; private set; }
        public byte[] Comm_header { get; private set; }
        public byte[] Comm_data { get; private set; }

        public SendRecvExEventArgs(NSocketEx socket, byte[] hed, byte[] dat)
        {
            Socket = socket;
            Comm_header = hed;
            Comm_data = dat;
        }
    }

    public class NSocketEx : NSocket
    {
        public int HeaderSize { get; private set; }
        public int DataLenOffset { get; private set; }
        public int DataLenSize { get; private set; }
        public bool DataLenIsPacketSize { get; private set; }

        public event SendRecvExEventHandler OnSendExEvent;
        public event SendRecvExEventHandler OnRecvExEvent;


        protected byte[] _comm_header=null;
        protected byte[] _comm_data=null;
        private int _head_recv_cnt = -1;
        private int _data_recv_cnt = -1;
        private int _data_length = -1;


        protected NSocketEx()
        {

        }
        protected NSocketEx(Socket soc) : base(soc)
        {

        }

        public NSocketEx(int hsize, int dlen_ofs, int dlen_size=2, bool bPacket=false)
        {
            init(hsize, dlen_ofs, dlen_size, bPacket);
        }

        public NSocketEx(Socket soc, int hsize, int dlen_ofs, int dlen_size = 2, bool bPacket = false) : base(soc)
        {
            init(hsize, dlen_ofs, dlen_size, bPacket);
        }

        protected void init(int hsize, int dlen_ofs, int dlen_size, bool bPacket)
        {
            if (dlen_ofs < 0)
            {
                throw new ArgumentException("データ長オフセットは、0以上で指定してください");
            }
            if ((new int[] { 1, 2, 4 }).Contains(dlen_size) == false)
            {
                throw new ArgumentException("データ長サイズは、1,2,4のいずれかで指定してください");
            }
            if (hsize < (dlen_ofs + dlen_size))
            {
                throw new ArgumentException("ヘッダサイズが小さすぎます");
            }
            DataLenIsPacketSize = bPacket;
            HeaderSize = hsize;
            DataLenOffset = dlen_ofs;
            DataLenSize = dlen_size;
        }

        protected virtual void OnRecvEx()
        {
            OnRecvExEvent?.Invoke(this, new SendRecvExEventArgs(this, _comm_header, _comm_data));
        }

        protected override void OnRecv()
        {
            if (_head_recv_cnt < HeaderSize)
            {
                receiveHead();
                if (_head_recv_cnt >= HeaderSize)
                {
                    _data_length = getDataLength();
                    if (_data_length == 0)
                    {
                        _comm_data = System.Array.Empty<byte>();
                        _data_recv_cnt = 0;
                    }
                }
            }
            else if (_data_recv_cnt < _data_length)
            {
                receiveData();
            }

            if (_data_recv_cnt == _data_length)
            {
                OnRecvEx();
                lock (this)
                {
                    _comm_header = null;
                    _comm_data = null;
                    _head_recv_cnt = -1;
                    _data_recv_cnt = -1;
                    _data_length = -1;
                }
            }
        }

        private void receiveHead()
        {
            if (_head_recv_cnt < 0)
            {
                // ヘッダ受信の準備
                _comm_header = new byte[HeaderSize];
                _head_recv_cnt = 0;
            }

            int rcnt = _soc.Receive(_comm_header, _head_recv_cnt, HeaderSize - _head_recv_cnt, SocketFlags.None);
            if (rcnt <= 0)
            {
                throw new Exception($"socket receive error({rcnt})");
            }
            _head_recv_cnt += rcnt;
        }

        private void receiveData()
        {
            if (_data_recv_cnt < 0)
            {
                // データ受信の準備
                _comm_data = new byte[_data_length];
                _data_recv_cnt = 0;
            }

            int rcnt = _soc.Receive(_comm_data, _data_recv_cnt, _data_length - _data_recv_cnt, SocketFlags.None);
            if (rcnt <= 0)
            {
                throw new Exception($"socket receive error({rcnt})");
            }
            _data_recv_cnt += rcnt;
        }

        private int getDataLength()
        {
            byte[] dlen = new byte[4];
            switch (DataLenSize)
            {
                case 1:
                    dlen[3] = _comm_header[DataLenOffset];
                    break;
                case 2:
                    dlen[2] = _comm_header[DataLenOffset];
                    dlen[3] = _comm_header[DataLenOffset + 1];
                    break;
                case 4:
                    dlen[0] = _comm_header[DataLenOffset];
                    dlen[1] = _comm_header[DataLenOffset + 1];
                    dlen[2] = _comm_header[DataLenOffset + 2];
                    dlen[3] = _comm_header[DataLenOffset + 3];
                    break;
            }
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(dlen);
            }

            int len = BitConverter.ToInt32(dlen, 0);
            if (DataLenIsPacketSize)
            {
                // ヘッダのレングスがヘッダ長を含むならヘッダ長を引く
                len -= HeaderSize;
            }
            return len;
        }

        protected virtual void OnSend(byte[] hed, byte[] dat)
        {
            OnSendExEvent?.Invoke(this, new SendRecvExEventArgs(this, hed, dat));
        }

        public void Send(byte[] hed, byte[] dat)
        {
            try
            {
                byte[] h = new byte[hed.Length];
                byte[] d = new byte[dat.Length];
                byte[] buf = new byte[hed.Length+dat.Length];

                Buffer.BlockCopy(hed, 0, h, 0, hed.Length);
                Buffer.BlockCopy(dat, 0, d, 0, dat.Length);
                Buffer.BlockCopy(hed, 0, buf, 0, hed.Length);
                Buffer.BlockCopy(dat, 0, buf, hed.Length, dat.Length);

                _soc.BeginSend(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(SendCallback), (this, h,d));
            }
            catch (Exception e)
            {
                OnDisConnect();
                OnException(new Exception("送信中に例外発生", e));
            }
        }

        protected override void AcceptCallback(IAsyncResult ar)
        {
            NSocketEx socket = (NSocketEx)ar.AsyncState;
            if (socket._soc == null)
            {
                return;
            }
            try
            {
                Socket soc = socket._soc.EndAccept(ar);
                OnAccept(new NSocketEx(soc, this.HeaderSize, this.DataLenOffset, this.DataLenSize, this.DataLenIsPacketSize));
            }
            catch (Exception ex)
            {
                OnDisConnect();
                OnException(ex);
            }
        }

        protected override void SendCallback(IAsyncResult ar)
        {
            var (socket, hed, dat) = ((NSocketEx, byte[], byte[]))ar.AsyncState;
            if (socket._soc == null)
            {
                return;
            }
            try
            {
                socket._soc.EndSend(ar);
                OnSend(hed, dat);
            }
            catch (System.ObjectDisposedException ex)
            {
                OnDisConnect();
                OnException(new Exception("ソケット送信エラー", ex));
            }
        }
    }
}
