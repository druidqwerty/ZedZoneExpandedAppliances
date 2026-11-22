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
}
