using NCommonUtility;
using SampleMain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketLib
{
    internal static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Log.Init();
            Log.Info("Aplication Started*******************************************");

            JsonTest();
            ByteArrayTest("[ 1234 5678 9abc def0 ]");


            Application.Run(new MainForm());

        }

        static void ByteArrayTest(string hex)
        {
            Log.Trace(hex);
            ByteArray ba1 = new ByteArray(ByteArray.ParseHex(hex));
            ByteArray ba2 = new ByteArray(ByteArray.ParseHex("[  31323334 c1c2c3c4 41424344]"));
            ByteArray ba3 = new ByteArray("テスト");
            ByteArray ba4 = new ByteArray("あいうえお", System.Text.Encoding.GetEncoding("Shift_JIS"));


            ba1.Append(ba1);
            ba1.Append(ba2);
            ba1.Append(ba4);

            Log.Debug("ByteArrayTest:[" + ba1.ToString() +"]");
            Log.Debug("ByteArrayTest:[" + ba1.to_hex(4,0,"-") + "]");
            Log.Debug("ByteArrayTest:[" + ba1.to_text_ascii(0, 0, " ") + "]");
            Log.Debug("ByteArrayTest:[" + ba1.to_text_utf8(24) + "]");
            Log.Debug("ByteArrayTest:[" + ba1.to_text_sjis(24) + "]");

            Log.Debug("ByteArrayTest:[" + ba3.to_text() + "]");
            Log.Debug("ByteArrayTest:[" + ba3.to_hex() + "]");

            ByteArray ba;
            ba = new ByteArray(ba2);
            Log.Debug("ByteArrayTest:( 0,  0)[" + ba.to_hex() + "]");
            ba = new ByteArray(ba2, 8, 16);
            Log.Debug("ByteArrayTest:( 8, 16)[" + ba.to_hex() + "]");
            ba = new ByteArray(ba2, 0, -16);
            Log.Debug("ByteArrayTest:( 0,-16)[" + ba.to_hex() + "]");
            ba = new ByteArray(ba2, 0, -4);
            Log.Debug("ByteArrayTest:( 0, -4)[" + ba.to_hex() + "]");
            ba = new ByteArray(ba2, -4, 8);
            Log.Debug("ByteArrayTest:(-4,  8)[" + ba.to_hex() + "]");
            ba = new ByteArray(ba2, -4, 20);
            Log.Debug("ByteArrayTest:(-4, 20)[" + ba.to_hex() + "]");

            ba = new ByteArray(ba3, 0, 20);
            Log.Debug("ByteArrayTest:[" + ba.to_hex() + "]");
            int len = ba.str_len();
            Log.Debug("ByteArrayTest:str_len =" + len.ToString());
            ba = new ByteArray(ba3, 0, len);
            Log.Debug("ByteArrayTest:[" + ba.to_text() + "]");

        }

        public enum Week
        {
            nodata =0,
            Monday = 1,
            Tuesday = 2,
            Wednesday = 3,
            Thursday = 4,
            Friday = 5,
            Saturday = 6,
            Sunday = 7,
        }


        static void JsonTest()
        {

            Type t = typeof(Week);
            string[] w =Enum.GetNames(t);
            Week w1 = (Week)Enum.Parse(t, w[1]);


            try
            {
                JsonConfig.RootNode root = JsonConfig.ReadJson(".\\config\\test.json");

                DateTime dt = root["日付"].SetFormat("yyyy/MM/dd").Required();
                int h1 = root["H1"].Required();
                int h2 = root["H2"].Required();
                int? h3 = root["H3"];
                Week W1 = root["W1"].Required().GetEnum<Week>();
                Week W2 = root["W2"].GetEnum<Week>();
                Week W3 = root["W3"].GetEnum<Week>();


                int max_datasize = (int?)root["max_datasize"] is int v ? v: 0;
                bool? auto_send = root["initdis"]["自動送信"];
                bool auto_resp = root["initdis"]["自動応答"].Required();
                int? _ = root["initdis"]["１系"]["受信側"]["xxx"];

                foreach (JsonConfig.Node node in root["remort"]["server2"])
                {
                    throw new NotImplementedException("空配列なのに実行");
                }

                foreach (JsonConfig.Node node in root["remort"]["server"])
                {
                    Addr addr = node.GetObject<Addr>();
                    Addr addr2 = new Addr(node["id"].Required(), node["ip"].Required());
                }
            }
            catch (Exception e)
            {
                Log.Error("JsonTestでエラー", e);
            }

        }
        public class Addr
        {
            public string id;
            public string ip;
            public Addr(string id, string ip) 
            {
                this.id = id;
                this.ip = ip;
            }
        }

    }
}
