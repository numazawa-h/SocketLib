using NCommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    /// <summary>
    /// 値定義
    /// </summary>
    /// <remarks>
    /// フィールドのデータ型と値に対応する説明を定義する。
    /// </remarks>
    public class ValuesDefine
    {
        public string FldId { get; private set; }

        public string FldName { get; private set; }

        public Format FormatDef { get; private set; }
        public byte[] Default { get; private set; } = null;

        Dictionary<string, JsonValue> _values_def = null;
        List<(string, string)> _valuesLDefist = new List<(string, string)>();
        List<string> _notdisp = new List<string>();

        public ValuesDefine(Node def)
        {
            FldId = def["id"].Required();
            FldName = def["name"];
            string deflt = (string)def["default"];
            if (deflt != null)
            {
                Default = ByteArray.ParseHex(deflt);
            }

            _values_def = def["values"].GetPropertyValues();
            if (_values_def.ContainsKey("notdisp"))
            {
                if (_values_def["notdisp"].GetValueKind() == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var val in _values_def["notdisp"].AsArray())
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

    /// <summary>
    /// フォーマット定義
    /// </summary>
    /// <remarks>
    /// フィールドのデータ型を定義する。
    /// </remarks>
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

    /// <summary>
    /// 整数型定義
    /// </summary>
    public class FormatInt : Format
    {
        int _minvalue;
        int _maxvalue;

        public FormatInt(Node def) : base(def)
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

    /// <summary>
    /// 日時型定義
    /// </summary>
    public class FormatDateTime : Format
    {
        public FormatDateTime(Node def) : base(def)
        {
            if (_value_format_def != null)
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
