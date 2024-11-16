using NCommonUtility;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public delegate void CommMessageEventHandler(Object sender, CommMessageEventArgs args);
    public class CommMessageEventArgs : EventArgs
    {
        public CommSocket Socket { get; private set; }
        public CommMessage CommMsg { get; private set; }

        public CommMessageEventArgs(CommSocket socket, CommMessage msg)
        {
            Socket = socket;
            CommMsg = msg;
        }
    }

    public class CommSocket: NSocketEx
    {
        public event CommMessageEventHandler OnSendCommEvent;
        public event CommMessageEventHandler OnPreSendCommEvent;
        public event CommMessageEventHandler OnRecvCommEvent;

        public CommSocket() : base() 
        {
            init();
        }

        public CommSocket(Socket soc) : base(soc)
        {
            init();
        }

        private void init()
        {
            MessageDefine def = CommMessageDefine.GetInstance().GetMessageDefine("head");
            int ofs = -1;
            int len = -1;
            bool bPacket = false;
            if (def.ContainsKey("plen"))
            {
                // 'plen'はパケット長（ヘッダ長を含む）
                bPacket = true;
                ofs = def.GetFldDefine("plen").Offset;
                len = def.GetFldDefine("plen").Length;
            }
            else
            {
                if (def.ContainsKey("dlen"))
                {
                    // 'dlen'はデータ長
                    ofs = def.GetFldDefine("dlen").Offset;
                    len = def.GetFldDefine("dlen").Length;
                }
                else
                {
                    throw new Exception($"headの定義に'dlen'または'plen'が含まれていません");
                }
            }
            base.init(def.DLength, ofs, len, bPacket);
        }

        protected override void AcceptCallback(IAsyncResult ar)
        {
            CommSocket socket = (CommSocket)ar.AsyncState;
            if (socket._soc == null)
            {
                return;
            }
            try
            {
                Socket soc = socket._soc.EndAccept(ar);
                OnAccept(new CommSocket(soc));
            }
            catch (Exception ex)
            {
                OnDisConnect();
                OnException(ex);
            }
        }
        protected override void OnRecvEx()
        {
            CommMessage msg = new CommMessage(_comm_header, _comm_data);
            OnRecvCommEvent?.Invoke(this, new CommMessageEventArgs(this, msg));
        }

        public void Send(CommMessage msg)
        {
            OnPreSendCommEvent?.Invoke(this, new CommMessageEventArgs(this, msg));
            Send(msg.GetHead(), msg.GetData());
            OnSendCommEvent?.Invoke(this, new CommMessageEventArgs(this, msg));
        }
    }
}
