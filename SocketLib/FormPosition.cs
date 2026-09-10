using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SocketLib
{
    public class FormPosition
    {
        public class Position
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FormPosition.json");
        private static Dictionary<string, Position> _positions = new Dictionary<string, Position>();

        // JSONファイルから読み込み
        public static void LoadPosition()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    var options = new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip, // コメントを許可
                        AllowTrailingCommas = true                      // 末尾のカンマを許可
                    };
                    _positions = JsonSerializer.Deserialize<Dictionary<string, Position>>(json, options) ?? new Dictionary<string, Position>();
                }
                catch
                {
                    _positions = new Dictionary<string, Position>();
                }
            }
        }

        // JSONファイルに書き出し
        public static void SavePosition()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // 日本語（Unicode文字）をエスケープせずに生テキストで出力する設定
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            string json = JsonSerializer.Serialize(_positions, options);
            File.WriteAllText(ConfigPath, json);
        }

        // 特定のフォームの位置を取得する
        public static Position GetPosition(string networkname)
        {
            return _positions.TryGetValue(networkname, out var pos) ? pos : null;
        }

        // 特定のフォームの位置を保存する
        public static void PutPosition(string networkname, Rectangle rect)
        {
            _positions[networkname] = new Position
            {
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
            };
        }
    }
}
