using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
[assembly: log4net.Config.XmlConfigurator(Watch = true, ConfigFile = "./log4net.xml")]

namespace NCommonUtility
{
    /// <summary>
    /// log4net拡張クラス
    /// </summary>
    /// <remarks>
    /// 0.assembly定義
    /// 　assemblyの定義をしているのでxmlファイル名を変更するためにはこのソースの変更が必要。
    /// 1.グローバルログ
    ///   プログラム開始時に Log.Init()を実行することで、どこからでもログ出力できる。
    ///   ex) Log.Info(),Log.Debug()
    /// 2.引数なしのGetLogger()
    ///   Log.GetLogger()でlog4netでの定番コーディングである以下を代替できる。
    ///   LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
    /// 3.Traceレベルの追加
    /// 　引数なしでTrace()を呼ぶと、呼び出し元のメソッド名と行位置がTraceレベルで出力される。
    /// 4.引数ありのGetLogger()
    /// 　動的にFileAppenderを生成してログファイル名に引数のnameを付加する。
    /// </remarks>
    public class Log
    {
        // グローバルログ
        static private NLog _logger = null;

        private static readonly Type _thisDeclaringType = typeof(Log);

        static public void Init()
        {
            _logger = new NLog(log4net.LogManager.GetLogger("root").Logger);
        }

        static public NLog GetLogger()
        {
            StackFrame frame = new StackFrame(1, false);
            var method = frame.GetMethod();
            return new NLog(log4net.LogManager.GetLogger(method.DeclaringType).Logger);
        }

        /// <summary>
        /// 動的生成Loggerの管理
        /// </summary>
        static private Dictionary<string, NLog> _generated_logger = new Dictionary<string, NLog>();

        /// <summary>
        /// 名前付きのLoggerを動的に生成する
        /// </summary>
        /// <remarks>
        /// 呼び出したクラス名で検索したLoggerをひな型として、最初に見つかったFileAppenderのコピーを作成して紐づける、
        /// FileAppenderが見つからなければ例外が発生する。
        /// コピーしたFileAppenderのFileプロパティには、引数のnameを付加する。
        /// </remarks>
        /// <param name="name">ログファイル名に付加する名前</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static public NLog GetLogger(string name)
        {
            lock (_generated_logger)
            {
                if (_generated_logger.ContainsKey(name))
                {
                    return _generated_logger[name];
                }
            }

            // ベースとなるLoggerとFileAppenderを取得する
            StackFrame frame = new StackFrame(1, false);
            var method = frame.GetMethod();
            Logger baseLogger = (Logger)log4net.LogManager.GetLogger(method.DeclaringType).Logger;
            FileAppender baseAppender = GetFileAppender(baseLogger);
            if(baseAppender == null)
            {
                throw new Exception($"FileAppenderがありません");
            }

            // baseAppenderをコピーしてnewAppenderを生成
            FileAppender newAppender = CopyAndModifyFileAppender(baseAppender, name);

            // baseLoggerをコピーしてnewAppenderを紐付け
            Logger newLogger = CopyAndModifyLogger(baseLogger, name, newAppender);
            NLog Nlogger = new NLog(newLogger);
            lock (_generated_logger)
            {
                _generated_logger.Add(name, Nlogger);
            }
            return Nlogger;
        }

        static public void RemoveLogger(string name)
        {
            lock (_generated_logger)
            {
                if (_generated_logger.ContainsKey(name) == false)
                {
                    return;
                }

                // Logger取得
                Logger logger = (Logger)_generated_logger[name].Logger;

                // Appender削除
                // ToArray()をしないとループ中にAppendersが変更されて漏れが生じる恐れがある
                foreach (var appender in logger.Appenders.ToArray())
                {
                    // Appenderをロガーから取り外す
                    logger.RemoveAppender(appender);

                    // ファイルを閉じてリソースを解放する（Closeを実行）
                    if (appender is IAppender iAppender)
                    {
                        iAppender.Close();
                    }
                }

                // 注意: log4netの仕様上、Loggerインスタンス自体を完全に消去するメソッドはありませんが、
                // Appenderを空にして無効化（LevelをOFFなど）することでメモリや挙動への影響をなくせます。
                logger.Level = log4net.Core.Level.Off;

                // Logger削除
                _generated_logger.Remove(name);
            }
        }

        static public void RemoveAllLogger()
        {
            string[] keys;
            lock (_generated_logger)
            {
                keys = _generated_logger.Keys.ToArray();
            }
            foreach (var key in keys)
            {
                RemoveLogger(key);
            }
        }

        static private FileAppender GetFileAppender(Logger baseLogger)
        {
            Logger logger = baseLogger;
            while (logger != null)
            {
                foreach (var appender in logger.Appenders)
                {
                    // FileAppender または RollingFileAppender であればキャストして返す
                    if (appender is FileAppender fileAppender)
                    {
                        return fileAppender;
                    }
                }
                logger = logger.Parent;
            }
            return null;
        }

