// The apron camera, copied from the web game: an orbit camera around a pivot on the ground. All the math is in
// ApronOrbit (Math/ApronOrbit.cs, pure C#, web units); this component only reads input and places the Camera.
//
// Input goes through a rebindable Input System action map ("ApronCamera", built in code below). Rebind it with the
// usual Input System calls (PerformInteractiveRebinding, ApplyBindingOverride) and keep the result with
// SaveBindingOverrides() / LoadBindingOverrides(). No rebinding UI here.
//
// Mouse wheel: the web zooms by deltaY * 0.06, where a browser wheel notch is deltaY ~ 100 (down = positive).
// Unity's scroll y is up = positive, and one notch is 1 with the Input System's default "uniform" scroll setting
// (or 120 on Windows with "keep platform-specific range"). BrowserDeltaY() turns one notch into deltaY = 100 either way.
//
// Mouse drags use Unity screen pixels (physical pixels). The browser counts CSS pixels, so on a display scaled above
// 100 % the web turns slower per physical pixel; PixelScale lets that be matched (1 = no change).
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hatchfield.CameraRig
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class ApronCameraRig : MonoBehaviour
    {
        [Tooltip("The camera speed setting (web: 0.5 - 4, default 2). Scales keyboard panning.")]
        [Range(0.5f, 4f)] public float CameraSpeed = (float)ApronOrbit.DefaultCameraSpeed;

        [Tooltip("Select mode: a left drag pans the camera after 8 px; a shorter left press is a click.")]
        public bool SelectMode = true;

        [Tooltip("Multiplies mouse pixel deltas before they reach the web numbers (1 = Unity pixels as-is).")]
        public float PixelScale = 1;

        [Tooltip("Read the mouse and keyboard every frame. Off: drive State from code and call Apply().")]
        public bool ReadInput = true;

        /// <summary>A left press-and-release that moved under 8 px (select mode). Unity screen position (y up).</summary>
        public event Action<Vector2> Clicked;

        /// <summary>The rig's state in web units (see ApronOrbit). Change it, then call Apply().</summary>
        public OrbitState State = ApronOrbit.Start;

        public InputActionMap Actions { get { Init(); return map; } }

        Camera cam;
        InputActionMap map;
        InputAction pan, rotate, keyZoom, wheel, point, orbitButton, selectButton;
        Vector2 lastPointer, pressPointer;
        bool leftDown, leftPanning, orbiting, hasPointer;

        void Awake() { Init(); Apply(); }
        void OnEnable() { Init(); map.Enable(); }
        void OnDisable() { map?.Disable(); }
        void OnDestroy() { map?.Dispose(); map = null; }
        void Update() { if (ReadInput) Tick(Time.unscaledDeltaTime); }

        void Init()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (map != null) return;
            map = new InputActionMap("ApronCamera");
            pan = map.AddAction("Pan", InputActionType.Value, expectedControlLayout: "Vector2");
            pan.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            pan.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            rotate = map.AddAction("Rotate", InputActionType.Value, expectedControlLayout: "Axis");
            rotate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            keyZoom = map.AddAction("ZoomKeys", InputActionType.Value, expectedControlLayout: "Axis");
            keyZoom.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/minus").With("Positive", "<Keyboard>/equals");
            wheel = map.AddAction("Wheel", InputActionType.PassThrough, "<Mouse>/scroll/y", expectedControlLayout: "Axis");
            point = map.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            orbitButton = map.AddAction("Orbit", InputActionType.Button, "<Mouse>/rightButton");
            orbitButton.AddBinding("<Mouse>/middleButton");
            selectButton = map.AddAction("Select", InputActionType.Button, "<Mouse>/leftButton");
            if (isActiveAndEnabled) map.Enable();
        }

        public string SaveBindingOverrides() => Actions.SaveBindingOverridesAsJson();
        public void LoadBindingOverrides(string json) { Actions.LoadBindingOverridesFromJson(json); }

        /// <summary>One frame of input. Called from Update; tests and tools may call it directly.</summary>
        public void Tick(float dt)
        {
            Init();
            if (!map.enabled) map.Enable();
            var s = State;

            // Keyboard: held keys, sampled every frame, ignored while Ctrl / Alt / Meta is held.
            var kb = Keyboard.current;
            var modifier = kb != null && (kb.ctrlKey.isPressed || kb.altKey.isPressed || kb.leftMetaKey.isPressed || kb.rightMetaKey.isPressed);
            if (!modifier)
            {
                var move = pan.ReadValue<Vector2>();
                if (move != Vector2.zero) s = ApronOrbit.KeyPan(s, move.x, move.y, CameraSpeed, dt);
                var r = rotate.ReadValue<float>();
                if (r != 0) s = ApronOrbit.KeyRotate(s, r, dt);
                var z = keyZoom.ReadValue<float>();
                if (z != 0) s = ApronOrbit.KeyZoom(s, z, dt);
            }

            // Mouse.
            var pos = PointerPosition();
            if (!hasPointer) { lastPointer = pos; hasPointer = true; }
            var d = (pos - lastPointer) * PixelScale;
            double dx = d.x, dy = -d.y;                 // browser convention: y down is positive

            var scroll = wheel.ReadValue<float>();
            if (scroll != 0)
            {
                var deltaDist = ApronOrbit.WheelZoomDelta(BrowserDeltaY(scroll));
                s = ScreenToGround(s, pos, out var anchor) ? ApronOrbit.ZoomToward(s, deltaDist, anchor) : ApronOrbit.ZoomToward(s, deltaDist, s.Pivot);
            }

            if (orbitButton.WasPressedThisFrame()) orbiting = true;
            else if (orbiting && orbitButton.IsPressed()) s = ApronOrbit.DragOrbit(s, dx, dy);
            if (!orbitButton.IsPressed()) orbiting = false;

            if (SelectMode)
            {
                if (selectButton.WasPressedThisFrame()) { leftDown = true; leftPanning = false; pressPointer = pos; }
                else if (leftDown && selectButton.IsPressed())
                {
                    if (!leftPanning && ((pos - pressPointer) * PixelScale).magnitude > ApronOrbit.ClickSlopPx) leftPanning = true;
                    if (leftPanning) s = ApronOrbit.DragPan(s, dx, dy);
                }
                if (leftDown && selectButton.WasReleasedThisFrame())
                {
                    if (!leftPanning) Clicked?.Invoke(pos);
                    leftDown = leftPanning = false;
                }
            }
            else leftDown = leftPanning = false;

            lastPointer = pos;
            State = s;
            Apply();
        }

        // The pointer's current position, read from the bound control itself (a pass-through action only knows a value
        // after the pointer has moved since the map was enabled).
        Vector2 PointerPosition()
        {
            foreach (var c in point.controls)
                if (c is InputControl<Vector2> v2) return v2.ReadValue();
            return point.ReadValue<Vector2>();
        }

        /// <summary>One Unity scroll reading as a browser wheel deltaY (a notch = 100, scrolling down positive).</summary>
        public static double BrowserDeltaY(float unityScrollY)
        {
            var perNotch = 1.0;
            if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor))
                perNotch = 120;
            return -unityScrollY / perNotch * 100;
        }

        bool ScreenToGround(in OrbitState s, Vector2 screen, out Vec3 hit)
        {
            var rect = cam.pixelRect;
            var ndcX = (screen.x - rect.x) / rect.width * 2 - 1;
            var ndcY = (screen.y - rect.y) / rect.height * 2 - 1;
            return ApronOrbit.ScreenToGround(s, ndcX, ndcY, cam.aspect, out hit);
        }

        /// <summary>Places the camera from State (clamped): eye from the orbit formula, looking at the pivot.</summary>
        public void Apply()
        {
            Init();
            State = ApronOrbit.Clamped(State);
            var k = (float)ApronOrbit.WebUnitsToMetres;
            cam.fieldOfView = (float)ApronOrbit.FovDeg;
            cam.nearClipPlane = (float)ApronOrbit.Near * k;
            cam.farClipPlane = (float)ApronOrbit.Far * k;
            var eye = ToUnity(ApronOrbit.ToUnityMetres(ApronOrbit.Eye(State)));
            var pivot = ToUnity(ApronOrbit.ToUnityMetres(State.Pivot));
            transform.SetPositionAndRotation(eye, Quaternion.LookRotation(pivot - eye, Vector3.up));
        }

        public static Vector3 ToUnity(Vec3 v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
    }
}
