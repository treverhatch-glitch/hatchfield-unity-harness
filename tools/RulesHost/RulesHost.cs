// Option A (Hatch, 2026-10-31): Unity runs the web game's own rules file; Unity only draws, takes input and shows
// UI. This host loads the packed rules (one JavaScript file built from the private game repo on Hatch's PC, never
// committed here) into Jint, an embedded JavaScript engine for .NET, and plays a replay scenario through it.
// Proven in plain .NET: the 3 city-start replays match exactly at day 3. UNTESTED inside Unity.
using System;
using System.Collections.Generic;
using System.IO;
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

        public RulesHost(string rulesFile)
        {
            if (!File.Exists(rulesFile)) throw new FileNotFoundException($"the packed rules file isn't there: {rulesFile} (build it on Hatch's PC: docs/crew/HOW-TO-WORK-HERE.md)");
            engine = new Engine(o => o.LimitRecursion(4000).Strict(false));
            engine.Execute(Globals);
            engine.Modules.Add("rules", File.ReadAllText(rulesFile));
            var ns = engine.Modules.Import("rules");
            play = ns.Get("play");
            if (play.IsUndefined()) throw new InvalidOperationException("the rules file has no play() export (pack tools/replay/replay.ts)");
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
