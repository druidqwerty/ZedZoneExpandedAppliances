using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ZedZone.ExpandedAppliances
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ExpandedAppliancesPlugin : BasePlugin
    {
        public const string PluginGuid = "com.druid.zedzone.expandedappliances";
        public const string PluginName = "Expanded Portable Appliances";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource LogSource;
        internal static Harmony HarmonyInstance;

        public override void Load()
        {
            LogSource = Log;
            ApplianceConfig.Bind(Config);

            HarmonyInstance = new Harmony(PluginGuid);
            int applied = SafeHarmonyPatcher.PatchAssemblySafely(HarmonyInstance, Assembly.GetExecutingAssembly(), LogSource);

            // In case ItemManager was already initialized before plugin load
            try
            {
                var im = ItemManager.instance;
                if (!ReferenceEquals(im, null) && im.Pointer != IntPtr.Zero)
                {
                    ApplianceCapacityManager.ApplyAllItemDefinitions(im, LogSource, force: true);
                }
            }
            catch
            {
                // Will be applied in ItemManager.Init Postfix
            }

            LogSource.LogInfo(
                $"[{PluginName} v{PluginVersion}] Loaded ({applied} Harmony target(s)). " +
                $"Targets: Fridge(1063)={ApplianceConfig.FridgeWidth.Value}x{ApplianceConfig.FridgeHeight.Value}, " +
                $"Charger(1066)={ApplianceConfig.PortableChargerWidth.Value}x{ApplianceConfig.PortableChargerHeight.Value}, " +
                $"SolarCharger(1067)={ApplianceConfig.SolarChargerWidth.Value}x{ApplianceConfig.SolarChargerHeight.Value}.");
        }
    }
}
