using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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


        public event SendRecvExEventHandler OnRecvExEvent;


        public byte[] _comm_header=null;
        public byte[] _comm_data=null;
        private int _head_recv_cnt = -1;
        private int _data_recv_cnt = -1;
        private int _data_length = -1;

        public NSocketEx(int hsize, int dlen_ofs, int dlen_size=2)
        {
            if (dlen_ofs <0 )
            {
                throw new ArgumentException("データ長オフセットは、0以上で指定してください");
            }
            if ((new int[] { 1, 2, 4 }).Contains(dlen_size) == false)
            {
                throw new ArgumentException("データ長サイズは、1,2,4のいずれかで指定してください");
            }
            if (hsize< (dlen_ofs+dlen_size))
            {
                throw new ArgumentException("ヘッダサイズが小さすぎます");
            }
            HeaderSize = hsize;
            DataLenOffset = dlen_ofs;
            DataLenSize = dlen_size;
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
                lock (this)
                {
                    OnRecvExEvent?.Invoke(this, new SendRecvExEventArgs(this, _comm_header, _comm_data));
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
            lock (this)
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
        }

        private void receiveData()
        {
            lock (this)
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

            return BitConverter.ToInt32(dlen, 0);
        }
    }
}
