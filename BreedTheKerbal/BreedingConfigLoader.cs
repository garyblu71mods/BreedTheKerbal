using UnityEngine;

namespace BreedTheKerbal
{
    /// <summary>
    /// Runs once at the KSP Main Menu and loads BreedTheKerbal.cfg from GameDatabase
    /// into the static BreedingConfig fields.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class BreedingConfigLoader : MonoBehaviour
    {
        private void Start()
        {
            UrlDir.UrlConfig[] configs =
                GameDatabase.Instance.GetConfigs("BREEDTHEKERBAL_CONFIG");

            if (configs == null || configs.Length == 0)
            {
                Debug.Log("[BreedTheKerbal] No BREEDTHEKERBAL_CONFIG node found — using defaults.");
                return;
            }

            BreedingConfig.LoadFromNode(configs[0].config);
        }
    }
}
