using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dungine.Library
{
    /// <summary>A small JSON reader: objects become Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double.</summary>
    public static class MiniJson
    {
        public static object Parse(string json) { int i = 0; return Value(json, ref i); }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++;
                Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); var k = Str(s, ref i); Ws(s, ref i); i++; // ':'
                    d[k] = Value(s, ref i); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return d; // '}'
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++;
                Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return l; // ']'
                }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder(); i++; // opening quote
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    char e = s[i];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 'b') sb.Append('\b');
                    else if (e == 'f') sb.Append('\f');
                    else if (e == 'u') { sb.Append((char)int.Parse(s.Substring(i + 1, 4), NumberStyles.HexNumber)); i += 4; }
                    else sb.Append(e);
                }
                else sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }

        // ---- helpers for reading the parsed tree ----
        public static Dictionary<string, object> Obj(object o, string key) => o is Dictionary<string, object> d && d.TryGetValue(key, out var v) ? v as Dictionary<string, object> : null;
        public static List<object> Arr(object o, string key) => o is Dictionary<string, object> d && d.TryGetValue(key, out var v) ? v as List<object> : null;
        public static string Str(object o, string key) => o is Dictionary<string, object> d && d.TryGetValue(key, out var v) ? v as string : null;
        public static float Num(object o, string key, float def = 0) => o is Dictionary<string, object> d && d.TryGetValue(key, out var v) && v is double x ? (float)x : def;
        public static float[] Floats(object o, string key)
        {
            var a = Arr(o, key); if (a == null) return null;
            var r = new float[a.Count];
            for (int k = 0; k < a.Count; k++) r[k] = a[k] is double x ? (float)x : 0;
            return r;
        }
    }
}
