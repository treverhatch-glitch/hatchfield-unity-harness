// Option A in the Unity editor: the web game's own rules run here (through Jint, Assets/Rules/RulesHost.cs) and
// Unity only draws them. UNTESTED in Unity: proven in plain .NET only (tools/SimCli live / replay).
//
// It starts by itself in Play Mode when the packed rules file is there (Rules~/hatchfield-sim.mjs, built on
// Hatch's PC from the private game repo, never committed). Without it, it shows a note and does nothing else.
//
// The rules run on a worker thread (Jint takes ~0.4 s per 3-second tick, too slow for the frame), one tick of
// StepSec game seconds at a time, as fast as the chosen speed allows. Each tick ends with a snapshot of what to
// draw; the main thread draws the newest snapshot: tiles as blocks (22 m cells, real metres), flights as markers.
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Hatchfield.Rules;
using UnityEngine;

public sealed class HatchfieldRulesDriver : MonoBehaviour
{
    const float CellMetres = 22f;          // METERS_PER_CELL in the web game
    const float WorldPerMetre = 0.45f;     // WORLD_PER_METER: flight positions are in these units
    const double StepSec = 3;              // the replays' step

    public uint Seed = 1;
    [Range(0, 5)] public float Speed = 1;  // game seconds per real second

    RulesHost host;
    Thread worker;
    volatile bool running;
    volatile Dictionary<string, object> latest;
    string note = "";
    double ticksDone;
    readonly Dictionary<string, Transform> flightMarkers = new Dictionary<string, Transform>();
    bool tilesDrawn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<HatchfieldRulesDriver>() == null) new GameObject("Hatchfield rules").AddComponent<HatchfieldRulesDriver>();
    }

    void Start()
    {
        var file = RulesHost.FindRulesFile(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
        if (file == null) { note = "No rules file: build it on this PC into Rules~/hatchfield-sim.mjs (docs/crew/HOW-TO-WORK-HERE.md)."; return; }
        try
        {
            host = new RulesHost(file);
            if (!host.CanPlayLive) { note = "This rules file can only play replays: re-pack it from tools/replay/host.ts."; return; }
            host.NewGame("city-start", Seed);
            latest = host.View();
        }
        catch (System.Exception e) { note = $"Rules failed to load: {e.Message}"; Debug.LogException(e); return; }
        Debug.Log($"[rules] loaded {file}: city-start seed {Seed}");
        running = true;
        worker = new Thread(Loop) { IsBackground = true, Name = "Hatchfield rules" };
        worker.Start();
    }

    void Loop()
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        double gameSecondsOwed = 0;
        double last = 0;
        while (running)
        {
            double now = clock.Elapsed.TotalSeconds;
            gameSecondsOwed += (now - last) * Speed;
            last = now;
            if (gameSecondsOwed < StepSec) { Thread.Sleep(20); continue; }
            gameSecondsOwed -= StepSec;
            try
            {
                host.Tick(StepSec);
                ticksDone++;
                latest = host.View();
            }
            catch (System.Exception e) { note = $"Rules stopped: {e.Message}"; running = false; }
        }
    }

    void OnDestroy() { running = false; }

    void Update()
    {
        var v = latest;
        if (v == null) return;
        if (!tilesDrawn) { DrawTiles(v); tilesDrawn = true; }
        var seen = new HashSet<string>();
        foreach (Dictionary<string, object> f in (List<object>)v["flights"])
        {
            var id = (string)f["id"];
            seen.Add(id);
            if (!flightMarkers.TryGetValue(id, out var t))
            {
                t = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                t.name = (string)f["flightNo"];
                t.localScale = new Vector3(8, 20, 8); // a stand-in about the size of an airliner's fuselage width
                t.rotation = Quaternion.Euler(0, 0, 90);
                flightMarkers[id] = t;
            }
            float x = (float)(double)f["x"] / WorldPerMetre, y = (float)(double)f["y"] / WorldPerMetre, z = (float)(double)f["z"] / WorldPerMetre;
            t.position = new Vector3(x, Mathf.Max(4, y), z);
        }
        foreach (var id in new List<string>(flightMarkers.Keys))
            if (!seen.Contains(id)) { Destroy(flightMarkers[id].gameObject); flightMarkers.Remove(id); }
    }

    void DrawTiles(Dictionary<string, object> v)
    {
        var root = new GameObject("Tiles").transform;
        var colours = new Dictionary<string, Color>
        {
            ["runway"] = new Color(0.2f, 0.2f, 0.22f), ["taxi"] = new Color(0.3f, 0.3f, 0.3f), ["stand"] = new Color(0.45f, 0.45f, 0.45f),
            ["terminal"] = new Color(0.8f, 0.8f, 0.85f), ["road"] = new Color(0.25f, 0.25f, 0.25f),
        };
        foreach (Dictionary<string, object> t in (List<object>)v["tiles"])
        {
            int c = (int)(double)t["c"], r = (int)(double)t["r"];
            var kind = (string)t["kind"];
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            block.SetParent(root, false);
            block.name = $"{kind} {c},{r}";
            float height = kind == "terminal" ? 12f : kind == "tower" ? 30f : 0.4f;
            block.localScale = new Vector3(CellMetres * 0.98f, height, CellMetres * 0.98f);
            block.position = new Vector3(c * CellMetres + CellMetres / 2, height / 2, r * CellMetres + CellMetres / 2);
            var colour = colours.TryGetValue(kind, out var col) ? col : new Color(0.6f, 0.5f, 0.4f);
            var mr = block.GetComponent<Renderer>();
            mr.material.color = colour;
        }
        if (Camera.main != null)
        {
            int cols = (int)(double)v["cols"], rows = (int)(double)v["rows"];
            var centre = new Vector3(cols * CellMetres / 2, 0, rows * CellMetres / 2);
            Camera.main.transform.position = centre + new Vector3(0, 450, -450);
            Camera.main.transform.LookAt(centre);
            Camera.main.farClipPlane = 5000;
        }
    }

    void OnGUI()
    {
        var v = latest;
        var text = note;
        if (v != null) text = $"Hatchfield rules (web game, in Jint) · seed {Seed} · day {v["day"]} · cash {(double)v["cash"]:N0} · passengers {v["paxServed"]} · flights done {v["flightsDone"]} · ticks {ticksDone:N0} · speed {Speed}x\n{note}";
        GUI.Label(new Rect(12, 12, 1200, 60), text);
    }
}
