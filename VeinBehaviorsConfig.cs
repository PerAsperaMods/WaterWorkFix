using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PerAspera.WaterWorkFix
{
    internal sealed class BuildingVeinConfig
    {
        [JsonPropertyName("buildingKey")]           public string BuildingKey            { get; set; } = "";

        // BUG 1 : zeroes extractionLevel before native OnTick to prevent unwanted block production
        [JsonPropertyName("suppressBlock")]         public bool   SuppressBlock          { get; set; } = false;

        // BUG 2 : custom vein drain in Postfix (vanilla mine tick skipped for these buildings)
        [JsonPropertyName("consumeVein")]           public bool   ConsumeVein            { get; set; } = false;

        // -1 = use building's progressPerDay; set a positive value to override
        [JsonPropertyName("consumptionRate")]       public float  ConsumptionRate        { get; set; } = -1f;

        // BUG 3 : zeroes pipesProduction when building is unpowered or vein is empty
        [JsonPropertyName("gatePipes")]             public bool   GatePipes              { get; set; } = false;

        // BONUS : waterStock added per degree above 0°C, per building, per day
        [JsonPropertyName("meltWaterRatePerDegree")] public float MeltWaterRatePerDegree { get; set; } = 0f;
    }

    internal sealed class VeinBehaviorsRoot
    {
        [JsonPropertyName("meltWater")] public bool MeltWater { get; set; } = true;
        [JsonPropertyName("buildings")] public List<BuildingVeinConfig> Buildings { get; set; } = new();
    }

    internal static class VeinBehaviorsConfig
    {
        private const string ConfigFileName = "PerAspera.VeinBehaviors.json";

        private static VeinBehaviorsRoot _root = new();
        private static Dictionary<string, BuildingVeinConfig> _index = new(StringComparer.OrdinalIgnoreCase);

        public static bool MeltWater => _root.MeltWater;

        public static bool TryGet(string buildingKey, out BuildingVeinConfig cfg)
            => _index.TryGetValue(buildingKey, out cfg!);

        public static void Load(string bepInExConfigDir)
        {
            string path = Path.Combine(bepInExConfigDir, ConfigFileName);

            if (!File.Exists(path))
            {
                WriteDefaults(path);
                WaterWorkFixPlugin.Log.Info($"[VeinBehaviors] Config créée : {path}");
            }

            try
            {
                string json = File.ReadAllText(path);
                _root = JsonSerializer.Deserialize<VeinBehaviorsRoot>(json) ?? new VeinBehaviorsRoot();
            }
            catch (Exception ex)
            {
                WaterWorkFixPlugin.Log.Warning($"[VeinBehaviors] Erreur lecture config, defaults utilisés : {ex.Message}");
                _root = DefaultRoot();
            }

            _index.Clear();
            foreach (var b in _root.Buildings)
                if (!string.IsNullOrEmpty(b.BuildingKey))
                    _index[b.BuildingKey] = b;

            WaterWorkFixPlugin.Log.Info($"[VeinBehaviors] {_index.Count} bâtiment(s) configuré(s), meltWater={_root.MeltWater}");
        }

        private static void WriteDefaults(string path)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(DefaultRoot(), opts));
        }

        private static VeinBehaviorsRoot DefaultRoot() => new()
        {
            MeltWater = true,
            Buildings = new List<BuildingVeinConfig>
            {
                // Crater lake — humidification (melt only, vanilla handles inputs)
                new() { BuildingKey = "building_crater_lake", SuppressBlock = false, ConsumeVein = false, GatePipes = false, MeltWaterRatePerDegree = 0.008f },

                // Vanilla water mines — melt effect only (vanilla handles vein drain and block production)
                new() { BuildingKey = "building_water_mine",   SuppressBlock = false, ConsumeVein = false, GatePipes = false, MeltWaterRatePerDegree = 0.003f },
                new() { BuildingKey = "building_water_mine_2", SuppressBlock = false, ConsumeVein = false, GatePipes = false, MeltWaterRatePerDegree = 0.006f },
                new() { BuildingKey = "building_water_mine_3", SuppressBlock = false, ConsumeVein = false, GatePipes = false, MeltWaterRatePerDegree = 0.010f },

                // Ice treatment plants — block suppression + vein drain + pipe gating + minor melt
                new() { BuildingKey = "ice_mine_purification_plant",   SuppressBlock = true, ConsumeVein = true, GatePipes = true, MeltWaterRatePerDegree = 0.0005f },
                new() { BuildingKey = "ice_mine_purification_plant_2", SuppressBlock = true, ConsumeVein = true, GatePipes = true, MeltWaterRatePerDegree = 0.001f  },
                new() { BuildingKey = "ice_mine_purification_plant_3", SuppressBlock = true, ConsumeVein = true, GatePipes = true, MeltWaterRatePerDegree = 0.002f  },
            }
        };
    }
}
