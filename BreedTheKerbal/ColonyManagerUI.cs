using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace BreedTheKerbal
{
    // Two thin wrappers so the window is available in both scenes.
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal sealed class ColonyManagerUI_Flight : ColonyManagerUI { }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    internal sealed class ColonyManagerUI_SpaceCentre : ColonyManagerUI { }

    /// <summary>
    /// IMGUI Colony Manager window.
    /// Shows life-stage progress bars for every tracked Kerbal and lets the
    /// player manually select breeding pairs per vessel.
    /// </summary>
    public class ColonyManagerUI : MonoBehaviour
    {
        // Shared across Flight / SpaceCentre instance
        private static bool _visible;

        private Rect       _windowRect = new Rect(Screen.width * 0.5f - 270f, 80f, 540f, 600f);
        private Vector2    _scrollPos;
        private ApplicationLauncherButton _appBtn;

        // Per-vessel pairing selection
        private readonly Dictionary<Guid, PairSelection> _selection =
            new Dictionary<Guid, PairSelection>();

        // 1×1 colour textures keyed by ARGB int to avoid per-frame allocations
        private readonly Dictionary<int, Texture2D> _texCache =
            new Dictionary<int, Texture2D>();

        // GUIStyles (created once after skin is available)
        private bool     _stylesReady;
        private GUIStyle _winStyle;
        private GUIStyle _barBg;
        private GUIStyle _bold;
        private GUIStyle _small;
        private GUIStyle _vesselHdr;
        private GUIStyle _warnStyle;
        private GUIStyle _effWarnStyle;
        private GUIStyle _effCritStyle;

        // Stable tooltip state — committed at end of each Repaint pass so
        // Layout and Repaint always agree on how many BeginArea groups to open.
        private ProtoCrewMember _tooltipKerbal;
        private bool            _lowSupportHovered;
        // Pending state built during Repaint, promoted to stable at frame end
        private ProtoCrewMember _nextTooltipKerbal;
        private bool            _nextLowSupportHovered;
        private Vector2         _tooltipPos;
        private GUIStyle        _tooltipBox;
        private GUIStyle        _tooltipName;
        private GUIStyle        _portStyle;

        private struct PairSelection
        {
            public string Male;
            public string Female;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnLauncherReady);
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnLauncherReady);
            if (_appBtn != null && ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(_appBtn);
        }

        // ── Toolbar button ────────────────────────────────────────────────────

        private void OnLauncherReady()
        {
            if (_appBtn != null) return;
            _appBtn = ApplicationLauncher.Instance.AddModApplication(
                () => _visible = true,
                () => _visible = false,
                null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER,
                BuildIcon());
        }

        private static Texture2D BuildIcon()
        {
            var t = new Texture2D(38, 38, TextureFormat.ARGB32, false);
            for (int y = 0; y < 38; y++)
            for (int x = 0; x < 38; x++)
            {
                float cx = x - 19f, cy = y - 19f;
                t.SetPixel(x, y, cx * cx + cy * cy < 18f * 18f
                    ? new Color(0.95f, 0.45f, 0.65f)
                    : Color.clear);
            }
            t.Apply();
            return t;
        }

        // ── OnGUI entry point ─────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            _tooltipPos = Event.current.mousePosition;

            // Begin building next hover state only during Repaint
            if (Event.current.type == EventType.Repaint)
            {
                _nextTooltipKerbal     = null;
                _nextLowSupportHovered = false;
            }

            _windowRect = GUILayout.Window(
                GetInstanceID(), _windowRect, DrawWindow,
                "Colony Manager",
                _winStyle,
                GUILayout.Width(540), GUILayout.MinHeight(120));

            // Use STABLE state so Layout and Repaint open the same number of groups
            if (_tooltipKerbal != null && BreedingScenario.Instance != null)
                DrawTooltip(BreedingScenario.Instance, _tooltipKerbal);

            if (_lowSupportHovered && BreedingScenario.Instance != null)
                DrawLowSupportTooltip(BreedingScenario.Instance);

            // Promote pending state to stable at end of each Repaint pass
            if (Event.current.type == EventType.Repaint)
            {
                _tooltipKerbal     = _nextTooltipKerbal;
                _lowSupportHovered = _nextLowSupportHovered;
            }
        }

        // ── Main window ───────────────────────────────────────────────────────

        private void DrawWindow(int _)
        {
            BreedingScenario sc = BreedingScenario.Instance;
            if (sc == null)
            {
                GUILayout.Label("Scenario not loaded yet.");
                GUI.DragWindow();
                return;
            }

            DrawStatusHeader(sc);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(530));

            if (FlightGlobals.Vessels != null)
            {
                // Active/loaded vessels first
                foreach (Vessel v in FlightGlobals.Vessels
                    .OrderByDescending(x => x.loaded)
                    .ThenBy(x => x.vesselName))
                {
                    if (v.GetVesselCrew().Count == 0) continue;
                    DrawVesselBlock(sc, v);
                }
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close")) _visible = false;
            GUI.DragWindow(new Rect(0, 0, 10000, 26));
        }

        // ── Status header ─────────────────────────────────────────────────────

        private void DrawStatusHeader(BreedingScenario sc)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);

            bool career = HighLogic.CurrentGame?.Mode == Game.Modes.CAREER;
            GUILayout.Label(career ? "Career" : "Non-Career", _small, GUILayout.Width(70));

            if (career)
                GUILayout.Label($"Daily upkeep:  {sc.GetDailyUpkeepCost():N0} funds/day", _small);

            GUILayout.FlexibleSpace();

            if (sc.IsLowSupport)
            {
                GUILayout.Label("[ LOW SUPPORT ]", _warnStyle);
                if (Event.current.type == EventType.Repaint
                    && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    _nextLowSupportHovered = true;
            }

            GUILayout.EndHorizontal();
        }

        // ── Vessel block ──────────────────────────────────────────────────────

        private void DrawVesselBlock(BreedingScenario sc, Vessel v)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            string tag = v.loaded ? "(active)" : "(unloaded)";
            GUILayout.Label($"  {v.vesselName}  {tag}", _vesselHdr);

            GUILayout.Space(2);

            foreach (ProtoCrewMember k in v.GetVesselCrew())
                DrawKerbalRow(sc, k);

            GUILayout.Space(4);

            if (v.loaded)
                DrawPairingPanel(sc, v);
            else
                GUILayout.Label("  Vessel must be active to manage breeding.", _small);

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        // ── Kerbal row with progress bar ──────────────────────────────────────

        private void DrawKerbalRow(BreedingScenario sc, ProtoCrewMember k)
        {
            KerbalLifeData d    = sc.GetKerbalDataPublic(k.name);
            bool           preg = sc.IsPregnant(k);
            bool           post = sc.IsPostpartum(k);
            float          eff  = sc.GetEfficiencyMultiplier(k.name);
            LifeStage      stage = sc.GetLifeStage(k.name);

            // Determine label, progress, time remaining and bar colour
            string stageLabel = "[Adult]";
            float  progress   = 1f;
            double remaining  = 0.0;
            Color  barCol     = new Color(0.50f, 0.50f, 0.50f);

            if (preg && d != null)
            {
                stageLabel = "[Pregnant]";
                progress   = 1f - (float)(d.PregnancyTimer / BreedingConfig.PregnancyDuration);
                remaining  = d.PregnancyTimer;
                barCol     = new Color(1.00f, 0.55f, 0.75f);
            }
            else if (post && d != null)
            {
                stageLabel = "[Postpartum]";
                progress   = 1f - (float)(d.PostpartumTimer / BreedingConfig.PostpartumDuration);
                remaining  = d.PostpartumTimer;
                barCol     = new Color(0.75f, 0.55f, 1.00f);
            }
            else
            {
                switch (stage)
                {
                    case LifeStage.Newborn:
                        stageLabel = "[Newborn]";
                        progress   = d == null ? 0f : 1f - (float)(d.AgeTimer / BreedingConfig.NewbornDuration);
                        remaining  = d?.AgeTimer ?? 0;
                        barCol     = new Color(1.00f, 0.85f, 0.20f);
                        break;
                    case LifeStage.Child:
                        stageLabel = "[Child]";
                        progress   = d == null ? 0f : 1f - (float)(d.AgeTimer / BreedingConfig.ChildDuration);
                        remaining  = d?.AgeTimer ?? 0;
                        barCol     = new Color(0.25f, 0.85f, 1.00f);
                        break;
                    case LifeStage.Teenager:
                        stageLabel = "[Teenager]";
                        progress   = d == null ? 0f : 1f - (float)(d.AgeTimer / BreedingConfig.TeenagerDuration);
                        remaining  = d?.AgeTimer ?? 0;
                        barCol     = new Color(0.35f, 1.00f, 0.45f);
                        break;
                    default:
                        stageLabel = "[Adult]";
                        progress   = 1f;
                        remaining  = 0;
                        barCol     = new Color(0.50f, 0.50f, 0.50f);
                        break;
                }
            }

            GUILayout.BeginHorizontal();

            // Portrait placeholder — coloured by life-stage
            Rect portRect = GUILayoutUtility.GetRect(32f, 32f,
                GUILayout.Width(32), GUILayout.Height(32));
            DrawPortrait(portRect, k, barCol);

            // Name column — hover to show tooltip
            GUILayout.Label(k.name, GUILayout.Width(112));
            if (Event.current.type == EventType.Repaint
                && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                _nextTooltipKerbal = k;

            // Stage badge
            GUILayout.Label(stageLabel, GUILayout.Width(96));

            // Progress bar
            Rect barArea = GUILayoutUtility.GetRect(128f, 14f, GUILayout.Width(128));
            DrawBar(barArea, progress, barCol);

            // Efficiency %
            GUIStyle effStyle = eff >= 1.0f ? _small
                              : eff > 0.01f ? _effWarnStyle
                              : _effCritStyle;
            GUILayout.Label($" {eff * 100f:F0}%", effStyle, GUILayout.Width(40));

            // Time remaining
            bool hasTimer = (stage != LifeStage.Adult || preg || post) && remaining > 0;
            GUILayout.Label(hasTimer ? KTime(remaining) : "  --", _small, GUILayout.Width(58));

            GUILayout.EndHorizontal();
        }

        // ── Pairing panel ─────────────────────────────────────────────────────

        private void DrawPairingPanel(BreedingScenario sc, Vessel v)
        {
            bool hasLab = v.Parts?.Any(p =>
                p.FindModuleImplementing<ModuleScienceLab>() != null) == true;
            bool hasHab = v.Parts?.Any(p =>
                p.CrewCapacity >= BreedingConfig.HabitatMinCrewCapacity) == true;

            if (!hasLab)
            {
                GUILayout.Label("  \u26d4 Needs a Science Lab to breed.", _small);
                return;
            }
            if (!hasHab)
            {
                GUILayout.Label(
                    $"  \u26d4 Needs a habitat with \u2265{BreedingConfig.HabitatMinCrewCapacity} seats.",
                    _small);
                return;
            }

            int freeSeats = sc.GetVesselFreeSeats(v);
            if (freeSeats < 1)
            {
                GUILayout.Label(
                    "  \u26d4 Vessel full \u2014 free a berth for the newborn (each pregnancy reserves one seat).",
                    _small);
                return;
            }

            List<ProtoCrewMember> crew    = v.GetVesselCrew();
            List<ProtoCrewMember> males   = crew.Where(k =>
                k.gender == ProtoCrewMember.Gender.Male && sc.IsAdult(k)).ToList();
            List<ProtoCrewMember> females = crew.Where(k =>
                k.gender == ProtoCrewMember.Gender.Female && sc.IsAdult(k)
                && !sc.IsPregnant(k) && !sc.IsPostpartum(k)).ToList();

            if (males.Count == 0 && females.Count == 0)
            {
                GUILayout.Label("  No eligible adult crew available for pairing.", _small);
                return;
            }

            if (!_selection.ContainsKey(v.id))
                _selection[v.id] = new PairSelection();

            PairSelection sel = _selection[v.id];

            GUILayout.Space(2);
            GUILayout.Label($"  ── Pair up  ·  {freeSeats} berth(s) free ──", _small);
            GUILayout.BeginHorizontal();

            // ── Male selector ─────────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("\u2642  Select male", _small);
            if (males.Count == 0)
            {
                GUILayout.Label("  (no eligible males)", _small);
            }
            else
            {
                foreach (ProtoCrewMember m in males)
                {
                    bool was = sel.Male == m.name;
                    bool now = GUILayout.Toggle(was, m.name);
                    if (now != was)
                    {
                        sel.Male = now ? m.name : null;
                        _selection[v.id] = sel;
                    }
                }
            }
            GUILayout.EndVertical();

            // ── Female selector ───────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("\u2640  Select female", _small);
            if (females.Count == 0)
            {
                GUILayout.Label("  (none eligible — pregnant or postpartum)", _small);
            }
            else
            {
                foreach (ProtoCrewMember f in females)
                {
                    bool was = sel.Female == f.name;
                    bool now = GUILayout.Toggle(was, f.name);
                    if (now != was)
                    {
                        sel.Female = now ? f.name : null;
                        _selection[v.id] = sel;
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Re-read in case it changed
            sel = _selection[v.id];

            bool canBreed = sel.Male != null && sel.Female != null;
            string btnText = canBreed
                ? $"\u25b6  Start Pregnancy:  {sel.Male.Split(' ')[0]}  \u00d7  {sel.Female.Split(' ')[0]}"
                : "\u25b6  Select a pair above";

            GUI.enabled = canBreed;
            if (GUILayout.Button(btnText))
            {
                ProtoCrewMember male   = crew.FirstOrDefault(k => k.name == sel.Male);
                ProtoCrewMember female = crew.FirstOrDefault(k => k.name == sel.Female);
                if (sc.RequestBreeding(v, female, male))
                    _selection[v.id] = new PairSelection();   // clear after successful pairing
            }
            GUI.enabled = true;
        }

        // ── Progress bar ──────────────────────────────────────────────────────

        private void DrawBar(Rect r, float progress, Color fill)
        {
            // Background track
            GUI.Box(r, GUIContent.none, _barBg);

            if (progress <= 0f) return;

            // Filled portion
            Rect filled = new Rect(r.x + 1f, r.y + 1f,
                (r.width - 2f) * Mathf.Clamp01(progress), r.height - 2f);

            int key = fill.GetHashCode();
            if (!_texCache.TryGetValue(key, out Texture2D tex) || tex == null)
            {
                tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, fill);
                tex.Apply();
                _texCache[key] = tex;
            }
            GUI.DrawTexture(filled, tex);
        }

        // ── Portrait placeholder ──────────────────────────────────────────────

        private static Color StageColor(LifeStage stage, bool preg, bool post)
        {
            if (preg) return new Color(1.00f, 0.55f, 0.75f);
            if (post) return new Color(0.75f, 0.55f, 1.00f);
            switch (stage)
            {
                case LifeStage.Newborn:  return new Color(1.00f, 0.85f, 0.20f);
                case LifeStage.Child:    return new Color(0.25f, 0.85f, 1.00f);
                case LifeStage.Teenager: return new Color(0.35f, 1.00f, 0.45f);
                default:                 return new Color(0.50f, 0.50f, 0.50f);
            }
        }

        private void DrawPortrait(Rect r, ProtoCrewMember k, Color stageCol)
        {
            int key = stageCol.GetHashCode();
            if (!_texCache.TryGetValue(key, out Texture2D bg) || bg == null)
            {
                bg = new Texture2D(1, 1);
                bg.SetPixel(0, 0, stageCol);
                bg.Apply();
                _texCache[key] = bg;
            }
            GUI.DrawTexture(r, bg);
            string glyph = k.gender == ProtoCrewMember.Gender.Male ? "\u2642" : "\u2640";
            GUI.Label(r, glyph, _portStyle);
        }

        // ── Kerbal tooltip popup ──────────────────────────────────────────────

        private void DrawTooltip(BreedingScenario sc, ProtoCrewMember k)
        {
            float px = Mathf.Clamp(_tooltipPos.x + 16f, 0f, Screen.width  - 270f);
            float py = Mathf.Clamp(_tooltipPos.y - 10f, 0f, Screen.height - 220f);
            GUILayout.BeginArea(new Rect(px, py, 260f, 290f));
            GUILayout.BeginVertical(_tooltipBox);
            DrawTooltipContent(sc, k);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawTooltipContent(BreedingScenario sc, ProtoCrewMember k)
        {
            KerbalLifeData d       = sc.GetKerbalDataPublic(k.name);
            bool           preg    = sc.IsPregnant(k);
            bool           post    = sc.IsPostpartum(k);
            float          eff     = sc.GetEfficiencyMultiplier(k.name);
            LifeStage      stage   = sc.GetLifeStage(k.name);
            string         stars   = new string('\u2605', k.experienceLevel)
                                   + new string('\u2606', 5 - k.experienceLevel);
            Color          col     = StageColor(stage, preg, post);
            string         stageStr = preg ? "Pregnant" : post ? "Postpartum" : stage.ToString();

            // Portrait + name / trait block
            GUILayout.BeginHorizontal();
            Rect portRect = GUILayoutUtility.GetRect(56f, 56f,
                GUILayout.Width(56), GUILayout.Height(56));
            DrawPortrait(portRect, k, col);
            GUILayout.Space(6f);
            GUILayout.BeginVertical();
            GUILayout.Label(k.name, _tooltipName);
            GUILayout.Label($"{k.trait}  {stars}", _small);
            GUILayout.Label(stageStr, _small);
            GUILayout.Label($"Eff: {eff * 100f:F0}%", _small);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            if (preg && d != null)
            {
                if (!string.IsNullOrEmpty(d.PartnerName))
                    GUILayout.Label($"Partner:    {d.PartnerName}", _small);
                GUILayout.Label($"Birth in:   {KTime(d.PregnancyTimer)}", _small);
            }
            else if (post && d != null)
            {
                GUILayout.Label($"Recovery:   {KTime(d.PostpartumTimer)}", _small);
            }
            else if (stage != LifeStage.Adult && d != null && d.AgeTimer > 0)
            {
                GUILayout.Label($"Grows up:   {KTime(d.AgeTimer)}", _small);
            }

            GUILayout.Label($"Courage: {k.courage:F2}  Stupid: {k.stupidity:F2}", _small);

            // Opis aktywnego debuffa
            string debuff = DescribeDebuff(sc, d, stage, preg, post, k);
            if (!string.IsNullOrEmpty(debuff))
            {
                GUILayout.Space(4f);
                GUILayout.Label(debuff, _effWarnStyle);
            }

            // Licznik braku opiekuna — czas do śmierci
            if (d != null && d.NoCaretakerTimer > 0.0)
            {
                double deathLimit = stage == LifeStage.Newborn ? BreedingConfig.NewbornDeathTimer
                                  : stage == LifeStage.Child   ? BreedingConfig.ChildDeathTimer
                                  : -1.0;
                if (deathLimit > 0.0)
                    GUILayout.Label(
                        $"\u26a0 No caretaker!  Dies in: {KTime(deathLimit - d.NoCaretakerTimer)}",
                        _effCritStyle);
            }
        }

        // ── Low-support tooltip ───────────────────────────────────────────────

        private void DrawLowSupportTooltip(BreedingScenario sc)
        {
            float px = Mathf.Clamp(_tooltipPos.x + 16f, 0f, Screen.width  - 310f);
            float py = Mathf.Clamp(_tooltipPos.y - 10f, 0f, Screen.height - 200f);
            GUILayout.BeginArea(new Rect(px, py, 300f, 200f));
            GUILayout.BeginVertical(_tooltipBox);
            DrawLowSupportContent(sc);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawLowSupportContent(BreedingScenario sc)
        {
            GUILayout.Label("\u26a0  Low Support", _warnStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Not enough funds to cover daily child care.", _small);
            GUILayout.Label($"Daily cost:  {sc.GetDailyUpkeepCost():N0} funds / Kerbin day", _small);
            GUILayout.Space(4f);
            GUILayout.Label("Penalties while active:", _small);
            GUILayout.Label("  \u2022  Children:   efficiency \u00d7 0.80", _small);
            GUILayout.Label("  \u2022  Teenagers:  efficiency \u00d7 0.70", _small);
            GUILayout.Space(4f);
            GUILayout.Label("How to fix:", _small);
            GUILayout.Label("  \u2022  Complete contracts to earn funds", _small);
            GUILayout.Label("  \u2022  Wait for non-adults to grow up", _small);
        }

        // ── Time formatter ────────────────────────────────────────────────────

        private static string KTime(double s)
        {
            if (s <= 0) return "now";
            int d = (int)(s / 21600.0);
            int h = (int)(s % 21600.0 / 3600.0);
            int m = (int)(s % 3600.0  /   60.0);
            if (d > 0) return $"{d}d {h}h";
            if (h > 0) return $"{h}h {m}m";
            return $"{m}m";
        }

        private static string DescribeDebuff(BreedingScenario sc, KerbalLifeData d,
            LifeStage stage, bool preg, bool post, ProtoCrewMember k)
        {
            if (post)
                return "\u2193 Postpartum: EVA zablokowane, 0% wydajno\u015bci";
            if (preg && d != null && d.PregnancyTimer <= BreedingConfig.PregnancyDuration / 2.0)
                return "\u2193 P\u00f3\u017ana ci\u0105\u017ca: EVA zablokowane, 40% wydajno\u015bci, SAS pilota: poziom 1";
            switch (stage)
            {
                case LifeStage.Newborn:
                    return "\u2193 Noworodek: EVA zablokowane, 0% wydajno\u015bci";
                case LifeStage.Child:
                    return sc.IsLowSupport
                        ? "\u2193 Dziecko: EVA zablokowane, Low Support \u2192 20% wydajno\u015bci"
                        : "\u2193 Dziecko: EVA zablokowane, 25% wydajno\u015bci";
                case LifeStage.Teenager:
                {
                    float eff = sc.GetEfficiencyMultiplier(k.name);
                    if (eff <= BreedingConfig.NoCaretakerTeenagerEfficiency + 0.01f)
                        return "\u2193 Nastolatek: brak opiekuna \u2192 20% eff, EVA zablokowane";
                    return sc.IsLowSupport
                        ? "\u2193 Nastolatek: Low Support \u2192 35% wydajno\u015bci"
                        : "\u2193 Nastolatek: 50% wydajno\u015bci, 0 gwiazdek do\u015bwiadczenia";
                }
            }
            return string.Empty;
        }

        // ── Style initialisation (deferred until skin is available) ───────────

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _winStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(8, 8, 22, 8)
            };

            var trackTex = new Texture2D(1, 1);
            trackTex.SetPixel(0, 0, new Color(0.10f, 0.10f, 0.12f, 0.95f));
            trackTex.Apply();

            var zero = new RectOffset(0, 0, 0, 0);
            _barBg = new GUIStyle(GUI.skin.box)
            {
                padding = zero, margin = zero, overflow = zero
            };
            _barBg.normal.background = trackTex;

            _bold = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };

            _vesselHdr = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 12,
                normal    = { textColor = new Color(0.90f, 0.85f, 0.50f) }
            };

            _warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 10,
                normal    = { textColor = new Color(1.00f, 0.50f, 0.10f) }
            };

            var ttBg = new Texture2D(1, 1);
            ttBg.SetPixel(0, 0, new Color(0.06f, 0.06f, 0.09f, 0.97f));
            ttBg.Apply();
            _tooltipBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8)
            };
            _tooltipBox.normal.background   = ttBg;
            _tooltipBox.onNormal.background = ttBg;

            _tooltipName = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 11,
                normal    = { textColor = new Color(1.00f, 0.90f, 0.60f) }
            };

            _portStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };

            _effWarnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1.00f, 0.65f, 0.10f) }
            };

            _effCritStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1.00f, 0.25f, 0.25f) }
            };
        }
    }
}
