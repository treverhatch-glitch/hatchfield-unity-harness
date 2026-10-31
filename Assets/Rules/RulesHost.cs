// Option A (Hatch, 2026-10-31): Unity runs the web game's own rules file; Unity only draws, takes input and shows
// UI. This host loads the packed rules (one JavaScript file built from the private game repo on Hatch's PC, never
// committed here) into Jint, an embedded JavaScript engine for .NET, and plays a replay scenario through it.
// Proven in plain .NET: the 3 city-start replays match exactly at day 3. UNTESTED inside Unity.
// Live play (newGame/tick/view) is the API in the web game's tools/replay/host.ts, packed into the same file.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Hatchfield.Sim.Replay;
using Jint;
using Jint.Native;

namespace Hatchfield.Rules
{
    public sealed class RulesHost : IReplayPlayer
    {
        // What a browser gives the rules and a bare engine lacks: in-memory storage for saves, a clock for
        // input de-duplication, and structuredClone (the rules copy plain data with it).
        const string Globals = @"
const __store = new Map();
globalThis.localStorage = { get length() { return __store.size }, clear: () => __store.clear(),
  getItem: (k) => __store.has(k) ? __store.get(k) : null, key: (i) => [...__store.keys()][i] ?? null,
  removeItem: (k) => __store.delete(k), setItem: (k, v) => __store.set(k, String(v)) };
globalThis.sessionStorage = globalThis.localStorage;
globalThis.performance = globalThis.performance || { now: () => Date.now() };
globalThis.structuredClone = globalThis.structuredClone || ((v) => v === undefined ? undefined : JSON.parse(JSON.stringify(v)));
";

        readonly Engine engine;
        readonly JsValue play;
        readonly JsValue newGame;
        readonly JsValue tick;
        readonly JsValue view;
        JsValue game = JsValue.Undefined;
        string scenario = "city-start";

        /// <summary>The loaded rules file's fingerprint: the first 12 hex digits of its sha256 (as sha256sum prints
        /// it), so a stale copy in Rules~/ is visible next to any result.</summary>
        public string Fingerprint { get; }

        public RulesHost(string rulesFile)
        {
            if (!File.Exists(rulesFile)) throw new FileNotFoundException($"the packed rules file isn't there: {rulesFile} (build it on Hatch's PC: docs/crew/HOW-TO-WORK-HERE.md)");
            Fingerprint = FingerprintOf(rulesFile);
            // Plain .NET has no UnityEngine here (Hatchfield.Rules has noEngineReferences): in Unity this line goes to
            // the Editor log, and HatchfieldRulesDriver repeats the fingerprint in the Console.
            Console.WriteLine($"[rules] {rulesFile}: sha256 {Fingerprint}");
            engine = new Engine(o => o.LimitRecursion(4000).Strict(false));
            engine.Execute(Globals);
            engine.Modules.Add("rules", File.ReadAllText(rulesFile));
            var ns = engine.Modules.Import("rules");
            play = ns.Get("play");
            newGame = ns.Get("newGame");
            tick = ns.Get("tick");
            view = ns.Get("view");
            if (play.IsUndefined()) throw new InvalidOperationException("the rules file has no play() export (pack tools/replay/host.ts)");
        }

        /// <summary>True when the rules file has the live-play API (tools/replay/host.ts), not only replays.</summary>
        public bool CanPlayLive => !newGame.IsUndefined() && !tick.IsUndefined() && !view.IsUndefined();

        /// <summary>Start a live game from a scenario's opening (Hatchfield's city-start: plot, starter kit, staff).</summary>
        public void NewGame(string scenarioName, uint seed)
        {
            if (!CanPlayLive) throw new InvalidOperationException("this rules file can only play replays: re-pack it from tools/replay/host.ts");
            scenario = scenarioName;
            game = newGame.Call(JsValue.FromObject(engine, scenarioName), (double)seed);
        }

        /// <summary>Advance the rules by dtSec game seconds (the web game steps 3 s at a time in replays).</summary>
        public void Tick(double dtSec) => tick.Call(game, dtSec, JsValue.FromObject(engine, scenario));

        /// <summary>What to draw now, as plain data: day, clock, cash, passengers, flights, the tile grid.</summary>
        public Dictionary<string, object> View()
        {
            var text = engine.Evaluate("JSON.stringify").Call(view.Call(game)).AsString();
            return (Dictionary<string, object>)MiniJson.Parse(text);
        }

        /// <summary>The fingerprint of any file, without loading it: first 12 hex digits of its sha256.</summary>
        public static string FingerprintOf(string file)
        {
            using (var sha = SHA256.Create())
            using (var s = File.OpenRead(file))
                return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant().Substring(0, 12);
        }

        /// <summary>The rules file next to this repo's Rules~/ folder, or HATCHFIELD_RULES, or null.</summary>
        public static string FindRulesFile(string startDir)
        {
            var env = Environment.GetEnvironmentVariable("HATCHFIELD_RULES");
            if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;
            for (var d = new DirectoryInfo(startDir); d != null; d = d.Parent)
            {
                var f = Path.Combine(d.FullName, "Rules~", "hatchfield-sim.mjs");
                if (File.Exists(f)) return f;
            }
            return null;
        }

        public List<Checkpoint> Play(string scenario, uint seed, int days, int stepSec)
        {
            var cps = play.Call(JsValue.FromObject(engine, scenario), (double)seed, (double)days, (double)stepSec).AsArray();
            var list = new List<Checkpoint>();
            for (uint i = 0; i < cps.Length; i++)
            {
                var c = cps[i];
                long L(string k) => (long)c.Get(k).AsNumber();
                list.Add(new Checkpoint
                {
                    Day = (int)L("day"), CashCents = L("cashCents"), RevenueCents = L("revenueCents"), PaxServed = (int)L("paxServed"),
                    FlightsDone = (int)L("flightsDone"), ReputationMilli = L("reputationMilli"), BagsLost = (int)L("bagsLost"),
                    LedgerEntries = (int)L("ledgerEntries"), LiveFlights = (int)L("liveFlights"), RngState = (uint)L("rngState"),
                });
            }
            return list;
        }
    }
}
