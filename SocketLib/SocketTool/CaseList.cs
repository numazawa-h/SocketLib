using NCommonUtility;
using SocketTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static NCommonUtility.JsonConfig;

namespace SocketTool
{
    internal class CaseList
    {
        Dictionary<string, JsonValue> _values = new Dictionary<string, JsonValue>();
        Dictionary<string, JsonValue[]> _array_values = new Dictionary<string, JsonValue[]>();

        public CaseList(Node node)
        {
            _values = node["case"].Required().GetPropertyValues();
            _array_values = node["case"].Required().GetPropertyArrayValue();
        }

        public CaseList Add(CaseList other)
        {
            foreach (var pair in other._values)
            {
                this._values.Add(pair.Key, pair.Value);
            }
            foreach (var pair in other._array_values)
            {
                this._array_values.Add(pair.Key, pair.Value);
            }
            return this;
        }

        public bool isTarget(CommMessage resmsg)
        {
            foreach (var pair in _values)
            {
                string key = pair.Key;
                JsonValue val = pair.Value;
                if (resmsg.ContainsFldKey(key) == false)
                {
                    return false;
                }
                ByteArray fldval = resmsg.GetFldValue(key);
                switch (val.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.Number:
                        int intval = val.GetValue<int>();
                        if (fldval.to_int() != intval)
                        {
                            return false;
                        }
                        break;
                    case System.Text.Json.JsonValueKind.String:
                        string hexval = val.GetValue<string>().ToUpper();
                        if (fldval.to_hex() != hexval)
                        {
                            return false;
                        }
                        break;
                }
            }
            foreach (var pair in _array_values)
            {
                string key = pair.Key;
                JsonValue[] vals = pair.Value;
                if (resmsg.ContainsFldKey(key) == false)
                {
                    return false;
                }
                ByteArray fldval = resmsg.GetFldValue(key);
                bool or = false;
                foreach (JsonValue val in vals)
                {
                    switch (val.GetValueKind())
                    {
                        case System.Text.Json.JsonValueKind.Number:
                            int intval = val.GetValue<int>();
                            if (fldval.to_int() == intval)
                            {
                                or = true;
                            }
                            break;
                        case System.Text.Json.JsonValueKind.String:
                            string hexval = val.GetValue<string>().ToUpper();
                            if (fldval.to_hex() == hexval)
                            {
                                or = true;
                            }
                            break;
                    }
                }
                if (or == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
