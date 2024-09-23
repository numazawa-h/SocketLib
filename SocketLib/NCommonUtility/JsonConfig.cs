﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using static log4net.Appender.ColoredConsoleAppender;
using static NCommonUtility.JsonConfig;
using static SocketLib.Program;
using static System.Net.Mime.MediaTypeNames;

namespace NCommonUtility
{


    public class JsonConfig
    {
        static public RootNode ReadJson(string path)
        {
            // Config用オプションの設定
            JsonDocumentOptions _options = new JsonDocumentOptions
            {
                // コメントを許可
                CommentHandling = JsonCommentHandling.Skip,
                // 末尾のコンマを許可
                AllowTrailingCommas = true
            };

            // Json読み込み
            JsonNode json_root;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    json_root = JsonNode.Parse(fs, null, _options);
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Jsonファイルの読み込みに失敗しました({path})", ex);
            }

            return new RootNode(json_root, path);
        }

        /// <summary>
        /// RootNodeクラス
        /// </summary>
        /// <remarks>ファイルパスを保持するNodeクラス</remarks>
        public class RootNode : Node
        {
            string _fpath;

            public RootNode(JsonNode jsonNode, string path) : base(null, jsonNode, "$")
            {
                _fpath = path;
            }
            public string GetFilePath() { return _fpath; }
        }

        /// <summary>
        /// Nodeクラス
        /// </summary>
        /// <remarks>JsonNodeクラスをラッピングするクラス</remarks>
        public class Node : IEnumerable
        {
            Node _parent;
            string _name;
            JsonNode _jsonNode;
            bool _isRequired = false;

            /// <summary>
            /// Property名
            /// </summary>
            /// <remarks>RootNodeから自NodeまでのProperty名を連結</remarks>
            public string PropertyNames
            {
                get
                {
                    var names = new List<string>();
                    Node node = this;
                    while (node._parent != null)
                    {
                        names.Add(node._name);
                        node = node._parent;
                    }

                    var sb = new StringBuilder();
                    for (int i = names.Count - 1; i >= 0; --i)
                    {
                        sb.Append($"[{names[i]}]");
                    }

                    return sb.ToString();
                }
            }

