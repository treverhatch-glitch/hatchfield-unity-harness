// The web game's replay files (replays/*.json, recorded by the web version's tools/replay): a scripted game from
// a seed, with a checkpoint at the end of every game day. The Unity rules must reach the same numbers.
// Until a part of the rules is ported, the port has nothing to play, and the check says so instead of passing.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Hatchfield.Sim.Replay
{
    public sealed class Checkpoint
    {
        public int Day;
        public long CashCents;
        public long RevenueCents;
        public int PaxServed;
        public int FlightsDone;
        public long ReputationMilli;
        public int BagsLost;
        public int LedgerEntries;
        public int LiveFlights;
        public uint RngState;
    }

    public sealed class ReplayFile
    {
        public const string Format = "hatchfield-replay/1";
        public string Scenario = "";
        public uint Seed;
        public int Days;
        public int StepSec;
        public string RulesCommit;
        public List<Checkpoint> Checkpoints = new List<Checkpoint>();

        public Checkpoint Last => Checkpoints.Count > 0 ? Checkpoints[Checkpoints.Count - 1] : null;

        public static ReplayFile FromJson(string json)
        {
            var o = (Dictionary<string, object>)MiniJson.Parse(json);
            if ((o["format"] as string) != Format) throw new FormatException($"not a {Format} file");
            var f = new ReplayFile
            {
                Scenario = (string)o["scenario"],
                Seed = (uint)(double)o["seed"],
                Days = (int)(double)o["days"],
                StepSec = (int)(double)o["stepSec"],
                RulesCommit = o.TryGetValue("rulesCommit", out var rc) ? rc as string : null,
            };
            foreach (Dictionary<string, object> c in (List<object>)o["checkpoints"])
            {
                f.Checkpoints.Add(new Checkpoint
                {
                    Day = (int)(double)c["day"],
                    CashCents = (long)(double)c["cashCents"],
                    RevenueCents = (long)(double)c["revenueCents"],
                    PaxServed = (int)(double)c["paxServed"],
                    FlightsDone = (int)(double)c["flightsDone"],
                    ReputationMilli = (long)(double)c["reputationMilli"],
                    BagsLost = (int)(double)c["bagsLost"],
                    LedgerEntries = (int)(double)c["ledgerEntries"],
                    LiveFlights = (int)(double)c["liveFlights"],
                    RngState = (uint)(double)c["rngState"],
                });
            }
            return f;
        }
    }

    /// <summary>Something that can play a replay's scenario from its seed: the Unity rules, once ported.</summary>
    public interface IReplayPlayer
    {
        /// <summary>The checkpoints for this scenario and seed, or null when that scenario isn't ported yet.</summary>
        List<Checkpoint> Play(string scenario, uint seed, int days, int stepSec);
    }

    /// <summary>The port today: no part of the rules is ported, so it plays nothing.</summary>
    public sealed class NotPortedYet : IReplayPlayer
    {
        public List<Checkpoint> Play(string scenario, uint seed, int days, int stepSec) => null;
    }

    public static class ReplayCheck
    {
        /// <summary>The web game's final-day numbers for one seed, as one JSON line (same shape as the web parity lines).</summary>
        public static string ExpectedLine(ReplayFile f)
        {
            var c = f.Last;
            return string.Format(CultureInfo.InvariantCulture,
                "{{\"seed\":{0},\"day\":{1},\"cash\":{2},\"pax\":{3},\"flights\":{4}}}", f.Seed, c.Day, c.CashCents, c.PaxServed, c.FlightsDone);
        }

        /// <summary>Compare the port with a replay, day by day: "match", "not ported yet", or the first difference.</summary>
        public static string Compare(ReplayFile f, IReplayPlayer port)
        {
            var got = port.Play(f.Scenario, f.Seed, f.Days, f.StepSec);
            if (got == null) return "not ported yet";
            for (int i = 0; i < f.Checkpoints.Count; i++)
            {
                if (i >= got.Count) return $"day {f.Checkpoints[i].Day}: missing in the port";
                var a = f.Checkpoints[i];
                var b = got[i];
                if (a.CashCents != b.CashCents) return $"day {a.Day}: cash {a.CashCents} on the web, {b.CashCents} in Unity";
                if (a.PaxServed != b.PaxServed) return $"day {a.Day}: passengers {a.PaxServed} on the web, {b.PaxServed} in Unity";
                if (a.FlightsDone != b.FlightsDone) return $"day {a.Day}: flights {a.FlightsDone} on the web, {b.FlightsDone} in Unity";
                if (a.ReputationMilli != b.ReputationMilli) return $"day {a.Day}: reputation {a.ReputationMilli} on the web, {b.ReputationMilli} in Unity";
                if (a.RngState != b.RngState) return $"day {a.Day}: dice position {a.RngState} on the web, {b.RngState} in Unity";
            }
            return "match";
        }
    }
}
