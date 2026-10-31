// Builds the camera rig's proof scene (Assets/Scenes/CameraRigTest.unity) and renders a scripted camera run to PNGs.
//
//   Unity.exe -batchmode -projectPath <repo> -executeMethod Hatchfield.CameraRig.EditorTools.CameraRigProof.BuildScene -quit
//   Unity.exe -batchmode -projectPath <repo> -executeMethod Hatchfield.CameraRig.EditorTools.CameraRigProof.Capture -proofDir <folder> -quit
//
// Capture drives the rig through its API (ApronOrbit steps at 30 fps: the same calls the rig makes for held keys,
// wheel notches and drags), not through input devices; the bindings themselves are covered by the EditMode tests.
// It writes frame_000.png..., state.csv (the rig state per frame) and stats.csv (mean brightness, change from the
// previous frame), so black or frozen frames show up in numbers, not only by eye.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Hatchfield.CameraRig.EditorTools
{
    public static class CameraRigProof
    {
        public const string ScenePath = "Assets/Scenes/CameraRigTest.unity";
        const string MatDir = "Assets/Game/Camera/TestScene";
        const float PlotMetres = 704, Half = 352, Cell = 22;

        [MenuItem("Hatchfield/Camera Rig/Build test scene")]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Game/Camera", "TestScene");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 100000;               // HDRP: lux, about full daylight
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<ApronCameraRig>();
            rig.Apply();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);   // 10 x 10 m
            ground.name = "Ground 704 m";
            ground.transform.localScale = new Vector3(PlotMetres / 10, 1, PlotMetres / 10);
            Paint(ground, "Ground", new Color(0.36f, 0.42f, 0.33f));

            var fence = new GameObject("Fence (plot edge, +-352 m)").transform;
            var fenceMat = Mat("Fence", new Color(0.95f, 0.95f, 0.9f));
            Block(fence, "Fence N", new Vector3(0, 1.25f, Half), new Vector3(PlotMetres, 2.5f, 0.6f), fenceMat);
            Block(fence, "Fence S", new Vector3(0, 1.25f, -Half), new Vector3(PlotMetres, 2.5f, 0.6f), fenceMat);
            Block(fence, "Fence E", new Vector3(Half, 1.25f, 0), new Vector3(0.6f, 2.5f, PlotMetres), fenceMat);
            Block(fence, "Fence W", new Vector3(-Half, 1.25f, 0), new Vector3(0.6f, 2.5f, PlotMetres), fenceMat);

            var markers = new GameObject("Markers").transform;
            var red = Mat("Marker Red", new Color(0.85f, 0.15f, 0.12f));
            var blue = Mat("Marker Blue", new Color(0.15f, 0.3f, 0.85f));
            var yellow = Mat("Marker Yellow", new Color(0.95f, 0.8f, 0.15f));
            var grey = Mat("Marker Grey", new Color(0.55f, 0.55f, 0.58f));
            Block(markers, "Origin (red, one cell)", new Vector3(0, Cell / 2, 0), Vector3.one * Cell, red);
            Block(markers, "+x (blue)", new Vector3(176, Cell / 2, 0), Vector3.one * Cell, blue);
            Block(markers, "+z (yellow)", new Vector3(0, Cell / 2, 176), Vector3.one * Cell, yellow);
            Block(markers, "Tower (-x -z)", new Vector3(-176, 40, -176), new Vector3(Cell, 80, Cell), grey);
            for (int i = -3; i <= 3; i++)                                    // a row of small posts every 2 cells along x
                if (i != 0) Block(markers, "Post " + i, new Vector3(i * 44, 3, -44), new Vector3(4, 6, 4), grey);
            for (int c = 0; c < 4; c++)                                       // corner cubes inside the fence
                Block(markers, "Corner " + c, new Vector3((c % 2 == 0 ? 1 : -1) * 330, 5, (c < 2 ? 1 : -1) * 330), Vector3.one * 10, c % 2 == 0 ? blue : yellow);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[camera-rig] scene saved: " + ScenePath);
        }

        static Material Mat(string name, Color c)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", c);
            m.color = c;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void Paint(GameObject go, string matName, Color c) => go.GetComponent<Renderer>().sharedMaterial = Mat(matName, c);

        static void Block(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // ---- The scripted run ----

        public static void Capture()
        {
            var dir = Arg("-proofDir") ?? Path.GetFullPath("Temp/camera-rig-proof");
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var rig = UnityEngine.Object.FindAnyObjectByType<ApronCameraRig>();
            var cam = rig.GetComponent<Camera>();
            const int W = 960, H = 540;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var log = new StringBuilder("frame,t,second,phase,pivotX,pivotZ,yaw,pitch,dist,eyeX_m,eyeY_m,eyeZ_m\n");
            var stats = new StringBuilder("frame,meanLuma,meanAbsDiffFromPrev\n");
            var inv = CultureInfo.InvariantCulture;
            Debug.Log($"[camera-rig] render pipeline: {(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : "built-in")}, device {SystemInfo.graphicsDeviceType}");

            rig.State = ApronOrbit.Start;
            rig.Apply();
            for (int i = 0; i < 60; i++) cam.Render();             // let auto exposure settle

            const double dt = 1.0 / 30;
            byte[] prev = null;
            var s = ApronOrbit.Start;
            var aspect = (double)W / H;
            int frame = 0;
            foreach (var (phase, seconds, step) in Script(aspect))
            {
                var n = (int)Math.Round(seconds / dt);
                for (int i = 0; i < n; i++, frame++)
                {
                    s = step(s, dt, i);
                    rig.State = s;
                    rig.Apply();
                    s = rig.State;
                    for (int r = 0; r < 3; r++) cam.Render();      // same pose 3 times: no motion blur / history smear
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    var raw = tex.GetRawTextureData();
                    double luma = 0, diff = 0;
                    for (int p = 0; p < raw.Length; p += 3)
                    {
                        luma += 0.2126 * raw[p] + 0.7152 * raw[p + 1] + 0.0722 * raw[p + 2];
                        if (prev != null) diff += Math.Abs(raw[p] - prev[p]) + Math.Abs(raw[p + 1] - prev[p + 1]) + Math.Abs(raw[p + 2] - prev[p + 2]);
                    }
                    luma /= raw.Length / 3;
                    diff /= raw.Length;
                    prev = (byte[])raw.Clone();
                    File.WriteAllBytes(Path.Combine(dir, $"frame_{frame:000}.png"), tex.EncodeToPNG());
                    var eye = ApronOrbit.ToUnityMetres(ApronOrbit.Eye(s));
                    log.AppendLine(string.Join(",", frame, (frame * dt).ToString("0.000", inv), i, phase, F(s.Pivot.X), F(s.Pivot.Z), F(s.Yaw), F(s.Pitch), F(s.Dist), F(eye.X), F(eye.Y), F(eye.Z)));
                    stats.AppendLine(string.Join(",", frame, luma.ToString("0.00", inv), diff.ToString("0.000", inv)));
                }
            }
            File.WriteAllText(Path.Combine(dir, "state.csv"), log.ToString());
            File.WriteAllText(Path.Combine(dir, "stats.csv"), stats.ToString());
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"[camera-rig] captured {frame} frames to {dir}");
        }

        static string F(double v) => v.ToString("0.0000", CultureInfo.InvariantCulture);

        // Each phase: what is "held", for how long. Steps are the rig's own calls (ApronOrbit), 30 fps.
        static IEnumerable<(string, double, Func<OrbitState, double, int, OrbitState>)> Script(double aspect)
        {
            const double speed = ApronOrbit.DefaultCameraSpeed;
            yield return ("start pose", 1.0, (s, dt, i) => s);
            yield return ("W held (pan forward)", 1.5, (s, dt, i) => ApronOrbit.KeyPan(s, 0, 1, speed, dt));
            yield return ("D held (pan right)", 1.5, (s, dt, i) => ApronOrbit.KeyPan(s, 1, 0, speed, dt));
            yield return ("E held (rotate)", 1.5, (s, dt, i) => ApronOrbit.KeyRotate(s, 1, dt));
            yield return ("wheel up x10 at cursor (0.5,-0.3)", 1.0, (s, dt, i) =>
            {
                if (i % 3 != 0) return s;                               // one notch every 0.1 s
                return ApronOrbit.ScreenToGround(s, 0.5, -0.3, aspect, out var a) ? ApronOrbit.ZoomToward(s, ApronOrbit.WheelZoomDelta(-100), a) : s;
            });
            yield return ("- held (zoom out)", 1.5, (s, dt, i) => ApronOrbit.KeyZoom(s, -1, dt));
            yield return ("right drag (orbit: 6 px right, 3 px up a frame)", 1.5, (s, dt, i) => ApronOrbit.DragOrbit(s, 6, -3));
            yield return ("left drag (pan: 10 px left a frame)", 1.0, (s, dt, i) => ApronOrbit.DragPan(s, -10, 0));
            yield return ("Q held (rotate back)", 1.5, (s, dt, i) => ApronOrbit.KeyRotate(s, -1, dt));
            yield return ("= held (zoom in)", 1.0, (s, dt, i) => ApronOrbit.KeyZoom(s, 1, dt));
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
