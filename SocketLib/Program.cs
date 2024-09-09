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
            Application.Run(new MainForm());

            ByteArrayTest("[ 1234 5678 9abc def0 ]");
        }

        static void ByteArrayTest(string bcd)
        {
            Log.Trace(bcd);
            ByteArray ba1 = new ByteArray(bcd);
            ByteArray ba2 = new ByteArray("[  31323334 c1c2c3c4 41424344]");

            ba1.Append(ba1);
            ba1.Append(ba2);
            Log.Debug("ByteArrayTest:[" + ba1.ToString() +"]");

            ba1.SetSeparator(null, " ", "    ");
            Log.Debug("ByteArrayTest:[" + ba1.ToString() + "]");
            ba1.SetSeparator(" ", "  ", "]  [");
            Log.Debug("ByteArrayTest:[" + ba1.ToString() + "]");

            Log.Debug("ByteArrayTest:[" + ba1.to_hex() + "]");
            Log.Debug("ByteArrayTest:[" + ba2.to_text() + "]");
        }
    }
}
