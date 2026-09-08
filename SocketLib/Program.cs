using NCommonUtility;
using SampleMain;
using SocketTool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SocketLib.Program;

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
            Application.ThreadException += Application_ThreadException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Log.Init();
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var ver = assembly.Version;
                Log.Info($"{assembly.Name} (version{ver.Major}.{ver.Minor}.{ver.Build}) Started*******************************************");
                NetworkDefine.GetInstance().ReadJson(".\\config\\NetworkDefine.json");

                // 起動時に空読みして定義エラーがないか確認する
                foreach (string name in NetworkDefine.GetInstance().GetNames())
                {
                    string config = NetworkDefine.GetInstance().GetConfig(name);
                    RuntimeWorkingArea runtime = new RuntimeWorkingArea(config);
                }
            }
            catch (Exception ex)
            {
                Log.Error("初期化エラー", ex);
                Environment.Exit(1);
            }
            Application.Run(new MainForm());


        }
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
            Log.Error("内部エラー", e.Exception);

        }
    }
}
