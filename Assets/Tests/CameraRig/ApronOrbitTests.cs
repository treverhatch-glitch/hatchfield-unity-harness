// EditMode tests for the apron camera's math (Assets/Game/Camera/Math/ApronOrbit.cs). Numbers are the web game's.
using System;
using NUnit.Framework;

namespace Hatchfield.CameraRig.Tests
{
    public class ApronOrbitTests
    {
        const double Eps = 1e-9;

        static void Near(Vec3 a, Vec3 b, double eps = 1e-6) =>
            Assert.That((a - b).Length, Is.LessThan(eps), $"expected {b}, got {a}");

        [Test]
        public void StartPose_EyeMatchesTheWebFormula()
        {
            var s = ApronOrbit.Start;
            Assert.That(s.Yaw, Is.EqualTo(0.72));
            Assert.That(s.Pitch, Is.EqualTo(0.68));
            Assert.That(s.Dist, Is.EqualTo(88));
            Near(s.Pivot, new Vec3(0, 0, 0));
            var eye = ApronOrbit.Eye(s);
            // eye = (sin .72 cos .68, sin .68, cos .72 cos .68) * 88
            var expected = new Vec3(Math.Sin(0.72) * Math.Cos(0.68) * 88, Math.Sin(0.68) * 88, Math.Cos(0.72) * Math.Cos(0.68) * 88);
            Near(eye, expected);
            Near(eye, new Vec3(45.1193, 55.3338, 51.4434), 1e-3); // checked in node: 45.11932, 55.33379, 51.44336
            Assert.That(eye.Length, Is.EqualTo(88).Within(1e-9));
        }

        [Test]
        public void StartPose_InUnityMetres_IsScaledAndZFlipped()
        {
            var eye = ApronOrbit.Eye(ApronOrbit.Start);
            var m = ApronOrbit.ToUnityMetres(eye);
            Assert.That(ApronOrbit.WebUnitsToMetres, Is.EqualTo(1 / 0.45).Within(Eps));
            Near(m, new Vec3(eye.X / 0.45, eye.Y / 0.45, -eye.Z / 0.45));
            Assert.That(m.Length, Is.EqualTo(88 / 0.45).Within(1e-6));  // 195.6 m from the pivot
            Near(ApronOrbit.FromUnityMetres(m), eye);
            // The plot: 32 cells x 22 m = 704 m = 316.8 web units.
            Assert.That(704 * ApronOrbit.WorldPerMeter, Is.EqualTo(316.8).Within(1e-9));
        }

        [Test]
        public void Clamps_DistAndPitch()
        {
            Assert.That(ApronOrbit.ClampDist(5), Is.EqualTo(12));
            Assert.That(ApronOrbit.ClampDist(1000), Is.EqualTo(280));
            Assert.That(ApronOrbit.ClampDist(100), Is.EqualTo(100));
            Assert.That(ApronOrbit.ClampPitch(0), Is.EqualTo(0.28));
            Assert.That(ApronOrbit.ClampPitch(2), Is.EqualTo(1.35));
            Assert.That(ApronOrbit.ClampPitch(0.9), Is.EqualTo(0.9));
            var s = ApronOrbit.Start;
            s = ApronOrbit.KeyZoom(s, +1, 10);           // 480 units in: clamps at 12
            Assert.That(s.Dist, Is.EqualTo(12));
            s = ApronOrbit.KeyZoom(s, -1, 10);           // 480 out: clamps at 280
            Assert.That(s.Dist, Is.EqualTo(280));
            s = ApronOrbit.DragOrbit(s, 0, -10000);      // drag up a long way: pitch to the top
            Assert.That(s.Pitch, Is.EqualTo(1.35));
            s = ApronOrbit.DragOrbit(s, 0, 10000);
            Assert.That(s.Pitch, Is.EqualTo(0.28));
        }

        [Test]
        public void NoPivotBounds()
        {
            var s = ApronOrbit.KeyPan(ApronOrbit.Start, 0, 1, 4, 1000);
            Assert.That(s.Pivot.Length, Is.GreaterThan(10000));
        }

        [TestCase(88, 1.0)]
        [TestCase(12, 0.35)]
        [TestCase(30.8, 0.35)]
        [TestCase(44, 0.5)]
        [TestCase(176, 2.0)]
        [TestCase(228.8, 2.6)]
        [TestCase(280, 2.6)]
        public void PanZoomScale(double dist, double expected) =>
            Assert.That(ApronOrbit.PanZoomScale(dist), Is.EqualTo(expected).Within(1e-9));

        [Test]
        public void KeyPan_SpeedIsTenTimesCameraSpeedTimesPanZoomScale()
        {
            var s = ApronOrbit.Start;                    // dist 88: scale 1
            var a = ApronOrbit.KeyPan(s, 0, 1, 2, 0.5);  // W for 0.5 s at speed 2: 10 units
            Assert.That(a.Pivot.Length, Is.EqualTo(10).Within(1e-9));
            Near(a.Pivot, ApronOrbit.GroundForward(s.Yaw) * 10);
            s.Dist = 176;                                // scale 2
            var b = ApronOrbit.KeyPan(s, 1, 0, 2, 0.5);  // D: 20 units right
            Near(b.Pivot, ApronOrbit.GroundRight(s.Yaw) * 20);
            var c = ApronOrbit.KeyPan(s, 1, 0, 99, 1);   // camera speed clamps to 4
            Assert.That(c.Pivot.Length, Is.EqualTo(10 * 4 * 2).Within(1e-9));
            Assert.That(b.Pivot.Y, Is.EqualTo(0));
        }

