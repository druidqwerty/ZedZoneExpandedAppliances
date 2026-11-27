using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ZedZone.ExpandedAppliances.Patches
{
    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.Init))]
    internal static class ItemManager_Init_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ItemManager __instance)
        {
            try
            {
                ApplianceCapacityManager.ApplyAllItemDefinitions(__instance, ExpandedAppliancesPlugin.LogSource, force: true);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[ItemManager.Init Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(BasicItemUI), nameof(BasicItemUI.PrepareBackpackInventory), new[] { typeof(ItemData) })]
    internal static class BasicItemUI_PrepareBackpackInventory_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ItemData __0)
        {
            try
            {
                ApplianceCapacityManager.EnsureApplianceItemUpgraded(__0);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[PrepareBackpackInventory Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PortableApplianceTicker), nameof(PortableApplianceTicker.ApplyInventoryLimit), new[] { typeof(ItemData), typeof(ItemAttr) })]
    internal static class PortableApplianceTicker_ApplyInventoryLimit_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ItemData __0)
        {
            try
            {
                ApplianceCapacityManager.EnsureApplianceItemUpgraded(__0);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[ApplyInventoryLimit Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryData), nameof(InventoryData.RepairLoadedItems))]
    internal static class InventoryData_RepairLoadedItems_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(InventoryData __instance)
        {
            try
            {
                ApplianceCapacityManager.UpgradeNestedAppliancesInInventory(__instance, maxDepth: 4);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[RepairLoadedItems Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.GetRandomDataByAttr), new[] { typeof(ItemAttr), typeof(bool), typeof(bool) })]
    internal static class ItemManager_GetRandomDataByAttr_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ItemData __result)
        {
            try
            {
                ApplianceCapacityManager.EnsureApplianceItemUpgraded(__result);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[GetRandomDataByAttr Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(TerrainObject_Production_Inventory), nameof(TerrainObject_Production_Inventory.Init))]
    internal static class TerrainObject_Production_Inventory_Init_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Prefix(TerrainObject_Production_Inventory __instance)
        {
            try
            {
                ApplianceCapacityManager.ApplyStationaryChargerPrefabSize(__instance);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[Production_Inventory.Init Prefix] {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(TerrainObject_Production_Inventory __instance)
        {
            try
            {
                ApplianceCapacityManager.EnsureStationaryChargerUpgraded(__instance);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[Production_Inventory.Init Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(TerrainObject_Production_Inventory), nameof(TerrainObject_Production_Inventory.OnBuildFinish))]
    internal static class TerrainObject_Production_Inventory_OnBuildFinish_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Prefix(TerrainObject_Production_Inventory __instance)
        {
            try
            {
                ApplianceCapacityManager.ApplyStationaryChargerPrefabSize(__instance);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[Production_Inventory.OnBuildFinish Prefix] {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(TerrainObject_Production_Inventory __instance)
        {
            try
            {
                ApplianceCapacityManager.EnsureStationaryChargerUpgraded(__instance);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[Production_Inventory.OnBuildFinish Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(TerrainObject_Production_Inventory), nameof(TerrainObject_Production_Inventory.LootInventory), new[] { typeof(Il2CppSystem.Object) })]
    internal static class TerrainObject_Production_Inventory_LootInventory_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Prefix(TerrainObject_Production_Inventory __instance)
        {
            try
            {
                ApplianceCapacityManager.EnsureStationaryChargerUpgraded(__instance);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[Production_Inventory.LootInventory Prefix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(TerrainObject_Production_BatteryCharger), nameof(TerrainObject_Production_BatteryCharger.OnInit_ProductionObject))]
    internal static class TerrainObject_Production_BatteryCharger_OnInit_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(TerrainObject_Production_BatteryCharger __instance)
        {
            try
            {
                ApplianceCapacityManager.EnsureStationaryChargerUpgraded(__instance);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[BatteryCharger.OnInit_ProductionObject Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ProductionManager), nameof(ProductionManager.OnLoadGame))]
    internal static class ProductionManager_OnLoadGame_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ProductionManager __instance)
        {
            try
            {
                ApplianceCapacityManager.UpgradeAllStationaryChargers(__instance, ExpandedAppliancesPlugin.LogSource);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[ProductionManager.OnLoadGame Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ProductionManager), nameof(ProductionManager.AddProductionData), new[] { typeof(ProductionData) })]
    internal static class ProductionManager_AddProductionData_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ProductionData __0)
        {
            try
            {
                ApplianceCapacityManager.EnsureStationaryChargerProductionDataUpgraded(__0);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[ProductionManager.AddProductionData Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ZedZone.Net.NetContainerSync), nameof(ZedZone.Net.NetContainerSync.ResolveContainerInventory), new[] { typeof(TerrainObject) })]
    internal static class NetContainerSync_ResolveContainerInventory_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(TerrainObject __0, InventoryData __result)
        {
            try
            {
                if (ReferenceEquals(__0, null) || __0.Pointer == IntPtr.Zero)
                    return;
                if (ReferenceEquals(__result, null) || __result.Pointer == IntPtr.Zero)
                    return;

                var charger = __0.TryCast<TerrainObject_Production_BatteryCharger>();
                if (ReferenceEquals(charger, null) || charger.Pointer == IntPtr.Zero)
                    return;

                if (ApplianceConfig.TryGetStationaryChargerSize(out int targetW, out int targetH))
                {
                    ApplianceCapacityManager.EnsureInventorySize(__result, targetW, targetH);
                }
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[NetContainerSync.ResolveContainerInventory Postfix] {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ProductionData), nameof(ProductionData.SetBatteryState), new[] { typeof(int), typeof(int) })]
    internal static class ProductionData_SetBatteryState_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Prefix(int __0, int __1)
        {
            // Native ProductionData packs 16 x 2-bit LED states into a 32-bit uint and throws
            // ArgumentOutOfRangeException for slot index >= 16, aborting UpdateBatteryCharger for the entire station.
            if (__0 < 0 || __0 >= ApplianceCapacityManager.NativeBatteryIndicatorSlots || __1 < 0 || __1 > 2)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(ProductionData), nameof(ProductionData.GetBatteryState), new[] { typeof(int) })]
    internal static class ProductionData_GetBatteryState_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Prefix(int __0, ref int __result)
        {
            if (__0 < 0 || __0 >= ApplianceCapacityManager.NativeBatteryIndicatorSlots)
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ProductionManager), nameof(ProductionManager.UpdateBatteryCharger), new[] { typeof(ProductionData), typeof(float) })]
    internal static class ProductionManager_UpdateBatteryCharger_Patch
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ProductionData __0)
        {
            try
            {
                ApplianceCapacityManager.SyncExpandedChargerIndicators(__0);
            }
            catch (Exception ex)
            {
                ExpandedAppliancesPlugin.LogSource?.LogWarning($"[ProductionManager.UpdateBatteryCharger Postfix] {ex.Message}");
            }
        }
    }
}