            /// <summary>
            /// Jsonファイルパス
            /// </summary>
            public string FilePath
            {
                get
                {
                    // RootNode(親Nodeがnull)になるまでNodeを遡る
                    Node node = this;
                    while (node._parent != null)
                    {
                        node = node._parent;
                    }

                    return ((RootNode)node).GetFilePath();
                } 
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="parent">親Node</param>
            /// <param name="jsonNode">JsonNode</param>
            /// <param name="name">Property名</param>
            public Node(Node parent, JsonNode jsonNode, string name)
            {
                _parent = parent;
                _jsonNode = jsonNode;
                _name = name;
            }

            /// <summary>
            /// 子Nodeの読み込み
            /// </summary>
            /// <param name="name">Property名</param>
            /// <returns>Node</returns>
            public Node this[string name]
            {
                get 
                {
                    JsonNode node = _jsonNode.AsObject().ContainsKey(name) ? _jsonNode[name] : null;

                    return new Node(this, node, name);
                }
            }

            override public string ToString()
            {
                return _jsonNode.ToString();
            }

            #region IEnumerator
            class Enumerator : IEnumerator
            {
                Node _parent;
                IEnumerator _array;
                int _idx =-1;
                public Enumerator(Node node)
                {
                    if (node._jsonNode == null)
                    {
                        throw new Exception("項目がありません");
                    }
                    if (node._jsonNode.GetValueKind() != JsonValueKind.Array)
                    {
                        throw new Exception("配列ではありません");
                    }
                    _parent = node;
                    _array = node._jsonNode.AsArray().GetEnumerator();
                }
                public Object Current {
                    get
                    {  
                        JsonNode node = (JsonNode)_array.Current;
                        return new Node(_parent, node, _idx.ToString());
                    }
                }
                public bool MoveNext() 
                {
                    if (_array.MoveNext())
                    {
                        ++_idx;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                public void Reset() 
                { 
                    _idx = -1;
                    _array.Reset();
                }
            }
            #endregion

            public IEnumerator GetEnumerator()
            {
                try
                {
                    return new Enumerator(this);
                }
                catch (System.Exception ex)
                {
                    throw new InvalidOperationException($"Enumerateに失敗しました({this.PropertyNames}) in {this.FilePath}", ex);
                }
            }


            private string _Format = null;
            public Node SetFormat(string fmt)
            {
                _Format = fmt;
                return this;
            }

            /// <summary>
            /// 値をEnum型に変換する
            /// </summary>
            /// <typeparam name="T">Enum型のクラス</typeparam>
            /// <returns>指定されたEnum型の値</returns>
            /// <exception cref="InvalidOperationException"></exception>
            public T Enum<T>() where T : System.Enum
            {
                System.Enum val =null;
                try
                {
                    if (_jsonNode != null)
                    {
                        switch (_jsonNode.GetValueKind() )
                        {
                            case JsonValueKind.String:
                                val = (T)System.Enum.Parse(typeof(T), _jsonNode.ToString());
                                break;
                            case JsonValueKind.Number:
                                val = (T)System.Enum.ToObject(typeof(T), _jsonNode.GetValue<int>());
                                break;
                            default:
                                throw new Exception("Enumに変換できません");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({this.PropertyNames}) in {this.FilePath}", ex);
                }

                if (val is null)
                {
                    throw new InvalidOperationException($"項目が存在しません({this.PropertyNames}) in {this.FilePath}");
                }
                return (T)val;
            }

            /// <summary>
            /// JsonObjectから任意のクラスのオブジェクトを生成する
            /// </summary>
            /// <typeparam name="T">任意のクラス</typeparam>
            /// <returns>指定されたクラスのオブジェクト</returns>
            /// <exception cref="InvalidOperationException"></exception>
            public T Class<T>() where T : class
            {
                T val = null;
                try
                {
                    if (_jsonNode != null)
                    {
                        var options = new JsonSerializerOptions
                        {
                            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                            IncludeFields = true,
                        };

                        switch (_jsonNode.GetValueKind())
                        {
                            case JsonValueKind.Object:
                                val =JsonSerializer.Deserialize<T>(_jsonNode, options);
                                break;
                            default:
                                throw new Exception("オブジェクト項目ではありません");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({this.PropertyNames}) in {this.FilePath}", ex);
                }

                if (val is null && this._isRequired)
                {
                    throw new InvalidOperationException($"項目が存在しません({this.PropertyNames}) in {this.FilePath}");
                }
                return val;
            }


            public Node Required()
            {
                _isRequired = true;
                return this;
            }

            public static implicit operator int(Node node)
            {
                return node._isRequired ? (int)(int?)node : throw new InvalidOperationException($"null非許容へのキャストにはRequired()が必要です({node.PropertyNames}) in {node.FilePath}");
            }

            public static implicit operator int?(Node node)
            {
                int? val = null;
                try
                {
                    if (node._jsonNode != null)
                    {
                        string hex = null;
                        switch (node._jsonNode.GetValueKind())
                        {
                            case JsonValueKind.Number:
                                break;
                            case JsonValueKind.String:
                                string s = node.ToString();
                                if (s.Length > 2 && s.Substring(0, 2) == "0x")
                                {
                                    hex = s.Substring(2);
                                }
                                break;
                            default:
                                throw new Exception("整数項目ではありません");
                        }
                        try
                        {
                            if (hex is null)
                            {
                                val = node._jsonNode.GetValue<int>();
                            }
                            else
                            {
                                val = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                            }
                        }
                        catch (System.FormatException)
                        {
                            throw new Exception("整数に変換できません");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({node.PropertyNames}) in {node.FilePath}", ex);
                }
                if (val == null && node._isRequired)
                {
                    throw new InvalidOperationException($"項目が存在しません({node.PropertyNames}) in {node.FilePath}");
                }
                return val;
            }

            public static implicit operator string(Node node)
            {
                string val = null;
                try
                {
                    if (node._jsonNode != null)
                    {
                        if (node._jsonNode.GetValueKind() != JsonValueKind.String)
                        {
                            throw new Exception("文字列項目ではありません");
                        }
                        val = node._jsonNode.GetValue<string>();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({node.PropertyNames}) in {node.FilePath}", ex);
                }
                if (val == null && node._isRequired)
                {
                    throw new InvalidOperationException($"項目が存在しません({node.PropertyNames}) in {node.FilePath}");
                }
                return val;
            }

            public static implicit operator bool(Node node)
            {
                return node._isRequired ? (bool)(bool?)node : throw new InvalidOperationException($"null非許容へのキャストにはRequired()が必要です({node.PropertyNames}) in {node.FilePath}");
            }

            public static implicit operator bool?(Node node)
            {
                bool? val = null;
                try
                {
                    if (node._jsonNode != null)
                    {
                        switch(node._jsonNode.GetValueKind())
                        {
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                break;
                            default:
                                throw new Exception("真偽値項目ではありません");
                        }
                        val = node._jsonNode.GetValue<bool>();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({node.PropertyNames}) in {node.FilePath}", ex);
                }
                if (val == null && node._isRequired)
                {
                    throw new InvalidOperationException($"項目が存在しません({node.PropertyNames}) in {node.FilePath}");
                }
                return val;
            }

            public static implicit operator DateTime(Node node)
            {
                return node._isRequired ? (DateTime)(DateTime?)node : throw new InvalidOperationException($"null非許容へのキャストにはRequired()が必要です({node.PropertyNames}) in {node.FilePath}");
            }

            public static implicit operator DateTime?(Node node)
            {
                DateTime? val = null;
                string fmt = node._Format is string v? v:"yyyy/MM/dd HH:mm:ss";
                try
                {
                    if (node._jsonNode != null)
                    {
                        if (node._jsonNode.GetValueKind() != JsonValueKind.String)
                        {
                            throw new Exception("文字列項目ではありません");
                        }
                        try
                        {
                            val = DateTime.ParseExact(node._jsonNode.GetValue<string>(), fmt, null);
                        }
                        catch (System.FormatException)
                        {
                            throw new Exception($"フォーマットエラー({fmt})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({node.PropertyNames}) in {node.FilePath}", ex);
                }
                if (val == null && node._isRequired)
                {
                    throw new InvalidOperationException($"項目が存在しません({node.PropertyNames}) in {node.FilePath}");
                }
                return val;
            }
        }
    }
}
