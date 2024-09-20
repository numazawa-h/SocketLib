using NCommonUtility;
using SampleMain;
using System;
using System.Collections.Generic;
using System.Linq;
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

        static void JsonTest()
        {
            try
            {
                JsonConfig.RootNode root = JsonConfig.ReadJson(".\\config\\test.json");

                DateTime dt = (Required)root["日付"].SetFormat("yyyy/MM/dd");
                int max_datasize = (int?)root["max_datasize"] is int v ? v: 0;
                int auto_send = (Required)root["initdis"]["自動送信"];
                int? _ = root["initdis"]["１系"]["受信側"]["xxx"];

                foreach (JsonConfig.Node node in root["remort"]["server"])
                {
                    string id = node["id"];
                    string ip = node["ip"];
                    int xxx = (Required)node["xxx"];         
                }
            }
            catch (Exception e)
            {
                Log.Error("JsonTestでエラー", e);
            }

        }
    }
}
