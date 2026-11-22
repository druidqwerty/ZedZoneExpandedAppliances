# MODLOG — ZED ZONE: Expanded Portable Appliances

## v1.0.0 (2026-11-22)
- Initial implementation for ZED ZONE `1.0.20` (BepInEx 6 IL2CPP / `ZedModsLoader`).
- Expands grid capacity for the three portable container appliances (matching screenshot `20x26` by default, fully configurable via `BepInEx\config\com.druid.zedzone.expandedappliances.cfg`):
  - **Portable Mini Fridge (`1063`)**: vanilla `8x6` -> default `20x26` (`520` slots).
  - **Portable Charger Box (`1066`)**: vanilla `8x4` -> default `20x26` (`520` slots).
  - **Solar Charger Box (`1067`)**: vanilla `8x4` -> default `20x26` (`520` slots).
- Automatic live-save upgrade without losing stored items via `ItemManager.Init`, `BasicItemUI.PrepareBackpackInventory`, `PortableApplianceTicker.ApplyInventoryLimit`, `InventoryData.RepairLoadedItems`, and `ItemManager.GetRandomDataByAttr`.
- Safe shrink protection (`AllowSafeShrink=false` by default; when enabled, never shrinks below occupied item bounds).
