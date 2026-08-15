using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;
using static SocketTool.CommMessageDefine;

namespace SocketTool
{
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

        // フィールド定義のdefault値で初期化したデータ(CommMessageのコンストラクタで使用する)
        private ByteArray _default_data = null;

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

            InitData();
            SetDefaultValue();
        }

        private void readBlock(BlockDefine block, Node def, int offset = 0)
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
                for (int i = 0; i < rep; i++, offset += len)
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
        private void InitData()
        {
            if (DLength == 0)
            {
                // データ部なしメッセージ
                _default_data = new ByteArray();
            }
            else if (DLength > 0)
            {
                // 固定長メッセージ
                _default_data = new ByteArray().Expand(DLength);
            }
            else
            {
                // 可変長メッセージ
                if (MinLength > 0)
                {
                    // 固定部分ありならその部分のみ生成
                    _default_data = new ByteArray().Expand(MinLength);
                }
                else
                {
                    // 固定部分がなければ空で生成
                    _default_data = new ByteArray();
                }
            }
        }

        private void SetDefaultValue()
        {
            foreach (var pair in _fields_def)
            {
                FieldDefine fld = pair.Value as FieldDefine;
                if (fld.Default != null)
                {
                    _default_data.Copy(fld.Default, fld.Offset, fld.Length);
                }
            }
        }
        public byte[] GetDefaultValue()
        {
            return _default_data.GetData();
        }

        // コピーコンストラクタ
        public MessageDefine(MessageDefine other)
        {
            DType = other.DType;
            DName = other.DName;
            DLength = other.DLength;
            MinLength = other.MinLength;
            BlockDefine = other.BlockDefine;
            _default_data = other._default_data;
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

    /// <summary>
    // ブロック定義
    /// </summary>
    ///<remarks>
    /// いくつかのフィールドをまとめてブロックとして定義する。
    ///</remarks>
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
            if (_owner.BlkId == string.Empty)
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
                    Name = new Regex($"##{i}").Replace(Name, (indexes[i] + 1).ToString());
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

    /// <summary>
    /// フィールド定義
    /// </summary>
    /// <remarks>
    /// メッセージを構成する一つのフィールドを定義する。
    /// </remarks>
    public class FieldDefine
    {
        public BlockDefine OwnerBlock { get; private set; }

        public string FldId { get; private set; }
        public string Name { get; private set; }
        public int Length { get; private set; }
        public int Offset { get; private set; }

        private bool _isDispDesc = false;
        private bool _isDispName = false;
        private ValuesDefine _valuesDefine = null;
        List<(string, string)> _valuesDefList = new List<(string, string)>();

        public byte[] Default { get; private set; } = null;

        public FieldDefine(Node def, int ofs, BlockDefine blk) : this(def)
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
            _isDispDesc = (bool?)def["disp"] is bool v1 ? v1 : false;
            _isDispName = (bool?)def["dispname"] is bool v2 ? v2 : false;
            string deflt = (string)def["default"];
            if (deflt != null)
            {
                Default = ByteArray.ParseHex(deflt);
            }

            // 項目値定義の取り込み
            string valid = FldId;
            if (valid.Contains("."))
            {
                // blockで階層化されていれば、最後のフィールド名のみを項目値IDとする
                valid = valid.Substring(valid.LastIndexOf(".") + 1);
            }
            if (valid.Contains("_"))
            {
                // フィールド名に"_"があれば、"_"より前の部分を項目値IDとする
                valid = valid.Substring(0, valid.IndexOf("_"));
            }
            _valuesDefine = CommMessageDefine.GetInstance().GetValuesDefine(valid);
            if (_valuesDefine != null)
            {
                _valuesDefList = _valuesDefine.ValuesDefList;
                if (Default == null)
                {
                    Default = _valuesDefine.Default;
                }
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
            if (_valuesDefine == null)
            {
                // 項目値定義がなければ表示しない
                return string.Empty;
            }
            if (_isDispDesc == false && _isDispName == false)
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
            if (_isDispDesc == false)
            {
                // 項目値非表示指定のフィールドなら項目値の説明なし（項目名のみを表示する）
                desc = string.Empty;
            }

            string name = string.Empty;
            if (_isDispName == true)
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
}
