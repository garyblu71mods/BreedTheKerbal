namespace BreedTheKerbal
{
    /// <summary>
    /// Holds all life-cycle state for a single Kerbal.
    /// Serialised into a ConfigNode child of the ScenarioModule save.
    /// </summary>
    public class KerbalLifeData
    {
        public string    KerbalName;
        public LifeStage Stage;
        public double    AgeTimer;           // seconds until next life stage
        public bool      IsPregnant;
        public double    PregnancyTimer;     // seconds remaining in pregnancy
        public string    PartnerName;        // father's name (used during pregnancy)
        public string    MotherName;         // biological mother (stored on child)
        public string    FatherName;         // biological father (stored on child)
        public bool      IsPostpartum;
        public double    PostpartumTimer;    // seconds remaining post-partum
        public double    NoCaretakerTimer;   // seconds without a caretaker (death clock)

        public void Save(ConfigNode node)
        {
            node.AddValue("kerbalName",       KerbalName   ?? string.Empty);
            node.AddValue("stage",            (int)Stage);
            node.AddValue("ageTimer",         AgeTimer);
            node.AddValue("isPregnant",       IsPregnant);
            node.AddValue("pregnancyTimer",   PregnancyTimer);
            node.AddValue("partnerName",      PartnerName  ?? string.Empty);
            node.AddValue("motherName",        MotherName   ?? string.Empty);
            node.AddValue("fatherName",        FatherName   ?? string.Empty);
            node.AddValue("isPostpartum",     IsPostpartum);
            node.AddValue("postpartumTimer",  PostpartumTimer);
            node.AddValue("noCaretakerTimer", NoCaretakerTimer);
        }

        public static KerbalLifeData Load(ConfigNode node)
        {
            var d = new KerbalLifeData();
            d.KerbalName = node.GetValue("kerbalName") ?? string.Empty;

            int stageInt;
            int.TryParse(node.GetValue("stage"), out stageInt);
            d.Stage = (LifeStage)stageInt;

            double.TryParse(node.GetValue("ageTimer"),         out d.AgeTimer);
            bool  .TryParse(node.GetValue("isPregnant"),       out d.IsPregnant);
            double.TryParse(node.GetValue("pregnancyTimer"),   out d.PregnancyTimer);
            d.PartnerName = node.GetValue("partnerName") ?? string.Empty;
            d.MotherName  = node.GetValue("motherName")  ?? string.Empty;
            d.FatherName  = node.GetValue("fatherName")  ?? string.Empty;
            bool  .TryParse(node.GetValue("isPostpartum"),     out d.IsPostpartum);
            double.TryParse(node.GetValue("postpartumTimer"),  out d.PostpartumTimer);
            double.TryParse(node.GetValue("noCaretakerTimer"), out d.NoCaretakerTimer);

            return d;
        }
    }
}
