// The apron camera's math, copied from the web game (src/render/world.ts, orbit.ts, input/actions.ts, Field.tsx).
// Pure C#: no UnityEngine (this assembly is built with noEngineReferences), so the EditMode tests can check it alone.
//
// Units. Every number here is in WEB units, exactly as the web game has it, so the feel matches 1:1. The web game
// uses WORLD_PER_METER = 0.45 (1 metre = 0.45 web units), so 1 web unit = 1/0.45 m = 2.222 m. The Unity scene is in
// metres; ToUnityMetres() is the only place the conversion happens (WebUnitsToMetres). For scale: a cell is 22 m,
// a plot is 32 x 32 cells = 704 m (316.8 web units), centred on the origin.
//
// Axes. three.js is right-handed (x right, y up, +z toward the viewer); Unity is left-handed. ToUnityMetres() also
// flips z (Unity z = -web z), so the picture, and the direction every key and drag moves it, match the web game
// instead of coming out mirrored. All the rig's math stays in web space.
using System;

namespace Hatchfield.CameraRig
{
    /// <summary>A point or direction in web space (double precision, like JavaScript numbers).</summary>
    public readonly struct Vec3
    {
        public readonly double X, Y, Z;
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double k) => new Vec3(a.X * k, a.Y * k, a.Z * k);
        public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vec3 Normalized { get { var l = Length; return l > 0 ? this * (1 / l) : this; } }
        public override string ToString() => $"({X:0.####}, {Y:0.####}, {Z:0.####})";
    }

    /// <summary>The whole camera state: a pivot on the ground (y = 0) and yaw, pitch and distance around it.</summary>
    public struct OrbitState
    {
        public Vec3 Pivot;   // web units, y always 0
        public double Yaw;   // radians
        public double Pitch; // radians
        public double Dist;  // web units
    }

    public static class ApronOrbit
    {
        // ---- The web game's numbers (web units, radians, seconds) ----
        public const double WorldPerMeter = 0.45;              // web WORLD_PER_METER
        public const double WebUnitsToMetres = 1 / WorldPerMeter;
        public const double FovDeg = 34, Near = 0.1, Far = 1200;
        public const double StartYaw = 0.72, StartPitch = 0.68, StartDist = 88;
        public const double MinDist = 12, MaxDist = 280;
        public const double MinPitch = 0.28, MaxPitch = 1.35;
        public const double KeyPanRate = 10;                   // units/s, times cameraSpeed and panZoomScale
        public const double DefaultCameraSpeed = 2, MinCameraSpeed = 0.5, MaxCameraSpeed = 4;
        public const double PanScaleRefDist = 88, MinPanScale = 0.35, MaxPanScale = 2.6;
        public const double KeyRotateRate = 1.4;               // rad/s (Q / E)
        public const double KeyZoomRate = 48;                  // units/s (= / -)
        public const double WheelZoomPerDeltaY = 0.06;         // dist += deltaY * 0.06 (browser deltaY, ~100 a notch)
        public const double OrbitYawPerPx = 0.007, OrbitPitchPerPx = 0.005;
        public const double DragPanPerPx = 0.08;
        public const double ClickSlopPx = 8;                   // a left press that moves less than this is a click

        public static OrbitState Start => new OrbitState { Pivot = new Vec3(0, 0, 0), Yaw = StartYaw, Pitch = StartPitch, Dist = StartDist };

        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
        public static double ClampDist(double d) => Clamp(d, MinDist, MaxDist);
        public static double ClampPitch(double p) => Clamp(p, MinPitch, MaxPitch);
        public static double ClampCameraSpeed(double s) => Clamp(s, MinCameraSpeed, MaxCameraSpeed);
        public static double PanZoomScale(double dist) => Clamp(dist / PanScaleRefDist, MinPanScale, MaxPanScale);

        /// <summary>(sin(yaw)cos(pitch), sin(pitch), cos(yaw)cos(pitch)): from the pivot toward the eye.</summary>
        public static Vec3 EyeDirection(double yaw, double pitch) =>
            new Vec3(Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(yaw) * Math.Cos(pitch));

        public static Vec3 Eye(in OrbitState s) => s.Pivot + EyeDirection(s.Yaw, s.Pitch) * s.Dist;

        /// <summary>Ground-plane forward (away from the camera, toward the pivot) for this yaw.</summary>
        public static Vec3 GroundForward(double yaw) => new Vec3(-Math.Sin(yaw), 0, -Math.Cos(yaw));
        /// <summary>Ground-plane screen-right for this yaw (three.js: forward x up).</summary>
        public static Vec3 GroundRight(double yaw) => new Vec3(Math.Cos(yaw), 0, -Math.Sin(yaw));

        public static OrbitState Clamped(OrbitState s)
        {
            s.Dist = ClampDist(s.Dist);
            s.Pitch = ClampPitch(s.Pitch);
            return s;
        }

        /// <summary>Held keys: move.x = right(+)/left(-), move.y = forward(+)/back(-), each -1..1.</summary>
        public static OrbitState KeyPan(OrbitState s, double moveX, double moveY, double cameraSpeed, double dt)
        {
            var step = KeyPanRate * ClampCameraSpeed(cameraSpeed) * PanZoomScale(s.Dist) * dt;
            s.Pivot = s.Pivot + GroundRight(s.Yaw) * (moveX * step) + GroundForward(s.Yaw) * (moveY * step);
            return s;
        }

        /// <summary>Q / E: axis -1 (Q) .. +1 (E). Continuous: yaw changes by axis * 1.4 rad/s * dt.</summary>
        public static OrbitState KeyRotate(OrbitState s, double axis, double dt)
        {
            s.Yaw += axis * KeyRotateRate * dt;
            return s;
        }

        /// <summary>= / -: axis +1 zooms in (=), -1 zooms out (-), at 48 units/s, about the pivot.</summary>
        public static OrbitState KeyZoom(OrbitState s, double axis, double dt)
        {
            s.Dist = ClampDist(s.Dist - axis * KeyZoomRate * dt);
            return s;
        }

        /// <summary>The distance change for one browser-style wheel deltaY (positive = scroll down = zoom out).</summary>
        public static double WheelZoomDelta(double deltaY) => deltaY * WheelZoomPerDeltaY;

        /// <summary>
        /// Changes the distance by <paramref name="deltaDist"/> (then clamps), keeping <paramref name="anchor"/> (a ground
        /// point, normally the one under the cursor) where it is on screen: the whole camera is scaled about the
        /// anchor by newDist/oldDist, with yaw and pitch unchanged, so every ray through the anchor stays the same ray.
        /// </summary>
        public static OrbitState ZoomToward(OrbitState s, double deltaDist, Vec3 anchor)
        {
            var newDist = ClampDist(s.Dist + deltaDist);
            var k = newDist / s.Dist;
            s.Pivot = anchor + (s.Pivot - anchor) * k;
            s.Pivot = new Vec3(s.Pivot.X, 0, s.Pivot.Z);
            s.Dist = newDist;
            return s;
        }

        /// <summary>Right or middle drag. dx, dy in screen pixels, browser convention (dy positive = down).</summary>
        public static OrbitState DragOrbit(OrbitState s, double dxPx, double dyPx)
        {
            s.Yaw -= dxPx * OrbitYawPerPx;
            s.Pitch = ClampPitch(s.Pitch - dyPx * OrbitPitchPerPx);
            return s;
        }

        /// <summary>Left drag (select mode, after the 8 px slop). dx, dy in pixels, browser convention.</summary>
        public static OrbitState DragPan(OrbitState s, double dxPx, double dyPx)
        {
            var k = DragPanPerPx * PanZoomScale(s.Dist);
            s.Pivot = s.Pivot + GroundRight(s.Yaw) * (-dxPx * k) + GroundForward(s.Yaw) * (dyPx * k);
            return s;
        }

        // ---- Screen <-> world (a perspective camera at Eye looking at the pivot, vertical FOV 34 deg) ----

        static void Basis(in OrbitState s, out Vec3 eye, out Vec3 fwd, out Vec3 right, out Vec3 up)
        {
            eye = Eye(s);
            fwd = (s.Pivot - eye).Normalized;
            right = Vec3.Cross(fwd, new Vec3(0, 1, 0)).Normalized;
            up = Vec3.Cross(right, fwd);
        }

        /// <summary>The ray through a screen point. ndcX, ndcY in -1..1 (y up), aspect = width / height.</summary>
        public static void ScreenRay(in OrbitState s, double ndcX, double ndcY, double aspect, out Vec3 origin, out Vec3 dir)
        {
            Basis(s, out origin, out var f, out var r, out var u);
            var t = Math.Tan(FovDeg * Math.PI / 360);
            dir = (f + r * (ndcX * t * aspect) + u * (ndcY * t)).Normalized;
        }

        /// <summary>Where the ray through a screen point meets the ground (y = 0). False if it never does.</summary>
        public static bool ScreenToGround(in OrbitState s, double ndcX, double ndcY, double aspect, out Vec3 hit)
        {
            ScreenRay(s, ndcX, ndcY, aspect, out var o, out var d);
            hit = default;
            if (d.Y > -1e-9) return false;
            var t = -o.Y / d.Y;
            hit = o + d * t;
            hit = new Vec3(hit.X, 0, hit.Z);
            return true;
        }

        /// <summary>A world point to screen ndc (-1..1, y up). False if it is behind the camera.</summary>
        public static bool WorldToScreen(in OrbitState s, Vec3 p, double aspect, out double ndcX, out double ndcY)
        {
            Basis(s, out var eye, out var f, out var r, out var u);
            var v = p - eye;
            var z = Vec3.Dot(v, f);
            var t = Math.Tan(FovDeg * Math.PI / 360);
            ndcX = ndcY = 0;
            if (z <= 0) return false;
            ndcX = Vec3.Dot(v, r) / (z * t * aspect);
            ndcY = Vec3.Dot(v, u) / (z * t);
            return true;
        }

        /// <summary>Web space to the Unity scene: metres, and z flipped (right- to left-handed). See the header.</summary>
        public static Vec3 ToUnityMetres(Vec3 web) => new Vec3(web.X * WebUnitsToMetres, web.Y * WebUnitsToMetres, -web.Z * WebUnitsToMetres);
        public static Vec3 FromUnityMetres(Vec3 unity) => new Vec3(unity.X / WebUnitsToMetres, unity.Y / WebUnitsToMetres, -unity.Z / WebUnitsToMetres);
    }
}