        static private FileAppender CopyAndModifyFileAppender(FileAppender baseAppender, string name)
        {
            Type appenderType = baseAppender.GetType();
            var newAppender = (FileAppender)Activator.CreateInstance(appenderType);
            PropertyInfo[] properties = appenderType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                // 書き込み可能項目を全てコピー(ただし、以下のプロパティは除く)
                // ・Name :後段で書き換えるため
                // ・Writer(内部ストリーム):コピーすると File が null になるため
                // ・SecurityContext(セキュリティ文脈):コピーするとエラーや権限競合が起きるため
                // ・LockingModel(ファイルのロックオブジェクト):参照で共有すると状態破壊が発生するため
                if (prop.CanWrite && prop.Name != "Name" && prop.Name != "Writer" && prop.Name != "SecurityContext" && prop.Name != "LockingModel")
                {
                    object value = prop.GetValue(baseAppender);
                    prop.SetValue(newAppender, value);
                }
            }

            // プロパティ書き換え
            newAppender.Name = baseAppender.Name + "." + name;
            newAppender.File = GetModifiedFileProperty(newAppender, name);
            if (baseAppender.LockingModel != null)
            {
                // 同じタイプの別インスタンスを生成する
                var lockingType = baseAppender.LockingModel.GetType();
                newAppender.LockingModel = (FileAppender.LockingModelBase)Activator.CreateInstance(lockingType);
            }
            newAppender.ActivateOptions();      // Appenderの設定を確定（必須）

            return newAppender;
        }

        /// <summary>
        /// Fileプロパティ書き換え返却
        /// </summary>
        /// <remarks>
        /// FileAppenderのFileプロパティは、xmlでの指定した値ではなく実際のログファイルパスになっている。
        /// ActivateOptions()を実行することで、xmlでの指定した<param name="File">の値がDatePatternなどの条件により変更されている。
        /// 従って、DatePatternなどの条件により変更され部分を元に戻した上で引数のnameを追加して返却する。
        /// </remarks>
        /// <param name="newAppender"></param>
        /// <param name="name"></param>
        /// <returns>書き換えたFileプロパティの値</returns>
        static private string GetModifiedFileProperty(FileAppender newAppender, string name)
        {
            string directory = Path.GetDirectoryName(newAppender.File);
            string filename = Path.GetFileName(newAppender.File);

            if (newAppender is RollingFileAppender rfa)
            {
                // 日付部分を削除する
                string todayPatternStr = DateTime.Now.ToString(rfa.DatePattern);
                filename = filename.Replace(todayPatternStr, "");
            }

            // nameを付加する
            filename = name + filename;

            return Path.Combine(directory, filename);
        }

        static private Logger CopyAndModifyLogger(Logger baseLogger, string name, FileAppender newAppender)
        {
            string loggerName = $"{name}{baseLogger.Name}";

            var hierarchy = (Hierarchy)LogManager.GetRepository();
            var newLogger = (Logger)hierarchy.GetLogger(loggerName);
            Type loggerType = typeof(Logger);
            PropertyInfo[] loggerProperties = loggerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in loggerProperties)
            {
                // 書き込み可能であり、かつ名前（Name）やアペンダー配列（Appenders）以外の共通設定をコピー
                if (prop.CanWrite && prop.Name != "Name" && prop.Name != "Appenders")
                {
                    object value = prop.GetValue(baseLogger);
                    prop.SetValue(newLogger, value);
                }
            }
            newLogger.RemoveAllAppenders();
            newLogger.AddAppender(newAppender);
            newLogger.Additivity = false;       // 親への重複出力を防止

            return newLogger;
        }


        static public void Info(string message)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Info, message, null);
        }

        static public void Debug(string message)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Debug, message, null);
        }
        static public void Warn(string message)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Warn, message, null);
        }
        static public void Warn(string message, Exception exception)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Warn, message, exception);
        }
        static public void Error(string message)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Error, message, null);
        }
        static public void Error(string message, Exception exception)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Error, message, exception);
        }
        static public void Fatal(string message)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Fatal, message, null);
        }
        static public void Fatal(string message, Exception exception)
        {
            _logger.Logger.Log(_thisDeclaringType, Level.Fatal, message, exception);
        }
        static public void Trace([CallerMemberName] string callerMethodName = null, [CallerLineNumber] int line = -1)
        {
            if (_logger.Logger.IsEnabledFor(Level.Trace))
            {
                if (callerMethodName != null)
                {
                    _logger.Logger.Log(_thisDeclaringType, Level.Trace, $"----- Call '{callerMethodName}' at line {line}", null);
                }
            }
        }
    }

    public class NLog : LogImpl
    {
        private static readonly Type _thisDeclaringType = typeof(NLog);

        public NLog(ILogger logger) : base(logger)
        {
        }

        public void Trace([CallerMemberName] string callerMethodName = null, [CallerLineNumber] int line = -1)
        {
            if (Logger.IsEnabledFor(Level.Trace))
            {
                if (callerMethodName != null)
                {
                    Logger.Log(_thisDeclaringType, Level.Trace, $"----- Call '{callerMethodName}' at line {line}", null);
                }
            }
        }
    }
}
