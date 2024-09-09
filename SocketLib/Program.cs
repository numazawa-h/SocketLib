﻿using System;
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

            TraceLogTest("Trace Test");
        }

        static void TraceLogTest(string message)
        {
            Log.Trace();
            Log.Trace(message);
        }
    }
}
