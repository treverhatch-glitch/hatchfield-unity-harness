// The Unity side of the replay check. Pure NUnit on the pure sim: Unity runs these in Edit Mode, and
// tools/SimTests runs the same file with .NET (dotnet test), with no editor and no licence.
using System;
using System.IO;
using NUnit.Framework;
using Hatchfield.Sim.Replay;

namespace Hatchfield.Sim.Tests
{
    public class ReplayTests
    {
        static string ReplayDir()
        {
            // The repo root's replays/ folder, found from the test's own folder upward.
            var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "replays"))) d = d.Parent;
            Assert.IsNotNull(d, "replays/ not found above the test folder");
            return Path.Combine(d.FullName, "replays");
        }

        static ReplayFile Load(int seed) => ReplayFile.FromJson(File.ReadAllText(Path.Combine(ReplayDir(), $"city-start-{seed}.json")));

        [Test]
        public void ReadsTheWebGamesReplayFiles()
        {
            var expect = new[] { (1, 25730600L, 399, 6), (2, 25964400L, 365, 5), (3, 25988400L, 367, 6) };
            foreach (var (seed, cash, pax, flights) in expect)
            {
                var f = Load(seed);
                Assert.AreEqual("city-start", f.Scenario);
                Assert.AreEqual(3, f.Days);
                Assert.AreEqual(4, f.Checkpoints.Count, "day 0 to day 3");
                Assert.AreEqual(cash, f.Last.CashCents, $"seed {seed} day-3 cash");
                Assert.AreEqual(pax, f.Last.PaxServed, $"seed {seed} day-3 passengers");
                Assert.AreEqual(flights, f.Last.FlightsDone, $"seed {seed} day-3 flights");
            }
        }

        [Test]
        public void TheDiceMatchTheWebGameBitForBit()
        {
            // new SimRng(1337) in the web game's src/game/simRng.ts: first three rolls and the state after them.
            var r = new SimRng(1337);
            Assert.AreEqual(0.18441183259710670, r.Next(), 0);
            Assert.AreEqual(0.18998925131745636, r.Next(), 0);
            Assert.AreEqual(0.81047199224121869, r.Next(), 0);
            Assert.AreEqual(1199731480u, r.State);
        }

        [Test]
        public void TheCheckSaysNotPortedYetInsteadOfPassing()
        {
            Assert.AreEqual("not ported yet", ReplayCheck.Compare(Load(1), new NotPortedYet()));
        }

        sealed class OffByOnePassenger : IReplayPlayer
        {
            readonly ReplayFile source;
            public OffByOnePassenger(ReplayFile f) { source = f; }
            public System.Collections.Generic.List<Checkpoint> Play(string scenario, uint seed, int days, int stepSec)
            {
                var copy = new System.Collections.Generic.List<Checkpoint>();
                foreach (var c in source.Checkpoints) copy.Add(new Checkpoint { Day = c.Day, CashCents = c.CashCents, PaxServed = c.PaxServed + (c.Day == 2 ? 1 : 0), FlightsDone = c.FlightsDone, ReputationMilli = c.ReputationMilli, RngState = c.RngState });
                return copy;
            }
        }

        [Test]
        public void AMismatchNamesTheDayAndTheNumber()
        {
            var f = Load(2);
            Assert.AreEqual("match", ReplayCheck.Compare(f, new OffByOnePassengerFixed(f)));
            StringAssert.StartsWith("day 2: passengers", ReplayCheck.Compare(f, new OffByOnePassenger(f)));
        }

        sealed class OffByOnePassengerFixed : IReplayPlayer
        {
            readonly ReplayFile source;
            public OffByOnePassengerFixed(ReplayFile f) { source = f; }
            public System.Collections.Generic.List<Checkpoint> Play(string scenario, uint seed, int days, int stepSec) => source.Checkpoints;
        }
    }
}
