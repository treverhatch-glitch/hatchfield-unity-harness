// EditMode tests for the rig's Input System bindings: fake keyboard and mouse (InputTestFixture), real action map.
using Hatchfield.CameraRig;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hatchfield.CameraRig.Tests
{
    public class ApronCameraRigInputTests : InputTestFixture
    {
        Keyboard kb;
        Mouse mouse;
        GameObject go;
        ApronCameraRig rig;
        RenderTexture rt;

        public override void Setup()
        {
            base.Setup();
            kb = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            go = new GameObject("rig test");
            var cam = go.AddComponent<Camera>();
            rt = new RenderTexture(800, 450, 0);
            cam.targetTexture = rt;                       // a known 800 x 450 screen
            rig = go.AddComponent<ApronCameraRig>();
            Set(mouse.position, new Vector2(400, 225));
            rig.Actions.Enable();
            rig.Tick(0);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rt);
            base.TearDown();
        }

        static void Near(Vec3 a, Vec3 b, double eps = 1e-4) => Assert.That((a - b).Length, Is.LessThan(eps), $"expected {b}, got {a}");

        [Test]
        public void HoldW_PansForward_10UnitsPerSecondAtDefaults()
        {
            Press(kb.wKey);
            rig.Tick(0.5f);
            Near(rig.State.Pivot, ApronOrbit.GroundForward(0.72) * 10);
        }

        [Test]
        public void ArrowRight_PansRight()
        {
            Press(kb.rightArrowKey);
            rig.Tick(0.25f);
            Near(rig.State.Pivot, ApronOrbit.GroundRight(0.72) * 5);
        }

        [Test]
        public void KeysAreIgnoredWhileCtrlIsHeld()
        {
            Press(kb.leftCtrlKey);
            Press(kb.wKey);
            Press(kb.eKey);
            rig.Tick(0.5f);
            Near(rig.State.Pivot, new Vec3(0, 0, 0));
            Assert.That(rig.State.Yaw, Is.EqualTo(0.72).Within(1e-9));
        }

        [Test]
        public void QAndE_RotateAt14RadPerSecond()
        {
            Press(kb.qKey);
            rig.Tick(0.5f);
            Assert.That(rig.State.Yaw, Is.EqualTo(0.72 - 0.7).Within(1e-6));
            Release(kb.qKey);
            Press(kb.eKey);
            rig.Tick(1f);
            Assert.That(rig.State.Yaw, Is.EqualTo(0.72 + 0.7).Within(1e-6));
        }

        [Test]
        public void EqualsAndMinus_Zoom48PerSecond()
        {
            Press(kb.equalsKey);
            rig.Tick(0.5f);
            Assert.That(rig.State.Dist, Is.EqualTo(64).Within(1e-4));
            Release(kb.equalsKey);
            Press(kb.minusKey);
            rig.Tick(1f);
            Assert.That(rig.State.Dist, Is.EqualTo(112).Within(1e-4));
        }

        [Test]
        public void WheelNotchAtScreenCentre_ZoomsOut6Units_AboutThePivot()
        {
            Set(mouse.scroll, new Vector2(0, -1));       // one notch toward the user (browser deltaY = +100)
            rig.Tick(0.016f);
            Assert.That(rig.State.Dist, Is.EqualTo(94).Within(1e-4));
            Near(rig.State.Pivot, new Vec3(0, 0, 0), 1e-3);
        }

        [Test]
        public void WheelOffCentre_KeepsTheGroundPointUnderTheCursor()
        {
            var cursor = new Vector2(650, 120);
            Set(mouse.position, cursor);
            rig.Tick(0);
            var before = rig.GetComponent<Camera>().ScreenPointToRay(cursor);
            var groundBefore = before.GetPoint(-before.origin.y / before.direction.y);
            Set(mouse.scroll, new Vector2(0, 3));        // three notches away: zoom in 18
            rig.Tick(0.016f);
            Assert.That(rig.State.Dist, Is.EqualTo(70).Within(1e-4));
            var cam = rig.GetComponent<Camera>();
            var after = cam.WorldToScreenPoint(groundBefore);
            Assert.That(after.x, Is.EqualTo(cursor.x).Within(0.05f));
            Assert.That(after.y, Is.EqualTo(cursor.y).Within(0.05f));
        }

        [Test]
        public void RightDrag_Orbits()
        {
            Press(mouse.rightButton);
            rig.Tick(0);
            Set(mouse.position, new Vector2(500, 205));  // 100 px right, 20 px down the screen (Unity y falls)
            rig.Tick(0.016f);
            Assert.That(rig.State.Yaw, Is.EqualTo(0.72 - 0.7).Within(1e-5));
            Assert.That(rig.State.Pitch, Is.EqualTo(0.68 - 20 * 0.005).Within(1e-5)); // browser dy = +20
        }

        [Test]
        public void MiddleDrag_AlsoOrbits()
        {
            Press(mouse.middleButton);
            rig.Tick(0);
            Set(mouse.position, new Vector2(350, 225));
            rig.Tick(0.016f);
            Assert.That(rig.State.Yaw, Is.EqualTo(0.72 + 50 * 0.007).Within(1e-5));
        }

        [Test]
        public void LeftPressUnder8Px_IsAClick_NotAPan()
        {
            Vector2? clicked = null;
            rig.Clicked += p => clicked = p;
            Press(mouse.leftButton);
            rig.Tick(0);
            Set(mouse.position, new Vector2(405, 220));  // ~7 px
            rig.Tick(0.016f);
            Release(mouse.leftButton);
            rig.Tick(0.016f);
            Assert.That(clicked.HasValue, Is.True);
            Near(rig.State.Pivot, new Vec3(0, 0, 0));
        }

        [Test]
        public void LeftDragOver8Px_Pans_AndIsNotAClick()
        {
            var clicks = 0;
            rig.Clicked += _ => clicks++;
            Press(mouse.leftButton);
            rig.Tick(0);
            Set(mouse.position, new Vector2(450, 225));  // 50 px right
            rig.Tick(0.016f);
            Release(mouse.leftButton);
            rig.Tick(0.016f);
            Assert.That(clicks, Is.EqualTo(0));
            Near(rig.State.Pivot, ApronOrbit.GroundRight(0.72) * (-50 * 0.08));
        }

        [Test]
        public void Rebinding_W_To_I_Works_AndSurvivesSaveLoad()
        {
            var pan = rig.Actions.FindAction("Pan");
            var up = pan.bindings.IndexOf(b => b.isPartOfComposite && string.Equals(b.name, "up", System.StringComparison.OrdinalIgnoreCase) && b.path == "<Keyboard>/w");
            Assert.That(up, Is.GreaterThanOrEqualTo(0));
            pan.ApplyBindingOverride(up, "<Keyboard>/i");
            var json = rig.SaveBindingOverrides();
            pan.RemoveAllBindingOverrides();
            rig.LoadBindingOverrides(json);
            Press(kb.iKey);
            rig.Tick(0.5f);
            Near(rig.State.Pivot, ApronOrbit.GroundForward(0.72) * 10);
            Release(kb.iKey);
            Press(kb.wKey);
            rig.Tick(0.5f);
            Near(rig.State.Pivot, ApronOrbit.GroundForward(0.72) * 10);   // W no longer pans
        }

        [Test]
        public void CameraIsPlacedFromTheOrbitFormula_InMetres()
        {
            var cam = rig.GetComponent<Camera>();
            Assert.That(cam.fieldOfView, Is.EqualTo(34).Within(1e-4));
            Assert.That(cam.nearClipPlane, Is.EqualTo(0.1f / 0.45f).Within(1e-4));
            Assert.That(cam.farClipPlane, Is.EqualTo(1200f / 0.45f).Within(1e-2));
            var eye = ApronCameraRig.ToUnity(ApronOrbit.ToUnityMetres(ApronOrbit.Eye(ApronOrbit.Start)));
            Assert.That(Vector3.Distance(rig.transform.position, eye), Is.LessThan(1e-3f));
            Assert.That(Vector3.Angle(rig.transform.forward, -eye), Is.LessThan(1e-2f));
        }
    }
}
