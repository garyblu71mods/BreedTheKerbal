using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BreedTheKerbal
{
    /// <summary>
    /// Central ScenarioModule for the Breed-The-Kerbal system.
    /// Handles pregnancy / birth timers, life-stage progression, caretaker absence,
    /// Science Lab efficiency modifiers, EVA restrictions and career-mode upkeep costs.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames,
        GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class BreedingScenario : ScenarioModule
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static BreedingScenario Instance { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────
        private readonly Dictionary<string, KerbalLifeData> _kerbalData =
            new Dictionary<string, KerbalLifeData>();

        private double _lastUpdateUT;
        private double _lastFundDeductUT;
        private bool   _isLowSupport;

        // Throttle the per-frame science-lab / harvester update to once per second
        private float  _labUpdateTimer;

        // Throttle the per-frame experience-level enforcement to once per 5 seconds
        private float  _expUpdateTimer;

        // Teenager 50 % XP: alternate keep / remove for each flight-log entry (per kerbal)
        private readonly Dictionary<string, bool> _teenXpAllow = new Dictionary<string, bool>();

        private const double KerbinDay         = 21600.0;
        private const float  LabUpdateInterval = 1f;
        private const float  ExpUpdateInterval = 5f;

        // ── ScenarioModule lifecycle ──────────────────────────────────────────

        public override void OnAwake()
        {
            Instance = this;
            double now = Planetarium.GetUniversalTime();
            _lastUpdateUT     = now;
            _lastFundDeductUT = now;

            GameEvents.onAttemptEva    .Add(OnAttemptEva);
            GameEvents.onCrewOnEva     .Add(OnCrewOnEva);
            GameEvents.onCrewTransferred.Add(OnCrewTransferred);
            GameEvents.OnFlightLogRecorded.Add(OnFlightLogRecorded);
        }

        private void OnDestroy()
        {
            GameEvents.onAttemptEva    .Remove(OnAttemptEva);
            GameEvents.onCrewOnEva     .Remove(OnCrewOnEva);
            GameEvents.onCrewTransferred.Remove(OnCrewTransferred);
            GameEvents.OnFlightLogRecorded.Remove(OnFlightLogRecorded);
            if (Instance == this) Instance = null;
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("lastUpdateUT",     _lastUpdateUT);
            node.AddValue("lastFundDeductUT", _lastFundDeductUT);
            node.AddValue("isLowSupport",     _isLowSupport);

            ConfigNode roster = node.AddNode("KERBAL_LIFE_DATA");
            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                ConfigNode kn = roster.AddNode("KERBAL");
                d.Save(kn);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            double savedUT;
            if (double.TryParse(node.GetValue("lastUpdateUT"), out savedUT) && savedUT > 0.0)
                _lastUpdateUT = savedUT;

            double savedFundUT;
            if (double.TryParse(node.GetValue("lastFundDeductUT"), out savedFundUT) && savedFundUT > 0.0)
                _lastFundDeductUT = savedFundUT;

            bool.TryParse(node.GetValue("isLowSupport"), out _isLowSupport);

            _kerbalData.Clear();
            ConfigNode roster = node.GetNode("KERBAL_LIFE_DATA");
            if (roster != null)
            {
                foreach (ConfigNode kn in roster.GetNodes("KERBAL"))
                {
                    KerbalLifeData d = KerbalLifeData.Load(kn);
                    if (d != null && !string.IsNullOrEmpty(d.KerbalName))
                        _kerbalData[d.KerbalName] = d;
                }
            }

            EnsureExistingCrewTracked();
        }

        // ── Unity Update ──────────────────────────────────────────────────────

        private void Update()
        {
            if (HighLogic.CurrentGame == null) return;

            double ut = Planetarium.GetUniversalTime();
            double dt = ut - _lastUpdateUT;
            if (dt <= 0.0) return;

            // Guard against unreasonably large jumps (e.g. first load with missing save key)
            const double maxDt = KerbinDay * 365.0 * 100.0;
            if (dt > maxDt) dt = maxDt;

            _lastUpdateUT = ut;

            TickKerbalTimers(dt);
            CheckBreedingConditions();
            ProcessFundDeductions(ut);

            _labUpdateTimer += Time.deltaTime;
            if (_labUpdateTimer >= LabUpdateInterval)
            {
                _labUpdateTimer = 0f;
                ApplyScienceLabEfficiency();
                ApplyHarvesterEfficiency();
                ApplyNonAdultIncapacitated();
                ApplyTeenagerEvaSas();
            }

            _expUpdateTimer += Time.deltaTime;
            if (_expUpdateTimer >= ExpUpdateInterval)
            {
                _expUpdateTimer = 0f;
                ForceNonAdultExperienceLevel();
            }
        }

        // ── Initialisation ────────────────────────────────────────────────────

        private void EnsureExistingCrewTracked()
        {
            KerbalRoster roster = HighLogic.CurrentGame?.CrewRoster;
            if (roster == null) return;

            foreach (ProtoCrewMember k in roster.Crew)
            {
                if (!_kerbalData.ContainsKey(k.name))
                    _kerbalData[k.name] = NewAdultData(k.name);
            }
        }

        private static KerbalLifeData NewAdultData(string name)
        {
            return new KerbalLifeData { KerbalName = name, Stage = LifeStage.Adult };
        }

        // ── Timer processing ──────────────────────────────────────────────────

        private void TickKerbalTimers(double dt)
        {
            var births   = new List<KerbalLifeData>();
            var toRemove = new List<string>();
            var toKill   = new List<ProtoCrewMember>();

            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                ProtoCrewMember k = HighLogic.CurrentGame.CrewRoster[d.KerbalName];
                if (k == null) { toRemove.Add(d.KerbalName); continue; }

                // Life-stage aging
                if (d.Stage != LifeStage.Adult && d.AgeTimer > 0.0)
                {
                    d.AgeTimer -= dt;
                    if (d.AgeTimer <= 0.0) AdvanceStage(d);
                }

                // Pregnancy countdown
                if (d.IsPregnant)
                {
                    d.PregnancyTimer -= dt;
                    if (d.PregnancyTimer <= 0.0)
                    {
                        d.IsPregnant      = false;
                        d.IsPostpartum    = true;
                        d.PostpartumTimer = BreedingConfig.PostpartumDuration;
                        births.Add(d);
                    }
                }

                // Post-partum countdown
                if (d.IsPostpartum)
                {
                    d.PostpartumTimer -= dt;
                    if (d.PostpartumTimer <= 0.0)
                        d.IsPostpartum = false;
                }

                // Caretaker-absence death clock
                if (TickCaretakerAbsence(d, k, dt))
                    toKill.Add(k);
            }

            // Apply deferred mutations outside the iteration
            foreach (string n in toRemove) _kerbalData.Remove(n);
            foreach (ProtoCrewMember k in toKill) KillKerbal(k, "abandoned without a caretaker");
            foreach (KerbalLifeData mother in births) SpawnNewborn(mother);
        }

        private void AdvanceStage(KerbalLifeData d)
        {
            switch (d.Stage)
            {
                case LifeStage.Newborn:
                    d.Stage    = LifeStage.Child;
                    d.AgeTimer = BreedingConfig.ChildDuration;
                    break;
                case LifeStage.Child:
                    d.Stage    = LifeStage.Teenager;
                    d.AgeTimer = BreedingConfig.TeenagerDuration;
                    // Teenager can EVA (with chaperone) — lift the G-force-blackout block
                    ProtoCrewMember pcmTeen = HighLogic.CurrentGame?.CrewRoster?[d.KerbalName];
                    if (pcmTeen != null) SetIncapacitated(pcmTeen, false);
                    break;
                case LifeStage.Teenager:
                    d.Stage    = LifeStage.Adult;
                    d.AgeTimer = 0.0;
                    break;
            }
            d.NoCaretakerTimer = 0.0;
            ScreenMessages.PostScreenMessage(
                $"{d.KerbalName} has grown to {d.Stage}!", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        /// <returns>True when the Kerbal should die immediately.</returns>
        private bool TickCaretakerAbsence(KerbalLifeData d, ProtoCrewMember k, double dt)
        {
            if (d.Stage == LifeStage.Adult) return false;

            Vessel v = FindKerbalVessel(k);
            if (v == null) return false;

            if (VesselHasAdultCaretaker(v, d.KerbalName))
            {
                d.NoCaretakerTimer = 0.0;
                return false;
            }

            // Teenagers survive but suffer -80 % efficiency (handled in GetEfficiencyMultiplier)
            if (d.Stage != LifeStage.Newborn && d.Stage != LifeStage.Child)
                return false;

            d.NoCaretakerTimer += dt;
            double limit = d.Stage == LifeStage.Newborn
                ? BreedingConfig.NewbornDeathTimer
                : BreedingConfig.ChildDeathTimer;

            return d.NoCaretakerTimer >= limit;
        }

        private void KillKerbal(ProtoCrewMember k, string reason)
        {
            ScreenMessages.PostScreenMessage(
                $"{k.name} has died ({reason})!", 8f, ScreenMessageStyle.UPPER_CENTER);
            _kerbalData.Remove(k.name);
            k.Die();
        }

        // ── Breeding conditions ───────────────────────────────────────────────

        private void CheckBreedingConditions()
        {
            // In manual mode the GUI calls RequestBreeding(); nothing to do here.
            if (!BreedingConfig.AutoBreeding) return;

            if (FlightGlobals.Vessels == null) return;

            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (!v.loaded)                 continue;
                if (!VesselHasScienceLab(v))   continue;
                if (!VesselHasLargeHabitat(v)) continue;

                List<ProtoCrewMember> crew = v.GetVesselCrew();
                if (!crew.Any(IsAdult))        continue;   // need at least one caretaker

                ProtoCrewMember male = crew.FirstOrDefault(k =>
                    k.gender == ProtoCrewMember.Gender.Male && IsAdult(k));

                ProtoCrewMember female = crew.FirstOrDefault(k =>
                    k.gender == ProtoCrewMember.Gender.Female && IsAdult(k)
                    && !IsPregnant(k) && !IsPostpartum(k));

                if (male == null || female == null) continue;

                StartPregnancy(female, male);
            }
        }

        private void StartPregnancy(ProtoCrewMember female, ProtoCrewMember male)
        {
            KerbalLifeData d = GetOrCreate(female);
            if (d.IsPregnant || d.IsPostpartum) return;

            d.IsPregnant     = true;
            d.PregnancyTimer = BreedingConfig.PregnancyDuration;
            d.PartnerName    = male.name;

            ScreenMessages.PostScreenMessage(
                $"{female.name} is pregnant!", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        // ── Birth ─────────────────────────────────────────────────────────────

        private void SpawnNewborn(KerbalLifeData motherData)
        {
            ProtoCrewMember mother = HighLogic.CurrentGame.CrewRoster[motherData.KerbalName];
            if (mother == null) return;

            ProtoCrewMember father     = HighLogic.CurrentGame.CrewRoster[motherData.PartnerName];
            string          fatherTrait = father?.experienceTrait?.TypeName ?? GeneticsHelper.Pilot;
            string          motherTrait = mother.experienceTrait?.TypeName  ?? GeneticsHelper.Scientist;

            string                 trait  = GeneticsHelper.DetermineOffspringTrait(fatherTrait, motherTrait);
            ProtoCrewMember.Gender gender = GeneticsHelper.DetermineGender();

            ProtoCrewMember newborn = HighLogic.CurrentGame.CrewRoster
                .GetNewKerbal(ProtoCrewMember.KerbalType.Crew);

            newborn.gender = gender;
            KerbalRoster.SetExperienceTrait(newborn, trait);

            _kerbalData[newborn.name] = new KerbalLifeData
            {
                KerbalName = newborn.name,
                Stage      = LifeStage.Newborn,
                AgeTimer   = BreedingConfig.NewbornDuration
            };

            Vessel vessel = FindKerbalVessel(mother);
                Debug.Log($"[BreedTheKerbal] SpawnNewborn: {newborn.name}, mother={mother.name}, vessel={(vessel == null ? "NULL" : vessel.vesselName)}, loaded={vessel?.loaded}");
                if (vessel != null)
                    BoardNewbornIntoHabitat(newborn, vessel);
                else
                    Debug.Log("[BreedTheKerbal] SpawnNewborn: no vessel found for mother — newborn stays Available");

                ScreenMessages.PostScreenMessage(
                $"A new Kerbal was born aboard: {newborn.name} ({trait}, {gender})!",
                8f, ScreenMessageStyle.UPPER_CENTER);
        }

        private static void BoardNewbornIntoHabitat(ProtoCrewMember newborn, Vessel vessel)
        {
            // Must be Assigned before adding to any part/snapshot
            newborn.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;

            if (vessel.loaded)
            {
                // Active vessel in the flight scene — use live Part API
                // FindHabitatPart already guarantees a part with free space
                Part habitat = FindHabitatPart(vessel);
                Debug.Log($"[BreedTheKerbal] BoardNewborn (loaded): habitat={(habitat == null ? "null" : habitat.partInfo.name + " " + habitat.protoModuleCrew.Count + "/" + habitat.CrewCapacity)}");
                if (habitat == null)
                {
                    newborn.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                    Debug.Log("[BreedTheKerbal] BoardNewborn: no free seat on loaded vessel — newborn stays Available");
                    return;
                }
                habitat.AddCrewmember(newborn);
                vessel.BackupVessel();   // sync live parts → ProtoVessel before any save
                GameEvents.onVesselCrewWasModified.Fire(vessel);
                vessel.SpawnCrew();      // refresh IVA models and portrait gallery
            }
            else
            {
                // Unloaded vessel (KSC / Tracking Station or distant vessel in flight)
                // vessel.Parts is empty — must edit the ProtoVessel snapshot directly
                ProtoPartSnapshot snap = FindHabitatSnap(vessel.protoVessel);
                Debug.Log($"[BreedTheKerbal] BoardNewborn (unloaded): snap={(snap == null ? "null" : snap.partName + " " + snap.protoModuleCrew.Count + "/" + (snap.partInfo?.partPrefab?.CrewCapacity ?? 0))}");
                if (snap == null)
                {
                    newborn.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                    Debug.Log("[BreedTheKerbal] BoardNewborn: no free seat on unloaded vessel — newborn stays Available");
                    return;
                }
                snap.protoModuleCrew.Add(newborn);
                GameEvents.onVesselCrewWasModified.Fire(vessel);
            }
        }

        // Find any crewable part snapshot that has at least one free seat
        private static ProtoPartSnapshot FindHabitatSnap(ProtoVessel pv)
        {
            if (pv == null) return null;
            foreach (ProtoPartSnapshot snap in pv.protoPartSnapshots)
            {
                int cap = snap.partInfo?.partPrefab?.CrewCapacity ?? 0;
                if (cap > 0 && snap.protoModuleCrew.Count < cap)
                    return snap;
            }
            return null;
        }

        // ── Fund deductions ───────────────────────────────────────────────────

        private void ProcessFundDeductions(double ut)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            if (Funding.Instance == null) return;

            // Clear flag immediately when funds recover mid-day
            if (_isLowSupport && Funding.Instance.Funds >= GetDailyUpkeepCost())
                _isLowSupport = false;

            double daysPassed = (ut - _lastFundDeductUT) / KerbinDay;
            if (daysPassed < 1.0) return;
            _lastFundDeductUT += KerbinDay;

            double cost = 0.0;
            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                switch (d.Stage)
                {
                    case LifeStage.Newborn:  cost += BreedingConfig.NewbornDailyCost;  break;
                    case LifeStage.Child:    cost += BreedingConfig.ChildDailyCost;    break;
                    case LifeStage.Teenager: cost += BreedingConfig.TeenagerDailyCost; break;
                }
            }

            if (cost <= 0.0) return;

            if (Funding.Instance.Funds >= cost)
            {
                Funding.Instance.AddFunds(-cost, TransactionReasons.VesselRecovery);
                _isLowSupport = false;
            }
            else
            {
                _isLowSupport = true;
                Debug.Log("[BreedTheKerbal] Low-support state active – insufficient funds for child care.");
            }
        }

        // ── Science Lab efficiency ────────────────────────────────────────────

        private void ApplyScienceLabEfficiency()
        {
            if (FlightGlobals.Vessels == null) return;

            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (!v.loaded) continue;

                List<ProtoCrewMember> crew = v.GetVesselCrew();

                foreach (Part p in v.Parts)
                {
                    if (p.FindModuleImplementing<ModuleScienceLab>() == null) continue;

                    ModuleScienceConverter converter = p.FindModuleImplementing<ModuleScienceConverter>();
                    if (converter == null) continue;

                    // Always multiply against the unmodified prefab value to avoid drift
                    ModuleScienceConverter prefabConverter = p.partInfo?.partPrefab
                        ?.FindModuleImplementing<ModuleScienceConverter>();
                    if (prefabConverter == null) continue;

                    float efficiency = crew
                        .Select(k => GetEfficiencyMultiplier(k.name))
                        .DefaultIfEmpty(1f)
                        .Min();

                    converter.dataProcessingMultiplier = prefabConverter.dataProcessingMultiplier * efficiency;
                }
            }
        }

        // ── EVA restriction ───────────────────────────────────────────────────

        private void OnAttemptEva(ProtoCrewMember kerbal, Part part, Transform transform)
        {
            if (!CanEVA(kerbal.name))
            {
                ScreenMessages.PostScreenMessage(
                    $"{kerbal.name} cannot perform EVA at this life stage.",
                    4f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            // Teenager needs an adult already on EVA as chaperone
            KerbalLifeData d = GetData(kerbal.name);
            if (d != null && d.Stage == LifeStage.Teenager
                && part?.vessel != null && !VesselHasAdultOnEva(part.vessel))
                ScreenMessages.PostScreenMessage(
                    $"{kerbal.name} needs an adult already on EVA as chaperone!",
                    4f, ScreenMessageStyle.UPPER_CENTER);
        }

        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (FlightGlobals.ActiveVessel == null || !FlightGlobals.ActiveVessel.isEVA) return;

            ProtoCrewMember evaKerbal = FlightGlobals.ActiveVessel
                .GetVesselCrew()
                .FirstOrDefault(k => !CanEVA(k.name));

            if (evaKerbal != null)
            {
                ScreenMessages.PostScreenMessage(
                    $"{evaKerbal.name} is not cleared for EVA — boarding immediately!",
                    5f, ScreenMessageStyle.UPPER_CENTER);

                // Force them back — onAttemptEva is informational only and cannot cancel the EVA
                KerbalEVA evaModule = FlightGlobals.ActiveVessel
                    .FindPartModuleImplementing<KerbalEVA>();
                if (evaModule != null && action.from != null)
                    evaModule.BoardPart(action.from);

                return;
            }

            // Teenager na EVA bez dorosłego opiekuna już na zewnątrz → cofnij
            ProtoCrewMember teen = FlightGlobals.ActiveVessel
                .GetVesselCrew()
                .FirstOrDefault(k => GetData(k.name)?.Stage == LifeStage.Teenager);

            if (teen != null && action.from?.vessel != null
                && !VesselHasAdultOnEva(action.from.vessel, teen.name))
            {
                ScreenMessages.PostScreenMessage(
                    $"{teen.name} nie ma dorosłego opiekuna na EVA — wraca na pokład!",
                    5f, ScreenMessageStyle.UPPER_CENTER);

                KerbalEVA evaModule = FlightGlobals.ActiveVessel
                    .FindPartModuleImplementing<KerbalEVA>();
                if (evaModule != null && action.from != null)
                    evaModule.BoardPart(action.from);
            }
        }

        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> action)
        {
            KerbalLifeData d = GetData(action.host.name);
            if (d == null || d.Stage != LifeStage.Newborn) return;
            if (action.to?.vessel == null) return;
            if (!VesselHasAdultCaretaker(action.to.vessel, action.host.name))
                ScreenMessages.PostScreenMessage(
                    $"{action.host.name} cannot be moved without an adult present!",
                    5f, ScreenMessageStyle.UPPER_CENTER);
        }

        private void OnFlightLogRecorded(Vessel vessel)
        {
            // Remove every other new flight-log entry for teenagers → ~50 % XP gain rate
            foreach (ProtoCrewMember pcm in vessel.GetVesselCrew())
            {
                KerbalLifeData d = GetData(pcm.name);
                if (d == null || d.Stage != LifeStage.Teenager) continue;
                if (pcm.flightLog.Entries.Count == 0) continue;

                bool allow;
                if (!_teenXpAllow.TryGetValue(pcm.name, out allow)) allow = true;
                _teenXpAllow[pcm.name] = !allow;

                if (!allow)
                    pcm.flightLog.Entries.RemoveAt(pcm.flightLog.Entries.Count - 1);
            }
        }

        // ── Public query API ──────────────────────────────────────────────────

        /// <summary>Exposes raw life data for a Kerbal (read-only use by GUI).</summary>
        public KerbalLifeData GetKerbalDataPublic(string name) => GetData(name);

        /// <summary>True while the colony cannot cover its daily upkeep.</summary>
        public bool IsLowSupport => _isLowSupport;

        /// <summary>Total funds deducted per Kerbin day for all non-adult Kerbals.</summary>
        public double GetDailyUpkeepCost()
        {
            double cost = 0.0;
            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                switch (d.Stage)
                {
                    case LifeStage.Newborn:  cost += BreedingConfig.NewbornDailyCost;  break;
                    case LifeStage.Child:    cost += BreedingConfig.ChildDailyCost;    break;
                    case LifeStage.Teenager: cost += BreedingConfig.TeenagerDailyCost; break;
                }
            }
            return cost;
        }

        /// <summary>
        /// GUI entry point: validates breeding conditions and starts pregnancy.
        /// Returns true if pregnancy was successfully started.
        /// </summary>
        public bool RequestBreeding(Vessel vessel, ProtoCrewMember female, ProtoCrewMember male)
        {
            if (vessel == null || female == null || male == null)            return false;
            if (female.gender != ProtoCrewMember.Gender.Female) return false;
            if (male.gender   != ProtoCrewMember.Gender.Male)   return false;
            if (!VesselHasScienceLab(vessel))   return false;
            if (!VesselHasLargeHabitat(vessel)) return false;
            if (!VesselHasAdultCaretaker(vessel)) return false;
            if (!IsAdult(female) || !IsAdult(male)) return false;
            if (IsPregnant(female) || IsPostpartum(female)) return false;
            if (VesselFreeSeats(vessel) < 1) return false;   // no room for newborn
            StartPregnancy(female, male);
            return true;
        }

        public bool IsAdult(ProtoCrewMember k)
        {
            KerbalLifeData d = GetData(k.name);
            return d == null || d.Stage == LifeStage.Adult;
        }

        public bool IsPregnant(ProtoCrewMember k)   => GetData(k.name)?.IsPregnant  == true;
        public bool IsPostpartum(ProtoCrewMember k) => GetData(k.name)?.IsPostpartum == true;

        public bool CanEVA(string kerbalName)
        {
            KerbalLifeData d = GetData(kerbalName);
            if (d == null) return true;
            if (d.Stage == LifeStage.Newborn || d.Stage == LifeStage.Child) return false;
            if (d.IsPostpartum) return false;
            if (d.IsPregnant && d.PregnancyTimer <= BreedingConfig.PregnancyDuration / 2.0) return false;
            if (d.Stage == LifeStage.Teenager)
            {
                ProtoCrewMember pcm = HighLogic.CurrentGame?.CrewRoster?[kerbalName];
                Vessel v = FindKerbalVessel(pcm);
                if (v != null && !VesselHasAdultOnEva(v)) return false;
            }
            return true;
        }

        public float GetEfficiencyMultiplier(string kerbalName)
        {
            KerbalLifeData d = GetData(kerbalName);
            if (d == null) return 1f;

            switch (d.Stage)
            {
                case LifeStage.Newborn:
                    return BreedingConfig.NewbornEfficiency;

                case LifeStage.Child:
                {
                    float eff = BreedingConfig.ChildEfficiency;
                    return _isLowSupport ? eff * BreedingConfig.LowSupportChildFactor : eff;
                }

                case LifeStage.Teenager:
                {
                    ProtoCrewMember pcm = HighLogic.CurrentGame?.CrewRoster?[kerbalName];
                    Vessel tv = FindKerbalVessel(pcm);
                    bool hasCarer = tv != null && VesselHasAdultCaretaker(tv, kerbalName);
                    if (!hasCarer) return BreedingConfig.NoCaretakerTeenagerEfficiency;
                    float eff = BreedingConfig.TeenagerEfficiency;
                    return _isLowSupport ? eff * BreedingConfig.LowSupportTeenagerFactor : eff;
                }

                case LifeStage.Adult:
                    if (d.IsPostpartum) return BreedingConfig.PostpartumEfficiency;
                    if (d.IsPregnant)
                    {
                        // First half of pregnancy: no penalty
                        if (d.PregnancyTimer > BreedingConfig.PregnancyDuration / 2.0)
                            return 1f;
                        // Second half: capped at 2-star level
                        return BreedingConfig.LatePregnancyEfficiency;
                    }
                    return 1f;

                default: return 1f;
            }
        }

        public LifeStage GetLifeStage(string kerbalName)
        {
            KerbalLifeData d = GetData(kerbalName);
            return d?.Stage ?? LifeStage.Adult;
        }

        // ── Skill / efficiency enforcement ────────────────────────────────────

        /// <summary>
        /// Keeps Newborns and Children in the G-force-blackout state every second.
        /// This disables their portrait EVA button at the KSP UI level (same mechanism
        /// as the post-G-force static effect the user observed).
        /// </summary>
        private void ApplyNonAdultIncapacitated()
        {
            if (HighLogic.CurrentGame?.CrewRoster == null) return;
            // Push inactiveTimeEnd far into the future each tick so the G-force
            // recovery system cannot expire it between our calls.
            double now       = Planetarium.GetUniversalTime();
            double farFuture = now + 1e10;
            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster[d.KerbalName];
                if (pcm == null) continue;

                bool shouldBlock = !CanEVA(d.KerbalName);

                // Teenager już na EVA: nie stosuj G-force blackout — obsługuje ApplyTeenagerEvaSas
                if (shouldBlock && d.Stage == LifeStage.Teenager
                    && FindKerbalVessel(pcm)?.isEVA == true)
                    shouldBlock = false;

                if (shouldBlock)
                {
                    pcm.outDueToG       = true;
                    pcm.inactiveTimeEnd = farFuture;
                    pcm.inactive        = true;
                }
                else if (pcm.outDueToG && pcm.inactiveTimeEnd > now + 1e9)
                {
                    // Czyść tylko stan ustawiony przez nas (rozpoznawalny po dalekim timestamp)
                    SetIncapacitated(pcm, false);
                }
            }
        }

        private static void SetIncapacitated(ProtoCrewMember pcm, bool incapacitated)
        {
            if (incapacitated)
            {
                pcm.outDueToG       = true;
                pcm.inactiveTimeEnd = double.MaxValue * 0.5;
                pcm.inactive        = true;
            }
            else
            {
                pcm.outDueToG       = false;
                pcm.inactiveTimeEnd = 0.0;
                pcm.inactive        = false;
            }
        }

        private void ApplyTeenagerEvaSas()
        {
            if (FlightGlobals.Vessels == null) return;
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (!v.isEVA || !v.loaded) continue;
                ProtoCrewMember k = v.GetVesselCrew().FirstOrDefault();
                if (k == null) continue;
                KerbalLifeData d = GetData(k.name);
                if (d == null || d.Stage != LifeStage.Teenager) continue;

                KerbalEVA evaModule = v.FindPartModuleImplementing<KerbalEVA>();
                float prefabThrust = evaModule?.part?.partInfo?.partPrefab
                    ?.FindModuleImplementing<KerbalEVA>()?.thrustPercentage ?? 100f;

                bool hasChaperone = VesselHasAdultOnEva(v, k.name);

                if (!hasChaperone)
                {
                    if (v.ActionGroups[KSPActionGroup.SAS])
                    {
                        v.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                        ScreenMessages.PostScreenMessage(
                            $"{k.name} — brak opiekuna na EVA, SAS wyłączony!",
                            4f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    if (evaModule != null)
                        evaModule.thrustPercentage = prefabThrust * 0.02f;
                }
                else if (evaModule != null)
                {
                    evaModule.thrustPercentage = prefabThrust;
                }
            }
        }

        private void ApplyHarvesterEfficiency()
        {
            if (FlightGlobals.Vessels == null) return;
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (!v.loaded) continue;
                List<ProtoCrewMember> crew = v.GetVesselCrew();
                float efficiency = crew
                    .Select(k => GetEfficiencyMultiplier(k.name))
                    .DefaultIfEmpty(1f)
                    .Min();
                foreach (Part p in v.Parts)
                {
                    ModuleResourceHarvester harvester = p.FindModuleImplementing<ModuleResourceHarvester>();
                    if (harvester == null) continue;
                    ModuleResourceHarvester prefab = p.partInfo?.partPrefab
                        ?.FindModuleImplementing<ModuleResourceHarvester>();
                    if (prefab == null) continue;
                    harvester.Efficiency = prefab.Efficiency * efficiency;
                }
            }
        }

        /// <summary>
        /// Pilnuje poziomów doświadczenia: nie-dorośli = 0, pilot w drugiej połowie ciąży = 1 (tylko podstawowy SAS),
        /// dorośli bez ograniczeń = przywróć właściwy poziom z XP.
        /// </summary>
        private void ForceNonAdultExperienceLevel()
        {
            if (HighLogic.CurrentGame?.CrewRoster == null) return;
            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster[d.KerbalName];
                if (pcm == null) continue;

                if (d.Stage != LifeStage.Adult)
                {
                    // Nie-dorośly: blokada na poziom 0
                    if (pcm.experienceLevel != 0)
                        SetExperienceLevel(pcm, 0);
                }
                else if (d.IsPregnant
                    && d.PregnancyTimer <= BreedingConfig.PregnancyDuration / 2.0
                    && pcm.trait == "Pilot")
                {
                    // Pilot w drugiej połowie ciąży: tylko poziom 1 (podstawowy SAS, brak trybów autopilota)
                    if (pcm.experienceLevel > 1)
                        SetExperienceLevel(pcm, 1);
                }
                else
                {
                    // Dorośla bez ograniczeń: przywróć poziom jeśli został przez nas obniżony
                    int expected = LevelFromExperience(pcm.experience);
                    if (pcm.experienceLevel < expected)
                        SetExperienceLevel(pcm, expected);
                }
            }
        }

        private static void SetExperienceLevel(ProtoCrewMember pcm, int level)
        {
            pcm.experienceLevel = level;
        }

        private static int LevelFromExperience(float xp)
        {
            if (xp >= 64f) return 5;
            if (xp >= 32f) return 4;
            if (xp >= 16f) return 3;
            if (xp >=  8f) return 2;
            if (xp >=  2f) return 1;
            return 0;
        }

        // ── Vessel helpers ────────────────────────────────────────────────────

        private static bool VesselHasScienceLab(Vessel v)
            => v.Parts.Any(p => p.FindModuleImplementing<ModuleScienceLab>() != null);

        private static bool VesselHasLargeHabitat(Vessel v)
            => v.Parts.Any(p => p.CrewCapacity >= BreedingConfig.HabitatMinCrewCapacity);

        private bool VesselHasAdultCaretaker(Vessel v, string excludeName = null)
            => v.GetVesselCrew().Any(k =>
                (excludeName == null || k.name != excludeName) && IsAdult(k));

        // Free seats = total capacity − current crew − seats reserved by ongoing pregnancies.
        // Each pregnant kerbal on the vessel "holds" one extra seat for the coming newborn.
        private int VesselFreeSeats(Vessel v)
        {
            int totalCap = v.Parts.Sum(p => p.CrewCapacity);
            List<ProtoCrewMember> crew = v.GetVesselCrew();
            int reserved = crew.Count(k => IsPregnant(k));
            return totalCap - crew.Count - reserved;
        }

        public int GetVesselFreeSeats(Vessel v) => VesselFreeSeats(v);

        /// <summary>
        /// Returns true if any adult Kerbal on EVA is within 200 m of <paramref name="parentVessel"/>.
        /// </summary>
        private bool VesselHasAdultOnEva(Vessel parentVessel, string excludeKerbalName = null)
        {
            if (FlightGlobals.Vessels == null) return false;
            foreach (Vessel ev in FlightGlobals.Vessels)
            {
                if (!ev.isEVA) continue;
                if (Vector3d.Distance(ev.GetWorldPos3D(), parentVessel.GetWorldPos3D()) > 200.0) continue;
                foreach (ProtoCrewMember k in ev.GetVesselCrew())
                {
                    if (excludeKerbalName != null && k.name == excludeKerbalName) continue;
                    if (IsAdult(k)) return true;
                }
            }
            return false;
        }

        // Find any crewable part that has at least one free seat
        private static Part FindHabitatPart(Vessel v)
            => v.Parts.FirstOrDefault(p => p.CrewCapacity > 0 && p.protoModuleCrew.Count < p.CrewCapacity);

        private static Vessel FindKerbalVessel(ProtoCrewMember k)
        {
            if (k == null || FlightGlobals.Vessels == null) return null;
            return FlightGlobals.Vessels
                .FirstOrDefault(v => v.GetVesselCrew().Any(c => c.name == k.name));
        }

        // ── Data helpers ──────────────────────────────────────────────────────

