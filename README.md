# WaterWorkFix

Companion C# plugin for the **WaterWork** YAML mod (Steam Workshop: `3359421244`).

Fixes three engine-level bugs affecting Ice Treatment buildings and adds an ice-melt mechanic to water mines and ice treatment plants.

## What it fixes

| Bug | Effect without fix |
|-----|-------------------|
| Block suppression | Ice Treatment plants produce resource_water blocks despite `outputResource: null` |
| Vein drain | Ice Treatment plants never consume the water vein — vein stays full forever |
| Pipe gating | Ice Treatment plants keep producing pipes when unpowered or vein is empty |

## Bonus: ice melt

When Mars average temperature > 0 °C, buildings listed in the config contribute to `planet.waterStock` at a configurable rate per degree per day.

Default rates (editable in `BepInEx/config/PerAspera.VeinBehaviors.json`):

| Building | Rate per °C/day |
|---------|----------------|
| Water Mine T1/T2/T3 | 0.003 / 0.006 / 0.010 |
| Ice Treatment T1/T2/T3 | 0.0005 / 0.001 / 0.002 |
| Crater Lake | 0.008 |

## Installation

1. **BepInEx** — Install [BepInEx 6 Bleeding Edge](https://builds.bepinex.dev/projects/bepinex_be) (UnityIL2CPP x64) into your Per Aspera folder. Launch the game once to generate BepInEx folders, then close it.

2. **Per Aspera SDK** — Download `PerAspera-SDK-vX.X.zip` from the [SDK releases](https://github.com/PerAsperaMods/PerAspera-SDK/releases), extract and place the `plugins/SDK/` folder into `BepInEx/plugins/`.

3. **WaterWorkFix** — Place `WaterWorkFix.dll` (from [releases](https://github.com/PerAsperaMods/WaterWorkFix/releases)) into `BepInEx/plugins/`.

4. **WaterWork YAML mod** — Subscribe on [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3359421244) (ID `3359421244`), or manually place the `WaterWork/` folder in `Per Aspera_Data/StreamingAssets/Mods/`.

```
BepInEx/plugins/
  SDK/
    PerAspera.Core.dll
    PerAspera.GameAPI.Wrappers.dll
    ...
  WaterWorkFix.dll
```

A popup will appear on day 1 if the companion DLL is missing.

## Configuration

Edit `BepInEx/config/PerAspera.VeinBehaviors.json` to adjust rates or add custom buildings.
Set `"meltWater": false` to disable the ice-melt mechanic entirely.

## Related

- [WaterWork YAML mod](https://github.com/PerAsperaMods/WaterWork) — the YAML mod this plugin companions
- [Per Aspera SDK](https://github.com/PerAsperaMods/PerAspera-SDK) — modding SDK used by this plugin
