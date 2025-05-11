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
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
    public class CommMessageDefine
    {
        // シングルトン
        static private CommMessageDefine _instance = null;
        static public CommMessageDefine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CommMessageDefine();
            }
            return _instance;
        }
        private CommMessageDefine() : base()
        {
        }

        // 通信メッセージ定義
        protected Dictionary<string, MessageDefine> _message_def = new Dictionary<string, MessageDefine>();

        // データの値の説明定義
        protected Dictionary<string, ValuesDefine> _values_def = new Dictionary<string, ValuesDefine>();

        public void ReadJson(string path)
        {
            RootNode root = JsonConfig.ReadJson(path);

            // 先に値定義を読む（フィールド定義に"name"がなければ値定義の"name"をフィールド名とするため）
            _values_def.Clear();
            foreach (Node def in root["values-def"])
            {
                try
                {
                    string id = def["id"].Required();
                    _values_def.Add(id, new ValuesDefine(def));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"CommMessageDefineで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }

            // メッセージ定義読み込み
            _message_def.Clear();
            foreach (Node def in root["message-def"])
            {
                try
                {
                    _message_def.Add(((string)def["id"]).ToLower(), new MessageDefine(def));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"CommMessageDefineで読み込みエラー({def.PropertyNames}) in {path}", ex);
                }
            }
        }

        public bool Contains(string dtype)
        {
            dtype = dtype.ToLower();
            return _message_def.ContainsKey(dtype);
        }

        public MessageDefine GetMessageDefine(string dtype)
        {
            dtype = dtype.ToLower();
            if (_message_def.ContainsKey(dtype) == false)
            {
                throw new Exception($"定義されていないデータ種別({dtype})");
            }
            return new MessageDefine(_message_def[dtype]);
        }

        public ValuesDefine GetValuesDefine(string id)
        {
            if ( _values_def.ContainsKey(id) == false)
            {
                return null;
            }
            return _values_def[id];
        }

        /// <summary>
        /// 通信電文定義
        /// </summary>
        public class MessageDefine
        {
            // 電文種別
            public string DType { get; private set; }
            // 電文名
            public string DName { get; private set; }
            // データ長（可変長の時、-1）
            public int DLength { get; private set; }
            // データ長（可変長の時の固定部の長さ）
            public int MinLength { get; private set; }
            // ブロック定義
            public BlockDefine BlockDefine { get; private set; }
            // フィールド定義
            private Dictionary<string, FieldDefine> _fields_def = new Dictionary<string, FieldDefine>();

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="def">Json定義</param>
            public MessageDefine(Node def)
            {
                DType = def["id"].Required();
                DName = def["name"].Required();
                DLength = def["len"].Required();
                MinLength = (int?)def["minlen"] is int v ? v : 0;
                BlockDefine = new BlockDefine();

                foreach (Node node in def["flds"])
                {
                    if (node.ContainsKey("block"))
                    {
                        readBlock(BlockDefine, node);
                    }
                    else
                    {
                        FieldDefine fld = new FieldDefine(node);
                        _fields_def.Add(node["id"], fld);
                        BlockDefine.AddField(fld);
                    }
                }
            }

            private void readBlock(BlockDefine block, Node def, int offset=0)
            {
                int blkofs = def["ofs"].Required();
                int len = (int?)def["len"] is int v1 ? v1 : -1;
                int rep = (int?)def["rep"] is int v2 ? v2 : 1;

                if (rep > 1)
                {
                    if (len < 0)
                    {
                        throw new Exception($"'rep'指定には'len'指定が必要です");
                    }
                    for(int i=0; i<rep; i++, offset += len)
                    {
                        BlockDefine blk = block.AddBlock(def, i);
                        int ofs = blkofs + offset;
                        foreach (Node node in def["flds"])
                        {
                            if (node.ContainsKey("block"))
                            {
                                readBlock(blk, node, ofs);
                            }
                            else
                            {
                                FieldDefine fld = new FieldDefine(node, ofs, blk);
                                _fields_def.Add(fld.FldId, fld);
                                blk.AddField(fld);
                            }
                        }
                    }
                }
                else
                {
                    BlockDefine blk = block.AddBlock(def);
                    int ofs = blkofs + offset;
                    foreach (Node node in def["flds"])
                    {
                        if (node.ContainsKey("block"))
                        {
                            readBlock(blk, node, ofs);
                        }
                        else
                        {
                            FieldDefine fld = new FieldDefine(node, ofs, blk);
                            _fields_def.Add(fld.FldId, fld);
                            blk.AddField(fld);
                        }
                    }
                }
            }

            // コピーコンストラクタ
            public MessageDefine(MessageDefine other)
            {
                DType = other.DType;
                DName = other.DName;
                DLength = other.DLength;
                MinLength = other.MinLength;
                BlockDefine = other.BlockDefine;
                foreach (KeyValuePair<string, FieldDefine> pair in other._fields_def)
                {
                    _fields_def.Add(pair.Key, pair.Value);
                }
            }

            public FieldDefine[] GetFldList()
            {
                return _fields_def.Values.ToArray();
            }
            public string[] GetFldidList()
            {
                return _fields_def.Keys.ToArray();
            }

            public FieldDefine GetFldDefine(string fldid)
            {
                if (_fields_def.ContainsKey(fldid) == false)
                {
                    throw new Exception($"フィールドの定義がありません({fldid})");
                }
                return _fields_def[fldid];
            }

            public bool ContainsKey(string fldid)
            {
                return _fields_def.ContainsKey(fldid);
            }
        }

        public class BlockDefine
        {
            public string GrpId { get; private set; }
            public string OwnerGrpId => _owner.GrpId;
            public string BlkId { get; private set; }
            public string Name { get; private set; }
            public int NestingLevel { get; private set; }

            private BlockDefine _owner = null;
            private List<FieldDefine> _fields = new List<FieldDefine>();
            private List<BlockDefine> _blocks = new List<BlockDefine>();
            public BlockDefine()
            {
                GrpId = "top";
                BlkId = string.Empty;
                Name = string.Empty;
                NestingLevel = 0;
            }

            public BlockDefine(BlockDefine owner, Node def, int idx)
            {
                _owner = owner;
                NestingLevel = owner.NestingLevel + 1;

                GrpId = def["block"].Required();
                if(_owner.BlkId == string.Empty)
                {
                    BlkId = GrpId;
                }
                else
                {
                    BlkId = $"{_owner.BlkId}.{GrpId}";
                    GrpId = $"{_owner.GrpId}.{GrpId}";
                }
                if (idx >= 0)
                {
                    BlkId = $"{BlkId}[{idx}]";
                }
                if (def.ContainsKey("name"))
                {
                    Name = def["name"];
                    // nameの#nをblockのインデックスで置き換える
                    List<int> indexes = new List<int>();
                    foreach (Match match in new Regex("\\[([0-9]+)\\]").Matches(BlkId))
                    {
                        int n = int.Parse(match.Groups[1].Value);
                        indexes.Add(n);
                    }
                    for (int i = 0; i < indexes.Count; ++i)
                    {
                        Name = new Regex($"##{i}").Replace(Name, (indexes[i]+1).ToString());
                        Name = new Regex($"#{i}").Replace(Name, indexes[i].ToString());
                    }
                }
                else
                {
                    if (_owner.Name == string.Empty)
                    {
                        Name = GrpId;
                    }
                    else
                    {
                        Name = $"{_owner.Name}.{GrpId}";
                    }
                    if (idx >= 0)
                    {
                        Name = $"{Name}[{idx}]";
                    }
                }
            }

            public BlockDefine AddBlock(Node def, int idx = -1)
            {
                BlockDefine block = new BlockDefine(this, def, idx);
                _blocks.Add(block);
                return block;
            }

            public void AddField(FieldDefine fld)
            {
                _fields.Add(fld);
            }

            public string[] GetGroupIdList()
            {
                List<string> list = new List<string>();
                foreach (BlockDefine blk in _blocks)
                {
                    if (list.Contains(blk.GrpId) == false)
                    {
                        list.Add(blk.GrpId);
                    }
                }
                return list.ToArray();
            }
            public BlockDefine[] GetBlocks(string id)
            {
                List<BlockDefine> list = new List<BlockDefine>();
                foreach (BlockDefine blk in _blocks)
                {
                    if (blk.GrpId == id)
                    {
                        list.Add(blk);
                    }
                }
                return list.ToArray();
            }

            public BlockDefine[] GetBlocks()
            {
                return _blocks.ToArray();
            }
            public FieldDefine[] GetFields()
            {
                return _fields.ToArray();
            }
        }

        public class FieldDefine
        {
            public BlockDefine OwnerBlock { get; private set; }

            public string FldId { get; private set; }
            public string Name { get; private set; }
            public int Length { get; private set; }
            public int Offset { get; private set; }
            public bool isDispDesc { get; private set; }
            public bool isDispName { get; private set; }

            private ValuesDefine _valuesDefine = null;
            List<(string, string)> _valuesDefList = new List<(string, string)>();


            public FieldDefine(Node def, int ofs, BlockDefine blk) :this(def)
            {
                OwnerBlock = blk;
                Offset += ofs;
                FldId = blk.BlkId + "." + FldId;
                if (Name != null)
                {
                    // nameの#nをblockのインデックスで置き換える
                    List<int> indexes = new List<int>();
                    foreach (Match match in new Regex("\\[([0-9]+)\\]").Matches(blk.BlkId))
                    {
                        int n = int.Parse(match.Groups[1].Value);
                        indexes.Add(n);
                    }
                    for (int i = 0; i < indexes.Count; ++i)
                    {
                        Name = new Regex($"##{i}").Replace(Name, (indexes[i] + 1).ToString());
                        Name = new Regex($"#{i}").Replace(Name, indexes[i].ToString());
                    }
                }
            }

            public FieldDefine(Node def)
            {
                OwnerBlock = null;
                FldId = def["id"].Required();
                Offset = def["ofs"].Required();
                Length = def["len"].Required();
                Name = def["name"];
                isDispDesc = (bool?)def["disp"] is bool v1 ? v1 : false;
                isDispName = (bool?)def["dispname"] is bool v2 ? v2 : false;

                string valid = FldId;
                if (valid.Contains("_"))
                {
                    valid = valid.Substring(0, valid.IndexOf("_"));
                }
                if (valid.Contains("."))
                {
                    valid = valid.Substring(valid.LastIndexOf(".") + 1);
                }
                _valuesDefine = CommMessageDefine.GetInstance().GetValuesDefine(valid);
                if (_valuesDefine != null)
                {
                    _valuesDefList = _valuesDefine.ValuesDefList;
                }
            }

            /// <summary>
            /// データ長変更
            /// </summary>
            /// <remarks>可変長データの時、後から設定する</remarks>
            /// <param name="len"></param>
            public void SetFldLength(int len)
            {
                Length = len;
            }

            /// <summary>
            /// 項目値の説明を返却する
            /// </summary>
            /// <remarks>
            /// 項目値の説明が定義されていれば説明を返却する。
            /// 定義されていなければ"？？？"を返却する。
            /// 非表示指定の項目値ならstring.Emptyを返却する。
            /// 項目名表示指定のフィールドなら、"[項目名]:[項目値の説明]"の形式で返却する。
            /// </remarks>
            /// <param name="val">項目値</param>
            /// <returns>項目値の説明</returns>
            public string GetValueDescription(byte[] val)
            {
                // todo: CommMessageDefineのGetValueDescriptionをこちらの呼び出しに変更する
                if(_valuesDefine == null)
                {
                    // 項目値定義がなければ表示しない
                    return string.Empty;
                }
                if (isDispDesc == false && isDispName == false)
                {
                    // 表示項目でなければ表示しない
                    return string.Empty;
                }

                string desc = _valuesDefine[val];
                if (desc == string.Empty)
                {
                    // 非表示指定の値なら表示しない
                    return string.Empty;
                }
                if (isDispDesc == false)
                {
                    // 項目値非表示指定のフィールドなら項目値の説明なし（項目名のみを表示する）
                    desc = string.Empty;
                }

                string name = string.Empty;
                if (isDispName == true)
                {
                    if (Name != null)
                    {
                        name = $"{Name}";
                    }
                    else
                    {
                        name = $"{FldId}";
                    }
                }
                if (name != string.Empty && desc != string.Empty)
                {
                    desc = $"{name}:{desc}";
                }
                else
                {
                    desc += name;   // どちらかはstring.Emptyなのでもう片方のみが設定される
                }
                return desc;
            }

            /// <summary>
            /// 項目値の説明一覧を取得する
            /// </summary>
            /// <returns>項目値の説明リスト(BCD文字列の項目値, 項目値の説明)</returns>
            public List<(string, string)> GetFldDescription()
            {
                return _valuesDefList;
            }
        }

        public class ValuesDefine
        {
            public string FldId { get; private set; }

            public string FldName { get; private set; }

            public Format FormatDef { get; private set; }

            Dictionary<string, JsonValue> _values_def = null;
            List<(string, string)> _valuesLDefist = new List<(string, string)>();
            List<string> _notdisp = new List<string>();

            public ValuesDefine(Node def)
            {
                FldId = def["id"].Required();
                FldName = def["name"];

                _values_def = def["values"].GetValues();
                if (_values_def.ContainsKey("notdisp"))
                {
                    if (_values_def["notdisp"].GetValueKind() == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach(var val in _values_def["notdisp"].AsArray())
                        {
                            _notdisp.Add((string)val);
                        }
                    }
                    else
                    {
                        _notdisp.Add((string)_values_def["notdisp"]);
                    }
                    _values_def.Remove("notdisp");
                }
                foreach (var pair in _values_def)
                {
                    string vals = (string)pair.Key;
                    string desc = (string)pair.Value;
                    _valuesLDefist.Add((vals, desc));
                }
                if (def.ContainsKey("format"))
                {
                    string type = def["format"]["type"].Required();
                    switch (type)
                    {
                        case "int":
                            FormatDef = new FormatInt(def["format"]);
                            break;
                        case "datetime":
                            FormatDef = new FormatDateTime(def["format"]);
                            break;
                        default:
                            throw new Exception($"formatの指定に定義されていない type('{type}')が使われています");
                    }
                }
            }

            /// <summary>
            /// 値に対応する説明を返却
            /// </summary>
            /// <param name="val">値（16進文字列）</param>
            /// <returns>値の説明</returns>
            public string this[byte[] val]
            {
                // todo:キーを文字列にする
                get
                {
                    string bcd = new ByteArray(val).to_hex();
                    if (_notdisp.Contains(bcd))
                    {
                        // 非表示指定の値なら表示しない
                        return string.Empty;
                    }
                    if (_values_def.ContainsKey(bcd))
                    {
                        // 値の一覧に存在すれば対応する値を返却する
                        return _values_def[bcd].ToString();
                    }
                    if (FormatDef != null)
                    {
                        return FormatDef.GetDescription(val);
                    }
                    return "？？？";
                }
            }
            public string[] Values
            {
                get { return _values_def.Keys.ToArray<string>(); }
            }
            public List<(string, string)> ValuesDefList
            {
                get { return _valuesLDefist; }
            }
        }

        public abstract class Format
        {
            protected string _format_def;
            protected string _value_format_def;
            public Format(Node def)
            {
                _format_def = def["fmt"];
                _value_format_def = def["valfmt"];
            }
            public virtual string GetValueFormat()
            {
                return _value_format_def;
            }


            public abstract string GetDescription(byte[] value);
        }

        public class FormatInt : Format
        {
            int _minvalue;
            int _maxvalue;

            public FormatInt(Node def): base(def)
            {
                _minvalue = (int?)def["minvalue"] is int v1 ? v1 : int.MinValue;
                _maxvalue = (int?)def["maxvalue"] is int v2 ? v2 : int.MaxValue;
            }

            public override string GetDescription(byte[] dat)
            {
                string desc = string.Empty;
                int value = new ByteArray(dat).to_int();
                if (value >= _minvalue && value <= _maxvalue)
                {
                    desc = string.Format(_format_def, value);
                }
                return desc;
            }
        }
        public class FormatDateTime : Format
        {
            public FormatDateTime(Node def) : base(def)
            {
                if(_value_format_def != null)
                {
                    switch (_value_format_def)
                    {
                        case "yyyyMMddHHmmss":
                        case "yyyyMMddHHmm":
                        case "yyyyMMddHH":
                        case "yyyyMMdd":
                        case "yyMMddHHmmss":
                        case "yyMMddHHmm":
                        case "yyMMddHH":
                        case "yyMMdd":
                            break;
                        default:
                            throw new Exception($"formatの指定に不正な valfmt('{_value_format_def}')が使われています");
                    }
                }
            }

            public override string GetDescription(byte[] dat)
            {
                string desc = string.Empty;
                DateTime dt = new ByteArray(dat).to_dateTime();
                desc = dt.ToString(_format_def);
                return desc;
            }
        }
    }
}
