# MODLOG — ZED ZONE: Expanded Portable Appliances

## v1.1.0 (2026-11-27)
- Added support for the **Stationary Battery Charger Bench (`ID 126`, `TerrainObject_Production_BatteryCharger`)**:
  - Expands grid capacity from vanilla **`4×4` (16 slots)** to **`20×26` (520 slots)** by default, matching the three portable appliances.
  - Separate config section `[StationaryBatteryCharger]` (`Enabled`, `Width` `4–40`, `Height` `4–100`) in `BepInEx\config\com.druid.zedzone.expandedappliances.cfg`.
  - Automatic live-save upgrade of already-built stations (including unloaded chunks) via `ProductionManager.OnLoadGame`, `ProductionManager.AddProductionData`, `TerrainObject_Production_Inventory.Init` / `OnBuildFinish` / `LootInventory`, `TerrainObject_Production_BatteryCharger.OnInit_ProductionObject`, and `NetContainerSync.ResolveContainerInventory` (co-op host support).
  - **Expanded charging & LED indicator fix:** guards `ProductionData.SetBatteryState` / `GetBatteryState` against native 16-slot `ArgumentOutOfRangeException` so batteries placed anywhere on the `20×26` grid charge normally without breaking `ProductionManager.UpdateBatteryCharger`, and mirrors overflow battery charging/charged states onto the 16 world LED indicators via `SyncExpandedChargerIndicators`.
  - Verified on ZED ZONE Patch `1.0.23` (`15/15` Harmony patches `failed=0`, `19/19` headless engine suite + `10/10` graphical live-world `Gaming` suite in isolated save lab with visual `LootInventoryPanel 20×26` check, `8 386/8 386` real `GameData` files unchanged).

## v1.0.0 (2026-11-22)
- Initial implementation for ZED ZONE `1.0.20` (BepInEx 6 IL2CPP / `ZedModsLoader`).
- Expands grid capacity for the three portable container appliances (matching screenshot `20x26` by default, fully configurable via `BepInEx\config\com.druid.zedzone.expandedappliances.cfg`):
  - **Portable Mini Fridge (`1063`)**: vanilla `8x6` -> default `20x26` (`520` slots).
  - **Portable Charger Box (`1066`)**: vanilla `8x4` -> default `20x26` (`520` slots).
  - **Solar Charger Box (`1067`)**: vanilla `8x4` -> default `20x26` (`520` slots).
- Automatic live-save upgrade without losing stored items via `ItemManager.Init`, `BasicItemUI.PrepareBackpackInventory`, `PortableApplianceTicker.ApplyInventoryLimit`, `InventoryData.RepairLoadedItems`, and `ItemManager.GetRandomDataByAttr`.
- Safe shrink protection (`AllowSafeShrink=false` by default; when enabled, never shrinks below occupied item bounds).
