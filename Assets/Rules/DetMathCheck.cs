// PU-22: the web game's rules use their own deterministic math (src/game/detMath.ts) instead of Math.sin, cos,
// atan2, hypot, pow, exp, log..., whose last bits differ between V8, Jint/.NET and each C library. This check
// plays the web game's fixture (inputs and the outputs Node computed, as IEEE-754 bits) through the packed rules
// in Jint and counts every output that isn't bit for bit the same. No UnityEngine here (noEngineReferences).
//
// The full fixture (about 100,000 lines) is built in the web game (node tools/replay/detmath-fixture.ts --unity
// <this project>) into Rules~/detmath-fixture.txt, ignored by Git like the rules file. A spread-out sample and a
// sha256 per function are committed here (tools/SimCli/detmath-expected.json), so the check also proves the
// fixture is the one the web game expects.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Hatchfield.Sim.Replay;
using Jint;
using Jint.Native;

namespace Hatchfield.Rules
{
    public sealed class DetMathCheck
    {
        public sealed class FnResult
        {
            public string Name;
            public int Count;
            public int Mismatches;
            public string FirstMismatch;
            public string Sha256;
        }

        public readonly Dictionary<string, FnResult> Functions = new Dictionary<string, FnResult>();
        public int Total;
        public int Mismatches;

        readonly RulesHost host;
        readonly Dictionary<string, JsValue> exports = new Dictionary<string, JsValue>();

        public DetMathCheck(RulesHost host) { this.host = host; }

        /// <summary>The export name of each fixture function (sin → detSin, ...).</summary>
        static string ExportOf(string fn) => "det" + char.ToUpperInvariant(fn[0]) + fn.Substring(1);

        public static double FromHex(string h) => BitConverter.Int64BitsToDouble(unchecked((long)ulong.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));

        /// <summary>Same as the web game's hex(): one NaN pattern, since JavaScript can't tell NaN payloads apart.</summary>
        public static string Hex(double x) => double.IsNaN(x) ? "7ff8000000000000" : unchecked((ulong)BitConverter.DoubleToInt64Bits(x)).ToString("x16");

        /// <summary>Checks fixture lines ("fn hexIn... hexOut"); '#' lines are comments.</summary>
        public void Run(IEnumerable<string> lines)
        {
            var hashes = new Dictionary<string, IncrementalHash>();
            foreach (var raw in lines)
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                var p = raw.Split(' ');
                var fn = p[0];
                if (!Functions.TryGetValue(fn, out var r)) { Functions[fn] = r = new FnResult { Name = fn }; hashes[fn] = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); }
                hashes[fn].AppendData(Encoding.ASCII.GetBytes(raw + "\n"));
                if (!exports.TryGetValue(fn, out var f))
                {
                    f = host.Export(ExportOf(fn));
                    if (f.IsUndefined()) throw new InvalidOperationException($"the rules file has no {ExportOf(fn)} export: re-pack it (npm run pack:rules) from a game commit with detMath");
                    exports[fn] = f;
                }
                var args = new JsValue[p.Length - 2];
                for (int i = 0; i < args.Length; i++) args[i] = new JsNumber(FromHex(p[i + 1]));
                var got = Hex(f.Call(JsValue.Undefined, args).AsNumber());
                var want = p[p.Length - 1];
                r.Count++;
                Total++;
                if (got != want)
                {
                    r.Mismatches++;
                    Mismatches++;
                    if (r.FirstMismatch == null) r.FirstMismatch = $"{raw} → Jint gave {got}";
                }
            }
            foreach (var kv in hashes) Functions[kv.Key].Sha256 = BitConverter.ToString(kv.Value.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>The committed expectations: format, a count and sha256 per function, and the sample lines.</summary>
        public sealed class Expected
        {
            public string Format;
            public Dictionary<string, (int count, string sha256)> Functions = new Dictionary<string, (int, string)>();
            public List<string> Sample = new List<string>();

            public static Expected Load(string file)
            {
                var o = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(file));
                var e = new Expected { Format = (string)o["format"] };
                foreach (var kv in (Dictionary<string, object>)o["functions"])
                {
                    var f = (Dictionary<string, object>)kv.Value;
                    e.Functions[kv.Key] = ((int)(double)f["count"], (string)f["sha256"]);
                }
                foreach (var s in (List<object>)o["sample"]) e.Sample.Add((string)s);
                return e;
            }
        }

        /// <summary>After Run on a full fixture: functions whose count or sha256 differ from the expectations.</summary>
        public List<string> FixtureDifferences(Expected want)
        {
            var bad = new List<string>();
            foreach (var kv in want.Functions)
            {
                if (!Functions.TryGetValue(kv.Key, out var r)) { bad.Add($"{kv.Key}: not in the fixture"); continue; }
                if (r.Count != kv.Value.count || r.Sha256 != kv.Value.sha256) bad.Add($"{kv.Key}: {r.Count} lines sha256 {r.Sha256.Substring(0, 12)}, expected {kv.Value.count} lines sha256 {kv.Value.sha256.Substring(0, 12)}");
            }
            foreach (var k in Functions.Keys.Where(k => !want.Functions.ContainsKey(k))) bad.Add($"{k}: not expected");
            return bad;
        }

        public string Report()
        {
            var sb = new StringBuilder();
            foreach (var r in Functions.Values) sb.AppendLine($"  {r.Name,-6} {r.Count,7} inputs  {(r.Mismatches == 0 ? "bit-identical" : r.Mismatches + " DIFFERENT, first: " + r.FirstMismatch)}");
            sb.Append($"{Total - Mismatches}/{Total} outputs bit-identical to Node");
            return sb.ToString();
        }
    }
}