        [Test]
        public void KeyPan_ForwardPointsAwayFromTheCameraOnTheGround()
        {
            var s = ApronOrbit.Start;
            var eye = ApronOrbit.Eye(s);
            var f = ApronOrbit.GroundForward(s.Yaw);
            var toPivot = new Vec3(-eye.X, 0, -eye.Z).Normalized;
            Near(f, toPivot);
            Assert.That(Vec3.Dot(f, ApronOrbit.GroundRight(s.Yaw)), Is.EqualTo(0).Within(Eps));
            // Right really is screen-right: a point to the right of the pivot projects right of centre.
            Assert.That(ApronOrbit.WorldToScreen(s, ApronOrbit.GroundRight(s.Yaw) * 5, 16.0 / 9, out var x, out _), Is.True);
            Assert.That(x, Is.GreaterThan(0));
        }

        [Test]
        public void KeyRotate_IsRateTimesDt()
        {
            var s = ApronOrbit.Start;
            Assert.That(ApronOrbit.KeyRotate(s, +1, 0.25).Yaw, Is.EqualTo(0.72 + 1.4 * 0.25).Within(Eps));
            Assert.That(ApronOrbit.KeyRotate(s, -1, 1.0 / 60).Yaw, Is.EqualTo(0.72 - 1.4 / 60).Within(Eps));
            var t = s;
            for (int i = 0; i < 60; i++) t = ApronOrbit.KeyRotate(t, 1, 1.0 / 60);  // one second, frame by frame
            Assert.That(t.Yaw, Is.EqualTo(0.72 + 1.4).Within(1e-9));
        }

        [Test]
        public void KeyZoom_Is48UnitsPerSecond()
        {
            var s = ApronOrbit.Start;
            Assert.That(ApronOrbit.KeyZoom(s, +1, 0.5).Dist, Is.EqualTo(88 - 24).Within(Eps));
            Assert.That(ApronOrbit.KeyZoom(s, -1, 0.5).Dist, Is.EqualTo(88 + 24).Within(Eps));
        }

        [Test]
        public void WheelZoomDelta_IsDeltaYTimes006()
        {
            Assert.That(ApronOrbit.WheelZoomDelta(100), Is.EqualTo(6).Within(Eps));   // one notch down: 6 units out
            Assert.That(ApronOrbit.WheelZoomDelta(-100), Is.EqualTo(-6).Within(Eps));
            Assert.That(ApronOrbit.WheelZoomDelta(53), Is.EqualTo(3.18).Within(Eps));
        }

        [Test]
        public void ZoomTowardCursor_KeepsTheAnchorFixedOnScreen()
        {
            const double aspect = 16.0 / 9;
            var s = ApronOrbit.Start;
            s.Pivot = new Vec3(13, 0, -7);
            foreach (var (nx, ny) in new[] { (0.6, -0.4), (-0.8, 0.3), (0.0, 0.0), (0.95, -0.95) })
            {
                Assert.That(ApronOrbit.ScreenToGround(s, nx, ny, aspect, out var anchor), Is.True);
                foreach (var delta in new[] { -6.0, 6.0, -60.0, 500.0 })
                {
                    var z = ApronOrbit.ZoomToward(s, delta, anchor);
                    Assert.That(z.Dist, Is.EqualTo(ApronOrbit.ClampDist(s.Dist + delta)).Within(Eps));
                    Assert.That(z.Pivot.Y, Is.EqualTo(0).Within(Eps));
                    Assert.That(ApronOrbit.WorldToScreen(z, anchor, aspect, out var x, out var y), Is.True);
                    Assert.That(x, Is.EqualTo(nx).Within(1e-9), "anchor moved on screen (x)");
                    Assert.That(y, Is.EqualTo(ny).Within(1e-9), "anchor moved on screen (y)");
                    Assert.That(ApronOrbit.ScreenToGround(z, nx, ny, aspect, out var again), Is.True);
                    Near(again, anchor, 1e-7);
                }
            }
        }

        [Test]
        public void ScreenCentre_HitsThePivot()
        {
            var s = ApronOrbit.Start;
            s.Pivot = new Vec3(40, 0, 25);
            Assert.That(ApronOrbit.ScreenToGround(s, 0, 0, 1.5, out var hit), Is.True);
            Near(hit, s.Pivot, 1e-7);
        }

        [Test]
        public void DragOrbit_UsesTheWebRates()
        {
            var s = ApronOrbit.DragOrbit(ApronOrbit.Start, 100, 20);   // right 100 px, down 20 px
            Assert.That(s.Yaw, Is.EqualTo(0.72 - 0.7).Within(Eps));
            Assert.That(s.Pitch, Is.EqualTo(0.68 - 0.1).Within(Eps));
        }

        [Test]
        public void DragPan_MovesByMinusDxDyTimes008TimesScale()
        {
            var s = ApronOrbit.Start;                                   // scale 1
            var p = ApronOrbit.DragPan(s, 50, 0);
            Near(p.Pivot, ApronOrbit.GroundRight(s.Yaw) * -4);          // -50 * 0.08
            p = ApronOrbit.DragPan(s, 0, 25);
            Near(p.Pivot, ApronOrbit.GroundForward(s.Yaw) * 2);         // 25 * 0.08
            s.Dist = 12;                                                // scale 0.35
            p = ApronOrbit.DragPan(s, 100, 0);
            Near(p.Pivot, ApronOrbit.GroundRight(s.Yaw) * (-100 * 0.08 * 0.35));
        }
    }
}
