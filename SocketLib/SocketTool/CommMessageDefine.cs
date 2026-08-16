using NCommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Linq;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class CommMessageDefine
    {
        public static Dictionary<string, MessageDefine> ReadJson(string path, RuntimeWorkingArea runtime)
        {
            RootNode root = JsonConfig.ReadJson(path);

            // メッセージ定義読み込み
            Dictionary<string, MessageDefine> message_def = new Dictionary<string, MessageDefine>();
            foreach (Node def in root["message-def"])
            {
                try
                {
                    message_def.Add(((string)def["id"]).ToLower(), new MessageDefine(def, runtime));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"CommMessageDefineで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }
            return message_def;
        }
    }
}
