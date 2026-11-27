using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ZedZone.ExpandedAppliances
{
    internal static class ApplianceCapacityManager
    {
        private static readonly int[] TargetItemIds =
        {
            ApplianceConfig.PortableFridgeId,
            ApplianceConfig.PortableChargerId,
            ApplianceConfig.SolarChargerId
        };

        private static IntPtr _lastAppliedItemManagerPtr = IntPtr.Zero;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ApplyAllItemDefinitions(ItemManager itemManager, ManualLogSource log, bool force = false)
        {
            if (ReferenceEquals(itemManager, null) || ((Il2CppObjectBase)itemManager).Pointer == IntPtr.Zero)
                return 0;

            var ptr = ((Il2CppObjectBase)itemManager).Pointer;
            if (!force && ptr == _lastAppliedItemManagerPtr)
                return 0;

            var dic = itemManager.itemAttrDic;
            if (ReferenceEquals(dic, null) || dic.Pointer == IntPtr.Zero)
                return 0;

            int updated = 0;
            for (int i = 0; i < TargetItemIds.Length; i++)
            {
                int itemId = TargetItemIds[i];
                if (!ApplianceConfig.TryGetTargetSize(itemId, out int targetW, out int targetH))
                    continue;

                if (!dic.ContainsKey(itemId))
                    continue;

                var rawAttr = itemManager.GetItemAttrById(itemId);
                if (ReferenceEquals(rawAttr, null) || rawAttr.Pointer == IntPtr.Zero)
                    continue;

                var bagAttr = rawAttr.TryCast<ItemAttr_Backpack>();
                if (ReferenceEquals(bagAttr, null) || bagAttr.Pointer == IntPtr.Zero)
                    continue;

                var prev = bagAttr.inventorySize;
                if (prev.x != targetW || prev.y != targetH)
                {
                    bagAttr.inventorySize = new Vector2Int(targetW, targetH);
                    updated++;
                    log?.LogInfo($"[ExpandedAppliances] ItemAttr_Backpack({itemId}): {prev.x}x{prev.y} -> {targetW}x{targetH}.");
                }
            }

            _lastAppliedItemManagerPtr = ptr;
            return updated;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool EnsureApplianceItemUpgraded(ItemData item)
        {
            if (ReferenceEquals(item, null) || item.Pointer == IntPtr.Zero)
                return false;

            if (!ApplianceConfig.TryGetTargetSize(item.itemId, out int targetW, out int targetH))
                return false;

            var inv = item.inventoryData;
            if (ReferenceEquals(inv, null) || inv.Pointer == IntPtr.Zero)
                return false;

            return EnsureInventorySize(inv, targetW, targetH);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int UpgradeNestedAppliancesInInventory(InventoryData root, int maxDepth = 4)
        {
            if (ReferenceEquals(root, null) || root.Pointer == IntPtr.Zero)
                return 0;

            var visited = new HashSet<IntPtr>();
            return UpgradeNestedRecursive(root, visited, maxDepth);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int UpgradeNestedRecursive(InventoryData inv, HashSet<IntPtr> visited, int remainingDepth)
        {
            if (remainingDepth < 0 || ReferenceEquals(inv, null) || inv.Pointer == IntPtr.Zero)
                return 0;

            if (!visited.Add(inv.Pointer))
                return 0;

            var items = inv.itemList;
            if (ReferenceEquals(items, null) || items.Pointer == IntPtr.Zero)
                return 0;

            int upgraded = 0;
            int count = items.Count;
            for (int i = 0; i < count; i++)
            {
                var item = items[i];
                if (ReferenceEquals(item, null) || item.Pointer == IntPtr.Zero)
                    continue;

                if (EnsureApplianceItemUpgraded(item))
                    upgraded++;

                var subInv = item.inventoryData;
                if (!ReferenceEquals(subInv, null) && subInv.Pointer != IntPtr.Zero && remainingDepth > 0)
                {
                    upgraded += UpgradeNestedRecursive(subInv, visited, remainingDepth - 1);
                }
            }

            return upgraded;
        }

        public const int NativeBatteryIndicatorSlots = 16;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ApplyStationaryChargerPrefabSize(TerrainObject_Production_Inventory prodInv)
        {
            if (ReferenceEquals(prodInv, null) || prodInv.Pointer == IntPtr.Zero)
                return;

            var charger = prodInv.TryCast<TerrainObject_Production_BatteryCharger>();
            if (ReferenceEquals(charger, null) || charger.Pointer == IntPtr.Zero)
                return;

            if (!ApplianceConfig.TryGetStationaryChargerSize(out int targetW, out int targetH))
                return;

            var cur = charger.inventorySize;
            bool allowShrink = ApplianceConfig.AllowSafeShrink != null && ApplianceConfig.AllowSafeShrink.Value;
            int desiredX = allowShrink ? targetW : Math.Max(cur.x, targetW);
            int desiredY = allowShrink ? targetH : Math.Max(cur.y, targetH);

            if (cur.x != desiredX || cur.y != desiredY)
            {
                charger.inventorySize = new Vector2Int(desiredX, desiredY);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool EnsureStationaryChargerUpgraded(TerrainObject_Production_Inventory prodInv)
        {
            if (ReferenceEquals(prodInv, null) || prodInv.Pointer == IntPtr.Zero)
                return false;

            var charger = prodInv.TryCast<TerrainObject_Production_BatteryCharger>();
            if (ReferenceEquals(charger, null) || charger.Pointer == IntPtr.Zero)
                return false;

            ApplyStationaryChargerPrefabSize(charger);

            var objData = charger.objectData;
            if (ReferenceEquals(objData, null) || objData.Pointer == IntPtr.Zero)
                return false;

            var pd = objData.productionData;
            return EnsureStationaryChargerProductionDataUpgraded(pd);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool EnsureStationaryChargerProductionDataUpgraded(ProductionData pd)
        {
            if (ReferenceEquals(pd, null) || pd.Pointer == IntPtr.Zero)
                return false;

            if (pd.productionObjectType != ProductionObjectType.BatteryCharger)
                return false;

            if (!ApplianceConfig.TryGetStationaryChargerSize(out int targetW, out int targetH))
                return false;

            var inv = pd.inventoryData1;
            if (ReferenceEquals(inv, null) || inv.Pointer == IntPtr.Zero)
                return false;

            return EnsureInventorySize(inv, targetW, targetH);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int UpgradeAllStationaryChargers(ProductionManager pm, ManualLogSource log)
        {
            if (ReferenceEquals(pm, null) || pm.Pointer == IntPtr.Zero)
                return 0;

            if (!ApplianceConfig.TryGetStationaryChargerSize(out _, out _))
                return 0;

            var list = pm.productionDataList;
            if (ReferenceEquals(list, null) || list.Pointer == IntPtr.Zero)
                return 0;

            int upgraded = 0;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var pd = list[i];
                if (EnsureStationaryChargerProductionDataUpgraded(pd))
                {
                    upgraded++;
                }
            }

            if (upgraded > 0)
            {
                log?.LogInfo($"[ExpandedAppliances] Upgraded {upgraded} stationary Battery Charger(s) on load.");
            }

            return upgraded;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SyncExpandedChargerIndicators(ProductionData pd)
        {
            if (ReferenceEquals(pd, null) || pd.Pointer == IntPtr.Zero || !pd.craftFinishFlag)
                return;

            var inv = pd.inventoryData1;
            if (ReferenceEquals(inv, null) || inv.Pointer == IntPtr.Zero)
                return;

            int width = inv.inventorySizeX;
            int height = inv.inventorySizeY;
            if (width <= 0 || height <= 0 || width * height <= NativeBatteryIndicatorSlots)
                return;

            var items = inv.itemList;
            if (ReferenceEquals(items, null) || items.Pointer == IntPtr.Zero || items.Count == 0)
                return;

            var im = ItemManager.instance;
            if (ReferenceEquals(im, null) || im.Pointer == IntPtr.Zero)
                return;

            int overflowCharging = 0;
            int overflowCharged = 0;
            int count = items.Count;
            for (int i = 0; i < count; i++)
            {
                var item = items[i];
                if (ReferenceEquals(item, null) || item.Pointer == IntPtr.Zero)
                    continue;

                var attr = im.GetItemAttrById(item.itemId);
                if (ReferenceEquals(attr, null) || attr.Pointer == IntPtr.Zero)
                    continue;

                var features = attr.itemFeatures;
                if (ReferenceEquals(features, null) || features.Pointer == IntPtr.Zero || !features.Contains(ItemFeatureType.Battery))
                    continue;

                var batteryFeature = attr.GetItemFeatureByFeatureType(ItemFeatureType.Battery)?.TryCast<ItemFeature_Battery>();
                if (ReferenceEquals(batteryFeature, null) || batteryFeature.Pointer == IntPtr.Zero)
                    continue;

                var coord = item.inventoryCoordinate;
                int slotIndex = coord.y * width + coord.x;
                if (slotIndex < NativeBatteryIndicatorSlots)
                    continue;

                float remaining = ItemFeature_Battery.GetBatteryRemainingPower(item);
                if (remaining < batteryFeature.batteryCapacityFloat)
                    overflowCharging++;
                else
                    overflowCharged++;
            }

            if (overflowCharging == 0 && overflowCharged == 0)
                return;

            bool anyChargingLed = false;
            for (int s = 0; s < NativeBatteryIndicatorSlots; s++)
            {
                int cur = pd.GetBatteryState(s);
                if (cur == 2)
                {
                    anyChargingLed = true;
                    continue;
                }

                if (cur == 0)
                {
                    if (overflowCharging > 0)
                    {
                        pd.SetBatteryState(s, 2);
                        overflowCharging--;
                        anyChargingLed = true;
                    }
                    else if (overflowCharged > 0)
                    {
                        pd.SetBatteryState(s, 1);
                        overflowCharged--;
                    }
                }
            }

            if (overflowCharging > 0 && !anyChargingLed)
            {
                pd.SetBatteryState(NativeBatteryIndicatorSlots - 1, 2);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool EnsureInventorySize(InventoryData inv, int targetW, int targetH)
        {
            if (ReferenceEquals(inv, null) || inv.Pointer == IntPtr.Zero)
                return false;

            int curX = inv.inventorySizeX;
            int curY = inv.inventorySizeY;

            bool allowShrink = ApplianceConfig.AllowSafeShrink != null && ApplianceConfig.AllowSafeShrink.Value;
            int desiredX = allowShrink ? targetW : Math.Max(curX, targetW);
            int desiredY = allowShrink ? targetH : Math.Max(curY, targetH);

            if (allowShrink && (desiredX < curX || desiredY < curY))
            {
                ComputeOccupiedBounds(inv, out int minRequiredX, out int minRequiredY);
                if (desiredX < minRequiredX) desiredX = minRequiredX;
                if (desiredY < minRequiredY) desiredY = minRequiredY;
            }

            if (curX == desiredX && curY == desiredY)
                return false;

            inv.inventorySize = new Vector2Int(desiredX, desiredY);
            inv.SetOccupyDataDirty();
            inv.weightMemoDirty = true;

            try
            {
                inv.onChangeAction?.Invoke(inv);
            }
            catch
            {
                // Ignore UI callback errors during background load
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ComputeOccupiedBounds(InventoryData inv, out int minX, out int minY)
        {
            minX = 1;
            minY = 1;

            var items = inv.itemList;
            if (ReferenceEquals(items, null) || items.Pointer == IntPtr.Zero)
                return;

            var im = ItemManager.instance;
            int count = items.Count;
            for (int i = 0; i < count; i++)
            {
                var item = items[i];
                if (ReferenceEquals(item, null) || item.Pointer == IntPtr.Zero)
                    continue;

                var coord = item.inventoryCoordinate;
                int w = 1;
                int h = 1;
                if (!ReferenceEquals(im, null) && im.Pointer != IntPtr.Zero)
                {
                    var attr = im.GetItemAttrById(item.itemId);
                    if (!ReferenceEquals(attr, null) && attr.Pointer != IntPtr.Zero)
                    {
                        var sz = attr.itemSize;
                        w = Math.Max(1, item.rotateFlag ? sz.y : sz.x);
                        h = Math.Max(1, item.rotateFlag ? sz.x : sz.y);
                    }
                }

                int right = coord.x + w;
                int bottom = coord.y + h;
                if (right > minX) minX = right;
                if (bottom > minY) minY = bottom;
            }
        }
    }
}
