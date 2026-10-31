using System;
using System.IO;
using System.Linq;
using Hatchfield.Rules;
using Hatchfield.Sim.Replay;

// dotnet run --project tools/SimCli -- replay [replays folder] [packed rules file]
// One line per replay file: what the web game reached, and what the Unity port says.
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "live")
        {
            // dotnet run --project tools/SimCli -- live [ticks] : the live API Unity uses (newGame, tick, view).
            var file = RulesHost.FindRulesFile(Directory.GetCurrentDirectory());
            if (file == null) { Console.Error.WriteLine("no packed rules file (Rules~/hatchfield-sim.mjs)"); return 1; }
            var host = new RulesHost(file);
            if (!host.CanPlayLive) { Console.Error.WriteLine("this rules file has no live API: re-pack it from tools/replay/host.ts"); return 1; }
            int ticks = args.Length > 1 ? int.Parse(args[1]) : 60;
            host.NewGame("city-start", 1);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < ticks; i++) host.Tick(3);
            var v = host.View();
            var flights = (System.Collections.Generic.List<object>)v["flights"];
            var tiles = (System.Collections.Generic.List<object>)v["tiles"];
            Console.WriteLine($"live city-start seed 1 after {ticks} ticks of 3 s: day {v["day"]}, cash {(double)v["cash"]:F0}, pax {v["paxServed"]}, {v["cols"]}x{v["rows"]} grid with {tiles.Count} built tiles, {flights.Count} flight(s) in the air or on the ground ({sw.Elapsed.TotalMilliseconds / ticks:F0} ms per tick)");
            return 0;
        }
        if (args.Length == 0 || args[0] != "replay")
        {
            Console.Error.WriteLine("usage: SimCli replay [folder]");
            return 2;
        }
        var dir = args.Length > 1 ? args[1] : "replays";
        var files = Directory.GetFiles(dir, "*.json").OrderBy(f => f).ToArray();
        if (files.Length == 0) { Console.Error.WriteLine($"no replay files in {dir}"); return 1; }
        var rules = args.Length > 2 ? args[2] : RulesHost.FindRulesFile(Directory.GetCurrentDirectory());
        IReplayPlayer port;
        if (rules == null) { Console.WriteLine("no packed rules file (Rules~/hatchfield-sim.mjs or HATCHFIELD_RULES): build it on Hatch's PC; until then nothing plays"); port = new NotPortedYet(); }
        else { Console.WriteLine($"rules: {rules} (the web game's own rules, in Jint)"); port = new RulesHost(rules); }
        int matched = 0;
        foreach (var path in files)
        {
            var f = ReplayFile.FromJson(File.ReadAllText(path));
            var verdict = ReplayCheck.Compare(f, port);
            if (verdict == "match") matched++;
            var c = f.Last;
            Console.WriteLine($"{Path.GetFileName(path)}  web day {c.Day}: cash {c.CashCents / 100.0:F2}  pax {c.PaxServed}  flights {c.FlightsDone}  |  port (web rules in Jint, .NET): {verdict}");
            Console.WriteLine(ReplayCheck.ExpectedLine(f));
        }
        Console.WriteLine($"{matched}/{files.Length} replay(s) match through the port");
        return 0;
    }
}
