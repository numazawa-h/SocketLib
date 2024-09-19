﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;
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
            public string FilePath => _fpath;

            public RootNode(JsonNode jsonNode, string path) : base(null, jsonNode, "$")
            {
                _fpath = path;
            }
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

            public string PropertyName
            {
                get
                {
                    return GetPropertyName();
                }
            }

            public Node(Node parent, JsonNode jsonNode, string name)
            {
                _parent = parent;
                _jsonNode = jsonNode;
                _name = name;
            }

            public Node this[string name]
            {
                get
                {
                    JsonNode node = _jsonNode.AsObject().ContainsKey(name) ? _jsonNode[name] : null;

                    return new Node(this, node, name);
                }
            }

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
            public IEnumerator GetEnumerator()
            {
                try
                {
                    return new Enumerator(this);
                }
                catch (System.Exception ex)
                {
                    throw new InvalidOperationException($"Enumerateに失敗しました({this.PropertyName}) in {this.GetFilePath()}", ex);
                }
            }
        
    
            public string GetPropertyName()
            {
                List<string> names = new List<string>();
                Node node = this;
                while (node._parent != null)
                {
                    names.Add(node._name);
                    node = node._parent;
                }

                StringBuilder sb = new StringBuilder();
                for( int i=names.Count-1; i>=0; --i)
                {
                    sb.Append($"[{names[i]}]");
                }

                return sb.ToString();
            }
            public string GetFilePath()
            {
                Node node = this;
                while (node._parent != null)
                {
                    node = node._parent;
                }

                return ((RootNode)node).FilePath;
            }

            public static implicit operator int?(Node node)
            {
                int? val = null;
                try
                {
                    if (node._jsonNode != null)
                    {
                        if (node._jsonNode.GetValueKind() != JsonValueKind.Number)
                        {
                            throw new Exception("整数項目ではありません");
                        }
                        val = node._jsonNode.GetValue<int>();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"値の取得に失敗しました({node.PropertyName}) in {node.GetFilePath()}", ex);
                }
                return val;
            }
            public static implicit operator int(Node node)
            {
                return (int?)node is int v ? v : throw new InvalidOperationException($"項目が存在しません({node.PropertyName}) in {node.GetFilePath()}");
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
                    throw new InvalidOperationException($"値の取得に失敗しました({node.PropertyName}) in {node.GetFilePath()}", ex);
                }
                return val;
            }
        }
    }
}
