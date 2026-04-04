namespace BreedTheKerbal
{
    /// <summary>
    /// All configurable durations, efficiency multipliers and cost values.
    /// Durations are in Kerbin seconds (1 Kerbin day = 21 600 s).
    /// Values are loaded at startup from GameData/BreedTheKerbal/BreedTheKerbal.cfg.
    /// </summary>
    public static class BreedingConfig
    {
        private const double KerbinDay = 21600.0;

        // ── Durations ─────────────────────────────────────────────────────────
        public static double PregnancyDuration  = KerbinDay *  30;
        public static double PostpartumDuration = KerbinDay *  10;

        public static double NewbornDuration    = KerbinDay *  50;
        public static double ChildDuration      = KerbinDay * 100;
        public static double TeenagerDuration   = KerbinDay * 150;

        // ── Efficiency multipliers ────────────────────────────────────────────
        public static float LatePregnancyEfficiency        = 0.40f;
        public static float PostpartumEfficiency           = 0.00f;
        public static float NewbornEfficiency              = 0.00f;
        public static float ChildEfficiency                = 0.25f;
        public static float TeenagerEfficiency             = 0.50f;
        public static float NoCaretakerTeenagerEfficiency  = 0.20f;

        public static float LowSupportChildFactor    = 0.80f;
        public static float LowSupportTeenagerFactor = 0.70f;

        // ── Caretaker-absence death timers ────────────────────────────────────
        public static double NewbornDeathTimer = KerbinDay *  5;
        public static double ChildDeathTimer   = KerbinDay * 15;

        // ── Daily upkeep costs (Career mode, per Kerbin day) ──────────────────
        public static double NewbornDailyCost  = 150.0;
        public static double ChildDailyCost    = 100.0;
        public static double TeenagerDailyCost =  50.0;

        // ── Vessel requirements ────────────────────────────────────────────────
        public static int  HabitatMinCrewCapacity = 4;

        // ── Cupola aging bonus ────────────────────────────────────────────────
        /// <summary>Fraction added to aging speed per cupola on the vessel (0.05 = +5%).</summary>
        public static float CupolaAgingBonusPerUnit = 0.05f;
        /// <summary>Aging speed bonus when the vessel is landed or splashed (0.10 = +10%).</summary>
        public static float LandedAgingBonus        = 0.10f;
        /// <summary>Bonus when both biological parents are on the same vessel (+5%).</summary>
        public static float BothParentsAgingBonus   = 0.05f;
        /// <summary>Penalty when neither parent is on the vessel (-5%).</summary>
        public static float NoParentsAgingPenalty      = 0.05f;
        /// <summary>Maximum number of cupolas counted for the aging bonus.</summary>
        public static int   CupolaAgingMaxCount        = 3;
        /// <summary>Aging speed penalty when Low Support is active (-10%).</summary>
        public static float LowSupportAgingPenalty     = 0.10f;
        /// <summary>Best-adult caretaker bonus for a Scientist (+5%).</summary>
        public static float ScientistCaretakerBonus    = 0.05f;
        /// <summary>Best-adult caretaker bonus for an Engineer (+3%).</summary>
        public static float EngineerCaretakerBonus     = 0.03f;
        /// <summary>Crew/capacity ratio above which the overcrowding penalty applies (0.75 = 75%).</summary>
        public static float OvercrowdingThreshold      = 0.75f;
        /// <summary>Aging speed penalty when the vessel is overcrowded (-5%).</summary>
        public static float OvercrowdingPenalty        = 0.05f;
        /// <summary>Aging speed bonus when the vessel has an active CommNet connection to home (+2%).</summary>
        public static float CommNetBonus               = 0.02f;

        // ── Breeding mode ──────────────────────────────────────────────────────
        public static bool AutoBreeding = false;

        // ── Config loader ─────────────────────────────────────────────────────
        public static void LoadFromNode(ConfigNode node)
        {
            node.TryGetValue("PregnancyDuration",           ref PregnancyDuration);
            node.TryGetValue("PostpartumDuration",          ref PostpartumDuration);
            node.TryGetValue("NewbornDuration",             ref NewbornDuration);
            node.TryGetValue("ChildDuration",               ref ChildDuration);
            node.TryGetValue("TeenagerDuration",            ref TeenagerDuration);

            node.TryGetValue("LatePregnancyEfficiency",        ref LatePregnancyEfficiency);
            node.TryGetValue("PostpartumEfficiency",           ref PostpartumEfficiency);
            node.TryGetValue("NewbornEfficiency",              ref NewbornEfficiency);
            node.TryGetValue("ChildEfficiency",                ref ChildEfficiency);
            node.TryGetValue("TeenagerEfficiency",             ref TeenagerEfficiency);
            node.TryGetValue("NoCaretakerTeenagerEfficiency",  ref NoCaretakerTeenagerEfficiency);
            node.TryGetValue("LowSupportChildFactor",          ref LowSupportChildFactor);
            node.TryGetValue("LowSupportTeenagerFactor",       ref LowSupportTeenagerFactor);

            node.TryGetValue("NewbornDeathTimer",           ref NewbornDeathTimer);
            node.TryGetValue("ChildDeathTimer",             ref ChildDeathTimer);

            node.TryGetValue("NewbornDailyCost",            ref NewbornDailyCost);
            node.TryGetValue("ChildDailyCost",              ref ChildDailyCost);
            node.TryGetValue("TeenagerDailyCost",           ref TeenagerDailyCost);

            node.TryGetValue("HabitatMinCrewCapacity",      ref HabitatMinCrewCapacity);
            node.TryGetValue("AutoBreeding",                ref AutoBreeding);
            node.TryGetValue("CupolaAgingBonusPerUnit",     ref CupolaAgingBonusPerUnit);
            node.TryGetValue("LandedAgingBonus",            ref LandedAgingBonus);
            node.TryGetValue("BothParentsAgingBonus",       ref BothParentsAgingBonus);
            node.TryGetValue("NoParentsAgingPenalty",       ref NoParentsAgingPenalty);
            node.TryGetValue("CupolaAgingMaxCount",         ref CupolaAgingMaxCount);
            node.TryGetValue("LowSupportAgingPenalty",      ref LowSupportAgingPenalty);
            node.TryGetValue("ScientistCaretakerBonus",     ref ScientistCaretakerBonus);
            node.TryGetValue("EngineerCaretakerBonus",      ref EngineerCaretakerBonus);
            node.TryGetValue("OvercrowdingThreshold",       ref OvercrowdingThreshold);
            node.TryGetValue("OvercrowdingPenalty",         ref OvercrowdingPenalty);
            node.TryGetValue("CommNetBonus",                ref CommNetBonus);

            UnityEngine.Debug.Log(
                $"[BreedTheKerbal] Config loaded — Pregnancy={PregnancyDuration}s " +
                $"Newborn={NewbornDuration}s Child={ChildDuration}s Teen={TeenagerDuration}s");
        }
    }
}
