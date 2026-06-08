using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PerAspera.Core;
using System.IO;

namespace PerAspera.WaterWorkFix
{
    /// <summary>
    /// Companion fix plugin for the WaterWork (water_early) YAML mod.
    ///
    /// Fixes three issues with ice_mine_purification_plant T1/T2/T3:
    ///   1. Ice blocks produced despite outputResource:null
    ///   2. Water vein never depleted
    ///   3. Pipes produced even when unpowered or vein empty
    ///
    /// Bonus: ice mines contribute to global water level when Mars temp > 0°C.
    ///
    /// Per-building configuration: BepInEx/config/PerAspera.VeinBehaviors.json
    /// (auto-created with defaults on first run — add any custom building key to extend)
    /// </summary>
    [BepInPlugin("com.modperaspera.waterworkfix", "WaterWorkFix", "1.0.0")]
    public class WaterWorkFixPlugin : BasePlugin
    {
        internal static new LogAspera Log = null!;

        public override void Load()
        {
            Log = new LogAspera("WaterWorkFix");

            string configDir = Path.Combine(Paths.ConfigPath);
            VeinBehaviorsConfig.Load(configDir);

            var harmony = new Harmony("com.modperaspera.waterworkfix");
            harmony.PatchAll();

            Log.Info("WaterWorkFix v1.0 actif — vein fix + ice melt (WaterWork companion)");
        }
    }
}
