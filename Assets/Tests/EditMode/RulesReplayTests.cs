// The replay check through the web game's own rules (P-036): each replay is played by the packed rules file in
// Jint (Assets/Rules/RulesHost.cs) and must match the web game day by day. Unity runs these in Edit Mode (Test
// Runner > EditMode, category "Slow"); tools/SimTests runs them with dotnet test, no editor needed.
//
// The rules file is the private game's code: never committed, so CI and fresh clones of this public repo don't
// have it. Without it the tests are Ignored with the reason, not failed. With it, the output names the file's
// fingerprint (first 12 hex of its sha256, P-034), so a stale copy in Rules~/ is visible. About a minute per seed
// in .NET.
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Hatchfield.Rules;
using Hatchfield.Sim.Replay;

namespace Hatchfield.Sim.Tests
{
    public class RulesReplayTests
    {
        /// <summary>Rules~/hatchfield-sim.mjs in the repo root: the folder above replays/, found like ReplayDir().</summary>
        static string RulesFile() => Path.Combine(Path.GetDirectoryName(ReplayTests.ReplayDir()), "Rules~", "hatchfield-sim.mjs");

        [TestCase(1), TestCase(2), TestCase(3)]
        [Category("Slow"), Timeout(15 * 60 * 1000)]
        public void TheReplayMatchesThroughTheRules(int seed)
        {
            var rules = RulesFile();
            if (!File.Exists(rules))
                Assert.Ignore($"rules file not found at {rules}: pack it from the web game (npm run pack:rules) and copy it to Rules~/; see docs/crew/HOW-TO-WORK-HERE.md");
            // Written before playing, so the fingerprint is in the output even if loading or playing throws.
            TestContext.WriteLine($"rules file {rules}, sha256 {RulesHost.FingerprintOf(rules)}");

            var replay = ReplayTests.Load(seed);
            var clock = Stopwatch.StartNew();
            var host = new RulesHost(rules);
            var verdict = ReplayCheck.Compare(replay, host);
            clock.Stop();

            TestContext.WriteLine($"seed {seed}: {verdict} in {clock.Elapsed.TotalSeconds:F1} s (rules sha256 {host.Fingerprint})");
            Assert.AreEqual("match", verdict,
                $"seed {seed} through the rules file with sha256 {host.Fingerprint} ({rules}). If that copy is older than the replays, re-pack it (npm run pack:rules in the web game) and copy it to Rules~/");
        }
    }
}
