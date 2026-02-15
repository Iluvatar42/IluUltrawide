using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace IluUltrawide
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Plugin Instance;

        public static ConfigEntry<bool>  EnableUltrawide;
        public static ConfigEntry<int>   TargetWidth;
        public static ConfigEntry<int>   TargetHeight;
        public static ConfigEntry<bool>  Fullscreen;
        public static ConfigEntry<float> UiScaleMultiplier;
        public static ConfigEntry<bool>  ConstrainUiTo169;
        public static ConfigEntry<bool>  RemoveBlackBars;
        public static ConfigEntry<KeyCode> GuiToggleKey;

        private static Harmony _harmony;
        internal const float BaseAspect = 16f / 9f;
        private static bool _runnerCreated = false;

        private void Awake()
        {
            Instance = this;
            Logger   = base.Logger;

            BindConfig();

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Patches));

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
            Logger.LogInfo($"Screen: {Screen.width}x{Screen.height}  Aspect: {(float)Screen.width / Screen.height:F4}");

            if (EnableUltrawide.Value)
                ApplyResolution();

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Logger.LogInfo("SceneLoaded event hooked.");
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Logger.LogInfo($"OnSceneLoaded: '{scene.name}'");
            if (!_runnerCreated)
            {
                _runnerCreated = true;
                GameObject runner = new GameObject("IluUltrawide_Runner");
                DontDestroyOnLoad(runner);
                runner.AddComponent<UltrawideRunner>();
                Logger.LogInfo("Runner created on first scene load.");
            }
        }

        private void BindConfig()
        {
            EnableUltrawide = Config.Bind("1 - Resolution", "EnableUltrawide", true,
                "Master switch. When enabled the plugin applies all fixes.");
            TargetWidth = Config.Bind("1 - Resolution", "TargetWidth", 3440,
                new ConfigDescription("Horizontal resolution. 3440 = 21:9, 5120 = 32:9.",
                    new AcceptableValueRange<int>(1280, 7680)));
            TargetHeight = Config.Bind("1 - Resolution", "TargetHeight", 1440,
                new ConfigDescription("Vertical resolution.",
                    new AcceptableValueRange<int>(720, 4320)));
            Fullscreen = Config.Bind("1 - Resolution", "Fullscreen", true,
                "Run in fullscreen (borderless window). Set false for windowed.");
            UiScaleMultiplier = Config.Bind("2 - UI Scale", "UiScaleMultiplier", 1.0f,
                new ConfigDescription("Manual UI scale multiplier. 0.8 = smaller, 1.2 = larger.",
                    new AcceptableValueRange<float>(0.25f, 4.0f)));
            ConstrainUiTo169 = Config.Bind("2 - UI Scale", "ConstrainUiTo169", false,
                "Constrain UI to a 16:9 region in the centre of the screen.");
            RemoveBlackBars = Config.Bind("3 - Misc", "RemoveBlackBars", true,
                "Remove pillarbox / letterbox black bars.");
            GuiToggleKey = Config.Bind("4 - GUI", "ToggleKey", KeyCode.F10,
                "Key to open/close the in-game settings overlay.");
        }

        // ── Static helpers ─────────────────────────────────────────────────────

        internal static void ApplyResolution()
        {
            if (!EnableUltrawide.Value) return;
            int w = TargetWidth.Value;
            int h = TargetHeight.Value;
            FullScreenMode mode = Fullscreen.Value
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Screen.SetResolution(w, h, mode);
            Logger.LogInfo($"Resolution set to {w}x{h} ({mode})");
        }

        internal static void ApplyUiScale()
        {
            CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>();
            Logger.LogInfo($"ApplyUiScale — found {(scalers == null ? 0 : scalers.Length)} CanvasScaler(s), Screen={Screen.width}x{Screen.height}");
            if (scalers == null || scalers.Length == 0) return;

            float currentAspect = (float)Screen.width / Screen.height;
            float autoScale     = ConstrainUiTo169.Value ? 1f : currentAspect / BaseAspect;
            float finalScale    = autoScale * UiScaleMultiplier.Value;
            Logger.LogInfo($"  scaleFactor={finalScale:F3} (aspect={currentAspect:F3}, multiplier={UiScaleMultiplier.Value})");

            foreach (CanvasScaler cs in scalers)
            {
                if (cs == null) continue;
                cs.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                cs.scaleFactor = finalScale;
                Logger.LogInfo($"  -> '{cs.name}' scaleFactor={finalScale:F3}");
            }
        }

        internal static void RemoveCameraBlackBars()
        {
            if (!RemoveBlackBars.Value) return;
            Camera[] cams = FindObjectsOfType<Camera>();
            foreach (Camera cam in cams)
            {
                if (cam == null) continue;
                if (cam.rect != new Rect(0, 0, 1, 1))
                {
                    cam.rect = new Rect(0, 0, 1, 1);
                    Logger.LogInfo($"Camera '{cam.name}' viewport reset.");
                }
            }
        }

        internal static void ApplyAll()
        {
            if (!EnableUltrawide.Value) return;
            ApplyResolution();
            RemoveCameraBlackBars();
            ApplyUiScale();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Runner MonoBehaviour + IMGUI overlay
    // ══════════════════════════════════════════════════════════════════════════

    public class UltrawideRunner : MonoBehaviour
    {
        private string _lastScene       = string.Empty;
        private float  _sceneChangeTime = -1f;
        private float  _startupTimer    = 0f;
        private bool   _startup01Done   = false;
        private bool   _startup1Done    = false;
        private bool   _startup3Done    = false;

        // ── GUI state ──────────────────────────────────────────────────────
        private bool    _showGui     = false;
        private Rect    _windowRect  = new Rect(10, 10, 380, 550);
        private Vector2 _scrollPos   = Vector2.zero;
        private bool    _resizing    = false;
        private Vector2 _resizeStart = Vector2.zero;

        // Local copies of config values for the sliders
        private float _uiScaleVal;
        private int   _resWidth;
        private int   _resHeight;

        // Text input strings
        private string _uiScaleText = "";
        private string _resWidthText = "";
        private string _resHeightText = "";

        // Resolution presets
        private static readonly (int w, int h, string label)[] ResPresets = new[]
        {
            (1920,  1080, "1920x1080  (16:9)"),
            (2560,  1440, "2560x1440  (16:9)"),
            (2560,  1080, "2560x1080  (21:9)"),
            (3440,  1440, "3440x1440  (21:9)"),
            (3840,  1080, "3840x1080  (32:9)"),
            (5120,  1440, "5120x1440  (32:9)"),
            (3840,  2160, "3840x2160  (4K)"),
        };

        private void Start()
        {
            Plugin.Logger.LogInfo("UltrawideRunner Start() called — Update loop is active.");
            SyncFromConfig();
        }

        private void SyncFromConfig()
        {
            _uiScaleVal = Plugin.UiScaleMultiplier.Value;
            _resWidth   = Plugin.TargetWidth.Value;
            _resHeight  = Plugin.TargetHeight.Value;

            _uiScaleText = _uiScaleVal.ToString("F2");
            _resWidthText = _resWidth.ToString();
            _resHeightText = _resHeight.ToString();
        }

        private void Update()
        {
            if (!Plugin.EnableUltrawide.Value) return;

            // ── Toggle GUI ─────────────────────────────────────────────────
            if (Input.GetKeyDown(Plugin.GuiToggleKey.Value))
            {
                _showGui = !_showGui;
                if (_showGui) SyncFromConfig();
                Plugin.Logger.LogInfo($"GUI toggled: {_showGui}");
            }

            // ── Handle resizing ────────────────────────────────────────────
            if (_showGui && _resizing)
            {
                Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                _windowRect.width  = Mathf.Max(320, mousePos.x - _windowRect.x);
                _windowRect.height = Mathf.Max(400, mousePos.y - _windowRect.y);

                if (Input.GetMouseButtonUp(0))
                {
                    _resizing = false;
                }
            }

            // ── Startup passes ─────────────────────────────────────────────
            _startupTimer += Time.unscaledDeltaTime;
            if (!_startup01Done && _startupTimer >= 0.1f) { _startup01Done = true; Plugin.ApplyUiScale(); }
            if (!_startup1Done  && _startupTimer >= 1f)   { _startup1Done  = true; Plugin.ApplyAll(); }
            if (!_startup3Done  && _startupTimer >= 3f)   { _startup3Done  = true; Plugin.ApplyAll(); }

            // ── Scene change detection ─────────────────────────────────────
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != _lastScene)
            {
                Plugin.Logger.LogInfo($"Scene changed: '{_lastScene}' -> '{currentScene}'");
                _lastScene       = currentScene;
                _sceneChangeTime = Time.unscaledTime;
            }

            if (_sceneChangeTime > 0f)
            {
                float elapsed = Time.unscaledTime - _sceneChangeTime;
                if (elapsed >= 3f)
                {
                    Plugin.Logger.LogInfo("Scene +3s — ApplyAll");
                    Plugin.ApplyAll();
                    _sceneChangeTime = -1f;
                }
                else if (elapsed >= 1f && elapsed < 2f)
                {
                    Plugin.Logger.LogInfo("Scene +1s — ApplyAll");
                    Plugin.ApplyAll();
                    _sceneChangeTime = 999999f;
                }
            }
        }

        private void OnGUI()
        {
            if (!_showGui) return;
            
            _windowRect = GUI.Window(0, _windowRect, DrawWindow, "IluUltrawide Settings");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4);

            // Start scroll view
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(_windowRect.width - 20), GUILayout.Height(_windowRect.height - 60));

            // ── Current resolution info ────────────────────────────────────
            GUILayout.Label($"Current Resolution:  {Screen.width} x {Screen.height}");
            GUILayout.Label($"Aspect Ratio:  {((float)Screen.width / Screen.height):F3}");

            GUILayout.Space(8);
            GUILayout.Label("── Resolution Presets ──────────────────");

            foreach (var (w, h, label) in ResPresets)
            {
                bool isCurrent = (Screen.width == w && Screen.height == h);
                string btnLabel = isCurrent ? $"► {label}" : $"   {label}";
                if (GUILayout.Button(btnLabel))
                {
                    Plugin.TargetWidth.Value  = w;
                    Plugin.TargetHeight.Value = h;
                    _resWidth  = w;
                    _resHeight = h;
                    Plugin.ApplyResolution();
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Custom Resolution:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("W:", GUILayout.Width(20));
            _resWidthText = GUILayout.TextField(_resWidthText, GUILayout.Width(60));
            GUILayout.Label("H:", GUILayout.Width(20));
            _resHeightText = GUILayout.TextField(_resHeightText, GUILayout.Width(60));
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                if (int.TryParse(_resWidthText, out int w) && int.TryParse(_resHeightText, out int h))
                {
                    w = Mathf.Clamp(w, 1280, 7680);
                    h = Mathf.Clamp(h, 720, 4320);
                    Plugin.TargetWidth.Value = w;
                    Plugin.TargetHeight.Value = h;
                    _resWidth = w;
                    _resHeight = h;
                    _resWidthText = w.ToString();
                    _resHeightText = h.ToString();
                    Plugin.ApplyResolution();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("── UI Scale ────────────────────────────");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Multiplier:", GUILayout.Width(70));
            _uiScaleText = GUILayout.TextField(_uiScaleText, GUILayout.Width(60));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (float.TryParse(_uiScaleText, out float val))
                {
                    val = Mathf.Clamp(val, 0.25f, 2.5f);
                    _uiScaleVal = val;
                    _uiScaleText = val.ToString("F2");
                    Plugin.UiScaleMultiplier.Value = _uiScaleVal;
                    Plugin.ApplyUiScale();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Value: {_uiScaleVal:F2}");
            float newUiScale = GUILayout.HorizontalSlider(_uiScaleVal, 0.25f, 2.5f);
            if (!Mathf.Approximately(newUiScale, _uiScaleVal))
            {
                _uiScaleVal = Mathf.Round(newUiScale * 100f) / 100f;
                _uiScaleText = _uiScaleVal.ToString("F2");
                Plugin.UiScaleMultiplier.Value = _uiScaleVal;
                Plugin.ApplyUiScale();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-0.1"))  { _uiScaleVal = Mathf.Max(0.25f, _uiScaleVal - 0.1f); _uiScaleText = _uiScaleVal.ToString("F2"); Plugin.UiScaleMultiplier.Value = _uiScaleVal; Plugin.ApplyUiScale(); }
            if (GUILayout.Button("Reset")) { _uiScaleVal = 1.0f; _uiScaleText = _uiScaleVal.ToString("F2"); Plugin.UiScaleMultiplier.Value = _uiScaleVal; Plugin.ApplyUiScale(); }
            if (GUILayout.Button("+0.1"))  { _uiScaleVal = Mathf.Min(2.5f,  _uiScaleVal + 0.1f); _uiScaleText = _uiScaleVal.ToString("F2"); Plugin.UiScaleMultiplier.Value = _uiScaleVal; Plugin.ApplyUiScale(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("Force Apply All Settings Now"))
            {
                Plugin.ApplyAll();
            }

            GUILayout.Space(12);
            GUILayout.Label($"Press {Plugin.GuiToggleKey.Value} to close  •  Drag corner to resize");

            GUILayout.EndScrollView();

            // Resize handle in bottom-right corner (inside window coords)
            Rect resizeHandle = new Rect(_windowRect.width - 25, _windowRect.height - 25, 20, 20);
            GUI.Box(resizeHandle, "◢");
            
            if (Event.current.type == EventType.MouseDown && resizeHandle.Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Harmony Patches
    // ══════════════════════════════════════════════════════════════════════════

    public static class Patches
    {
        [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution),
            new[] { typeof(int), typeof(int), typeof(FullScreenMode), typeof(int) })]
        [HarmonyPrefix]
        public static bool Screen_SetResolution_Prefix(ref int width, ref int height,
            ref FullScreenMode fullscreenMode, ref int preferredRefreshRate)
        {
            if (!Plugin.EnableUltrawide.Value) return true;
            width          = Plugin.TargetWidth.Value;
            height         = Plugin.TargetHeight.Value;
            fullscreenMode = Plugin.Fullscreen.Value
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            return true;
        }

        [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution),
            new[] { typeof(int), typeof(int), typeof(bool) })]
        [HarmonyPrefix]
        public static bool Screen_SetResolution_Bool_Prefix(ref int width, ref int height,
            ref bool fullscreen)
        {
            if (!Plugin.EnableUltrawide.Value) return true;
            width      = Plugin.TargetWidth.Value;
            height     = Plugin.TargetHeight.Value;
            fullscreen = Plugin.Fullscreen.Value;
            return true;
        }

        [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
        [HarmonyPostfix]
        public static void CanvasScaler_OnEnable_Postfix(CanvasScaler __instance)
        {
            if (!Plugin.EnableUltrawide.Value) return;
            float currentAspect = (float)Screen.width / Screen.height;
            float autoScale     = Plugin.ConstrainUiTo169.Value ? 1f : currentAspect / (16f / 9f);
            __instance.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            __instance.scaleFactor = autoScale * Plugin.UiScaleMultiplier.Value;
        }

        [HarmonyPatch(typeof(AspectRatioFitter), "OnEnable")]
        [HarmonyPostfix]
        public static void AspectRatioFitter_OnEnable_Postfix(AspectRatioFitter __instance)
        {
            if (!Plugin.EnableUltrawide.Value) return;
            if (Plugin.ConstrainUiTo169.Value) return;
            float currentAspect = (float)Screen.width / Screen.height;
            __instance.aspectRatio = currentAspect;
        }
    }
}
