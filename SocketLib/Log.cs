using System;
using System.Runtime.CompilerServices;

namespace SocketLib
{
    internal class Log
    {        
        // シングルトン
        static private Log _instance = null;
        static public Log GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Log();
            }
            return _instance;
        }
        private Log()
        {
        }

        static private log4net.ILog _logger = null;
        static public void  Init()
        {
            _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }


        static public void Info(string message)
        {
            Log._logger.Info(message);
        }
        static public void Debug(string message)
        {
            Log._logger.Debug(message);
        }
        static public void Warn(string message, Exception ex = null)
        {
            if (ex == null)
            {
                Log._logger.Warn(message);
            }
            else
            {
                Log._logger.Warn(message, ex);
            }
            Log._logger.Warn("\r\n");
        }
        static public void Error(string message, Exception ex = null)
        {
            if (ex == null) {
                Log._logger.Error(message);
            }
            else
            {
                Log._logger.Error(message, ex);
            }
            Log._logger.Error("\r\n");
        }

        static public void Trace(string arg = null, [CallerMemberName] string callerMethodName = null, [CallerLineNumber] int line = -1)
        {
            if (callerMethodName !=null)
            {
                if(arg != null)
                {
                    Log._logger.Debug($"----- Call '{callerMethodName}'({arg}) at line {line}");
                }
                else
                {
                    Log._logger.Debug($"----- Call '{callerMethodName}' at line {line}");
                }
            }
        }
    }
}
