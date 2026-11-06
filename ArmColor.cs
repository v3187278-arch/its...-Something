using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ArmColor
{
    [BepInPlugin("local.armcolor", "Arm Color", "1.0.0")]
    public class ArmColorPlugin : BaseUnityPlugin
    {
        private ConfigEntry<string> _hex;
        private ConfigEntry<string> _filter;
        private ConfigEntry<bool> _logPaths;

        private float _nextCheck;
        private readonly HashSet<int> _logged = new HashSet<int>();

        private void Awake()
        {
            // Stops the game from destroying the plugin object on scene loads.
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            _hex = Config.Bind("General", "ArmColor", "#FF0000",
                "Arm color as hex, e.g. #FF0000 (red), #00FFFF (cyan), #FF00FF (pink).");

            _filter = Config.Bind("General", "NameFilter", "arm",
                "Only renderers whose name or parent names contain this text get recolored.");

            _logPaths = Config.Bind("Debug", "LogRendererPaths", false,
                "Set to true to print every skinned renderer under the player camera to the BepInEx log. " +
                "Use it to find the right NameFilter if the wrong thing (or nothing) changes color.");

            Logger.LogInfo("Arm Color loaded.");
        }

        private void Update()
        {
            // Re-apply once per second so it survives scene loads and arm swaps.
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;
            Apply();
        }

        private void Apply()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Color color;
            if (!ColorUtility.TryParseHtmlString(_hex.Value, out color)) return;

            string filter = (_filter.Value ?? "").ToLowerInvariant();

            foreach (SkinnedMeshRenderer r in cam.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string path = PathFrom(r.transform, cam.transform);

                if (_logPaths.Value && _logged.Add(r.GetInstanceID()))
                    Logger.LogInfo("Renderer: " + path);

                // "Armature" is a rig name, not an arm, so ignore it when matching.
                string match = path.ToLowerInvariant().Replace("armature", "");
                if (filter.Length > 0 && match.IndexOf(filter) < 0) continue;

                // .materials gives per-renderer instances, so other objects stay untouched.
                foreach (Material m in r.materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                }
            }
        }

        private static string PathFrom(Transform t, Transform stopAt)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null && p != stopAt; p = p.parent)
                sb.Insert(0, p.name + "/");
            return sb.ToString();
        }
    }
}
