// PU-22: the web game's deterministic math (src/game/detMath.ts, packed into the rules file) must give the same
// bits in Jint as in Node, or replays drift. With native Math they differ in about 10% of the fixture's outputs on
// .NET 8/Linux (hypot 44%, atan2 11%, pow 10%); Windows' C runtime differs again.
//
// Needs the packed rules file (Rules~/hatchfield-sim.mjs, never committed): without it the tests are Ignored with
// the reason, not failed. The full fixture (Rules~/detmath-fixture.txt, built by the web game's
// tools/replay/detmath-fixture.ts --unity <this project>) is optional; the committed sample always runs.
using System.IO;
using NUnit.Framework;
using Hatchfield.Rules;

namespace Hatchfield.Sim.Tests
{
    public class DetMathTests
    {
        static string Root() => Path.GetDirectoryName(ReplayTests.ReplayDir());
        static string RulesFile() => Path.Combine(Root(), "Rules~", "hatchfield-sim.mjs");
        static string FixtureFile() => Path.Combine(Root(), "Rules~", "detmath-fixture.txt");
        static string ExpectedFile() => Path.Combine(Root(), "tools", "SimCli", "detmath-expected.json");

        static RulesHost Host()
        {
            if (!File.Exists(RulesFile())) Assert.Ignore($"rules file not found at {RulesFile()}: pack it from the web game (npm run pack:rules) and copy it to Rules~/");
            TestContext.WriteLine($"rules file sha256 {RulesHost.FingerprintOf(RulesFile())}");
            var host = new RulesHost(RulesFile());
            if (!host.HasExport("detSin")) Assert.Ignore("this rules file predates detMath (PU-22): re-pack it");
            return host;
        }

        [Test, Category("Slow")]
        public void TheCommittedSampleIsBitIdenticalInJint()
        {
            var expected = DetMathCheck.Expected.Load(ExpectedFile());
            Assert.AreEqual("hatchfield-detmath/1", expected.Format);
            var check = new DetMathCheck(Host());
            check.Run(expected.Sample);
            TestContext.WriteLine(check.Report());
            Assert.Greater(check.Total, 400, "the sample has every function");
            Assert.AreEqual(0, check.Mismatches, check.Report());
        }

        [Test, Category("Slow"), Timeout(10 * 60 * 1000)]
        public void TheFullFixtureIsBitIdenticalInJint()
        {
            var host = Host();
            if (!File.Exists(FixtureFile())) Assert.Ignore($"no full fixture at {FixtureFile()}: build it in the web game (node tools/replay/detmath-fixture.ts --unity <this project>)");
            var check = new DetMathCheck(host);
            check.Run(File.ReadLines(FixtureFile()));
            TestContext.WriteLine(check.Report());
            var diff = check.FixtureDifferences(DetMathCheck.Expected.Load(ExpectedFile()));
            Assert.IsEmpty(diff, "the fixture isn't the one the web game expects: " + string.Join("; ", diff));
            Assert.AreEqual(0, check.Mismatches, check.Report());
        }

        [Test]
        public void HexMatchesTheWebGame()
        {
            Assert.AreEqual("8000000000000000", DetMathCheck.Hex(-0.0));
            Assert.AreEqual("7ff8000000000000", DetMathCheck.Hex(double.NaN));
            Assert.AreEqual("3ff0000000000000", DetMathCheck.Hex(1.0));
            Assert.AreEqual(-0.0, DetMathCheck.FromHex("8000000000000000"));
            Assert.IsTrue(double.IsNegative(DetMathCheck.FromHex("8000000000000000")));
        }
    }
}