#if DEBUG
        // Alt+Shift+B  →  wyświetl status wszystkich Kerbali na ekranie
        // Alt+Shift+N  →  przesuń WSZYSTKICH nie-dorosłych o jeden etap do przodu
        private void OnGUI()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (!e.alt || !e.shift) return;

            if (e.keyCode == KeyCode.B) { DebugPrintStatus();      e.Use(); }
            if (e.keyCode == KeyCode.N) { DebugAdvanceAllStages(); e.Use(); }
        }

        private void DebugPrintStatus()
        {
            ScreenMessages.PostScreenMessage(
                "[BTK DEBUG] ── Status Kerbali ──",
                6f, ScreenMessageStyle.UPPER_LEFT);

            foreach (KerbalLifeData d in _kerbalData.Values)
            {
                string extra = "";
                if (d.IsPregnant)   extra += $" ciąża={d.PregnancyTimer:F0}s";
                if (d.IsPostpartum) extra += $" połóg={d.PostpartumTimer:F0}s";
                if (d.AgeTimer > 0) extra += $" wiek={d.AgeTimer:F0}s";

                ScreenMessages.PostScreenMessage(
                    $"  {d.KerbalName}: {d.Stage}{extra}",
                    6f, ScreenMessageStyle.UPPER_LEFT);
            }
        }

        private void DebugAdvanceAllStages()
        {
            var toAdvance = new System.Collections.Generic.List<KerbalLifeData>();
            foreach (KerbalLifeData d in _kerbalData.Values)
                if (d.Stage != LifeStage.Adult) toAdvance.Add(d);

            foreach (KerbalLifeData d in toAdvance)
            {
                d.AgeTimer = 0.0;
                AdvanceStage(d);
            }

            ScreenMessages.PostScreenMessage(
                $"[BTK DEBUG] Przesunięto {toAdvance.Count} Kerbali o 1 etap.",
                5f, ScreenMessageStyle.UPPER_CENTER);
        }
#endif

        private KerbalLifeData GetData(string name)
        {
            KerbalLifeData d;
            _kerbalData.TryGetValue(name, out d);
            return d;
        }

        private KerbalLifeData GetOrCreate(ProtoCrewMember k)
        {
            KerbalLifeData d;
            if (!_kerbalData.TryGetValue(k.name, out d))
            {
                d = NewAdultData(k.name);
                _kerbalData[k.name] = d;
            }
            return d;
        }
    }
}
