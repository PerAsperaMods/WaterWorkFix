using HarmonyLib;
using PerAspera.GameAPI.Wrappers;
using System;
using System.Diagnostics;
using System.Reflection;

namespace PerAspera.WaterWorkFix
{
    // Patches Building.OnTick for all buildings listed in PerAspera.VeinBehaviors.json.
    //
    // Per-building flags (see BuildingVeinConfig):
    //   suppressBlock  — zeros extractionLevel before native call → no unwanted resource block produced
    //   consumeVein    — Postfix drains vein at progressPerDay (or consumptionRate) per day
    //   gatePipes      — zeros pipesProduction when building is unpowered or vein is empty
    //   meltWaterRatePerDegree — adds to planet.waterStock when Mars temp > 0°C
    //
    // Water mines (vanilla): only meltWaterRatePerDegree active (all other flags false).
    // Ice purification plants (mod): all four flags active.

    internal struct VeinPatchState
    {
        public bool  InConfig;
        public bool  SuppressBlock;
        public bool  ConsumeVein;
        public bool  GatePipes;
        public float ConsumptionRate;
        public float MeltRate;

        public int   SavedExtractionLevel;
        public float SavedPipesProduction; // negative sentinel = not saved
    }

    // Prefix on Universe.OnDaysPassed guarantees the blackboard flag is set
    // synchronously BEFORE GevUniverseDayPassed dispatches (and before YAML rules evaluate).
    // Must write to Universe.blackboardMain ("main" scope) — that's what Criterion.Evaluate reads.
    [HarmonyPatch(typeof(Universe), "OnDaysPassed")]
    internal static class UniverseDaysPassedCompanionPatch
    {
        private static bool _flagSet;

        static void Prefix(Universe __instance)
        {
            if (_flagSet) return;
            try
            {
                var bb = __instance.blackboardMain;
                if (bb == null)
                {
                    WaterWorkFixPlugin.Log.Warning("[CompanionFlag] blackboardMain is null — retry next day");
                    return;
                }
                bb.SetValue("waterworkfix_companion_loaded", true);
                _flagSet = true;
                WaterWorkFixPlugin.Log.Info("[CompanionFlag] waterworkfix_companion_loaded = true (main blackboard)");
            }
            catch (Exception ex)
            {
                WaterWorkFixPlugin.Log.Warning($"[CompanionFlag] Error setting blackboard: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class IceMineVeinPatch
    {
        private static PlanetWrapper? _planet;
        private const float FREEZING_K = 273.15f;

        [HarmonyTargetMethod]
        static MethodBase? TargetMethod()
        {
            var method = AccessTools.Method(typeof(Building), "OnTick", new[] { typeof(float) });
            if (method == null)
                WaterWorkFixPlugin.Log.Error("[VeinPatch] Building.OnTick introuvable — patch non appliqué");
            else
                WaterWorkFixPlugin.Log.Info("[VeinPatch] Building.OnTick hookée");
            return method!;
        }

        [HarmonyPrefix]
        public static void Prefix(Building __instance, ref VeinPatchState __state)
        {
            __state = default;
            __state.SavedPipesProduction = -1f;

            try
            {
                if (__instance == null) return;
                var bt = __instance.buildingType;
                if (bt == null) return;

                string key = bt.key ?? "";
                if (!VeinBehaviorsConfig.TryGet(key, out var cfg)) return;

                __state.InConfig       = true;
                __state.SuppressBlock  = cfg.SuppressBlock;
                __state.ConsumeVein    = cfg.ConsumeVein;
                __state.GatePipes      = cfg.GatePipes;
                __state.ConsumptionRate = cfg.ConsumptionRate;
                __state.MeltRate       = cfg.MeltWaterRatePerDegree;

                if (cfg.SuppressBlock)
                {
                    __state.SavedExtractionLevel = bt.extractionLevel;
                    bt.extractionLevel = 0;
                }

                if (cfg.GatePipes)
                {
                    var vein = __instance.vein;
                    bool veinEmpty = vein != null && !vein.infinite && vein.quantity <= 0f;
                    if (veinEmpty || !__instance.powered)
                    {
                        __state.SavedPipesProduction = bt.pipesProduction;
                        bt.pipesProduction = 0f;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[VeinPatch] Prefix error: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Building __instance, float deltaDays, VeinPatchState __state)
        {
            try
            {
                // Always restore first, before any early return
                if (__state.InConfig && __instance?.buildingType != null)
                {
                    if (__state.SuppressBlock)
                        __instance.buildingType.extractionLevel = __state.SavedExtractionLevel;
                    if (__state.SavedPipesProduction >= 0f)
                        __instance.buildingType.pipesProduction = __state.SavedPipesProduction;
                }

                if (!__state.InConfig || __instance == null) return;
                if (!__instance.powered) return;

                var bt = __instance.buildingType;
                if (bt == null) return;

                var vein = __instance.vein;

                // Custom vein drain (for buildings whose vanilla tick skips it)
                if (__state.ConsumeVein && vein != null && !vein.infinite && vein.quantity > 0f
                    && __instance.workProgress >= 1f)
                {
                    float rate = __state.ConsumptionRate < 0f ? bt.progressPerDay : __state.ConsumptionRate;
                    vein.quantity = Math.Max(0f, vein.quantity - rate * deltaDays);
                }

                // Ice melt → planet waterStock
                if (!VeinBehaviorsConfig.MeltWater || __state.MeltRate <= 0f) return;
                if (vein != null && !vein.infinite && vein.quantity <= 0f) return; // vein exhausted

                _planet ??= PlanetWrapper.GetCurrent();
                if (_planet == null) return;

                float excessK = _planet.GetAverageTemperature() - FREEZING_K;
                if (excessK <= 0f) return;

                _planet.AddWaterStock(excessK * __state.MeltRate * deltaDays);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[VeinPatch] Postfix error: {ex.Message}");
            }
        }
    }
}
