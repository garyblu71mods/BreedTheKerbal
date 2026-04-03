namespace BreedTheKerbal
{
    /// <summary>
    /// Determines offspring class (experience trait) and gender from parent traits.
    /// Rules: same + same = 96 % same / 4 % mutation (2 % each other trait);
    /// mixed combinations = 48 % traitA / 48 % traitB / 4 % third trait.
    /// </summary>
    public static class GeneticsHelper
    {
        // KSP experience-trait type names
        public const string Pilot     = "Pilot";
        public const string Engineer  = "Engineer";
        public const string Scientist = "Scientist";

        private static readonly string[] AllTraits = { Pilot, Engineer, Scientist };

        private static readonly System.Random Rng = new System.Random();

        public static string DetermineOffspringTrait(string traitA, string traitB)
        {
            if (traitA == traitB)
            {
                // 4 % chance of mutation into one of the two other traits
                if (Rng.NextDouble() < 0.04)
                {
                    string mutant;
                    do { mutant = AllTraits[Rng.Next(AllTraits.Length)]; }
                    while (mutant == traitA);
                    return mutant;
                }
                return traitA;
            }

            // Mixed: 4 % third trait, otherwise 50 / 50 between the parents' traits
            if (Rng.NextDouble() < 0.04)
            {
                string third;
                do { third = AllTraits[Rng.Next(AllTraits.Length)]; }
                while (third == traitA || third == traitB);
                return third;
            }
            return Rng.NextDouble() < 0.50 ? traitA : traitB;
        }

        public static ProtoCrewMember.Gender DetermineGender()
        {
            return Rng.NextDouble() < 0.50
                ? ProtoCrewMember.Gender.Male
                : ProtoCrewMember.Gender.Female;
        }
    }
}
