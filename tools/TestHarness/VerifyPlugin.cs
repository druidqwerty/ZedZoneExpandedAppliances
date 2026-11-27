using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using ZedZone.ExpandedAppliances;

namespace ZExpandedAppliancesVerify
{
    [BepInPlugin("com.druid.zedzone.expandedappliances.verify", "ZExpandedAppliancesVerify", "1.1.0")]
    [BepInDependency(ExpandedAppliancesPlugin.PluginGuid)]
    public sealed class VerifyPlugin : BasePlugin
    {
        internal static ManualLogSource LogSrc;
        internal static string HeadlessReportPath = @"F:\DeepSeek Harness workflows\MODS\projects\zedzone-expanded-appliances\tools\TestHarness\verify_report.txt";
        internal static string LiveReportPath = @"F:\DeepSeek Harness workflows\MODS\projects\zedzone-expanded-appliances\tools\TestHarness\live_verify_report.txt";
        internal static string ScreenshotDir = @"F:\DeepSeek Harness workflows\MODS\projects\zedzone-expanded-appliances\tools\TestHarness\screenshots";
        internal static string Lab;
        internal static string Original;
        internal static string OriginalBackslash;
        internal static string OriginalForward;
        internal static bool IsLiveLab;

        public override void Load()
        {
            LogSrc = Log;
            var args = Environment.GetCommandLineArgs();
            var labArg = args.FirstOrDefault(a => a.StartsWith("--ea-live-lab=", StringComparison.OrdinalIgnoreCase));
            if (labArg != null)
            {
                IsLiveLab = true;
                Lab = Path.GetFullPath(labArg.Substring("--ea-live-lab=".Length)).TrimEnd('\\', '/');
                Original = Application.persistentDataPath;
                OriginalBackslash = Path.GetFullPath(Original).TrimEnd('\\', '/');
                OriginalForward = OriginalBackslash.Replace('\\', '/');
                Application.runInBackground = true;
                Directory.CreateDirectory(ScreenshotDir);
                new Harmony("com.druid.zedzone.expandedappliances.verify.live").PatchAll(typeof(VerifyPlugin).Assembly);
                AddComponent<LiveWorldVerifyRunner>();
                Log.LogInfo("[ZExpandedAppliancesVerify] Loaded in LIVE WORLD mode: " + Lab);
            }
            else
            {
                AddComponent<VerifyRunner>();
                Log.LogInfo("[ZExpandedAppliancesVerify] Loaded in HEADLESS SUITE mode.");
            }
        }
    }

    [HarmonyPatch]
    internal static class RedirectSavePaths
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            if (!VerifyPlugin.IsLiveLab) yield break;
            foreach (var m in typeof(GameConfigs).GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(string) && p.Name.EndsWith("Path") && p.GetMethod != null && p.SetMethod == null)
                .Select(p => p.GetMethod))
            {
                yield return m;
            }
        }

        static void Postfix(ref string __result)
        {
            if (!VerifyPlugin.IsLiveLab || string.IsNullOrEmpty(__result)) return;
            if (__result.StartsWith(VerifyPlugin.OriginalBackslash, StringComparison.OrdinalIgnoreCase))
                __result = VerifyPlugin.Lab + __result.Substring(VerifyPlugin.OriginalBackslash.Length);
            else if (__result.StartsWith(VerifyPlugin.OriginalForward, StringComparison.OrdinalIgnoreCase))
                __result = VerifyPlugin.Lab + __result.Substring(VerifyPlugin.OriginalForward.Length);
            else if (__result.StartsWith(VerifyPlugin.Original, StringComparison.OrdinalIgnoreCase))
                __result = VerifyPlugin.Lab + __result.Substring(VerifyPlugin.Original.Length);
        }
    }

    [HarmonyPatch(typeof(SteamCloudManager), nameof(SteamCloudManager.SteamCloudSave))]
    internal static class BlockCloudSave
    {
        static bool Prefix() => !VerifyPlugin.IsLiveLab;
    }

    [HarmonyPatch(typeof(GameController), nameof(GameController.UpdateFocusAutoPause))]
    internal static class KeepUnfocusedRunning
    {
        static void Prefix(ref bool hasFocus)
        {
            if (VerifyPlugin.IsLiveLab) hasFocus = true;
        }
    }

    [HarmonyPatch(typeof(Time), "set_timeScale")]
    internal static class PinTimeScale
    {
        static void Prefix(ref float value)
        {
            if (VerifyPlugin.IsLiveLab && LiveWorldVerifyRunner.WorldReady && value < 1f)
                value = 1f;
        }
    }

    internal static class VerifyHelpers
    {
        public static ItemData MakeBattery(int batteryItemId, int x, int y, float initialWh)
        {
            var b = new ItemData { itemId = batteryItemId, inventoryCoordinate = new Vector2Int(x, y) };
            b.SetProperty("RemainingBattery", initialWh.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return b;
        }

        public static bool FindBatteryItem(ItemManager im, out int batteryId, out float capacity, out string name)
        {
            batteryId = -1;
            capacity = 0f;
            name = "";
            foreach (var kv in im.itemAttrDic)
            {
                var ia = kv.Value;
                if (ReferenceEquals(ia, null) || ia.Pointer == IntPtr.Zero) continue;
                if (ia.itemFeatures == null || !ia.itemFeatures.Contains(ItemFeatureType.Battery)) continue;
                var bf = ia.GetItemFeatureByFeatureType(ItemFeatureType.Battery)?.TryCast<ItemFeature_Battery>();
                if (bf == null || bf.batteryCapacityFloat <= 0f) continue;
                if (ia.itemSize.x == 1 && ia.itemSize.y == 1)
                {
                    batteryId = ia.itemId;
                    capacity = bf.batteryCapacityFloat;
                    name = ia.ItemName_EN ?? $"id_{ia.itemId}";
                    return true;
                }
                if (batteryId < 0)
                {
                    batteryId = ia.itemId;
                    capacity = bf.batteryCapacityFloat;
                    name = ia.ItemName_EN ?? $"id_{ia.itemId}";
                }
            }
            return batteryId > 0 && capacity > 0f;
        }

        public static TerrainObjectAttr FindBatteryChargerAttr(GameController gc, out List<string> details)
        {
            details = new List<string>();
            TerrainObjectAttr found = null;
            foreach (var kv in gc.terrainObjectAttrDic)
            {
                var ta = kv.Value;
                if (ReferenceEquals(ta, null) || ta.Pointer == IntPtr.Zero) continue;
                if (ta.isProductionObject && ta.productionObjectType == ProductionObjectType.BatteryCharger)
                {
                    details.Add($"id={ta.id} en='{ta.englishName}' cn='{ta.chineseName}' watt={ta.electricWattage}");
                    if (found == null) found = ta;
                }
            }
            return found;
        }

        public static void WriteReport(string outPath, List<string> lines, int passed, int failed)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SUMMARY: passed={passed}, failed={failed}");
            foreach (var l in lines) sb.AppendLine(l);
            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            VerifyPlugin.LogSrc?.LogInfo($"[ZExpandedAppliancesVerify] ({Path.GetFileName(outPath)}) SUMMARY: passed={passed}, failed={failed}");
        }
    }

    public sealed class VerifyRunner : MonoBehaviour
    {
        public VerifyRunner(IntPtr ptr) : base(ptr) { }

        private bool _done;
        private float _startTime;

        private void Awake()
        {
            _startTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (_done) return;

            var im = ItemManager.instance;
            var gc = GameController.instance;
            if (ReferenceEquals(im, null) || im.Pointer == IntPtr.Zero || im.itemAttrDic == null || im.itemAttrDic.Count == 0 ||
                ReferenceEquals(gc, null) || gc.Pointer == IntPtr.Zero || gc.terrainObjectAttrDic == null || gc.terrainObjectAttrDic.Count == 0)
            {
                if (Time.realtimeSinceStartup - _startTime > 30f)
                {
                    VerifyHelpers.WriteReport(VerifyPlugin.HeadlessReportPath, new List<string> { "FAIL: Timed out waiting for ItemManager / GameController" }, 0, 1);
                    _done = true;
                    Application.Quit();
                }
                return;
            }

            _done = true;
            var lines = new List<string>();
            int passed = 0;
            int failed = 0;

            void Check(string name, bool cond, string detail)
            {
                if (cond) { passed++; lines.Add($"PASS: {name} ({detail})"); }
                else { failed++; lines.Add($"FAIL: {name} ({detail})"); }
            }

            try
            {
                // PART A: Existing Portable Appliances (1063, 1066, 1067)
                int[] ids = { 1063, 1066, 1067 };
                foreach (int id in ids)
                {
                    var attr = im.GetItemAttrById(id)?.TryCast<ItemAttr_Backpack>();
                    bool ok = attr != null && attr.inventorySize.x == 20 && attr.inventorySize.y == 26;
                    Check($"ItemAttr_{id}", ok, attr == null ? "null" : $"{attr.inventorySize.x}x{attr.inventorySize.y}, isAppliance={attr.isAppliance}");
                }

                foreach (int id in ids)
                {
                    var item = new ItemData { itemId = id };
                    var oldInv = new InventoryData();
                    oldInv.inventorySize = new Vector2Int(8, id == 1063 ? 6 : 4);
                    oldInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                    oldInv.itemList.Add(new ItemData { itemId = 1, inventoryCoordinate = new Vector2Int(2, 1) });
                    item.inventoryData = oldInv;

                    BasicItemUI.PrepareBackpackInventory(item);

                    bool upgraded = item.inventoryData.inventorySizeX == 20 && item.inventoryData.inventorySizeY == 26;
                    bool keptLoot = item.inventoryData.itemList.Count == 1 &&
                                    item.inventoryData.itemList[0].inventoryCoordinate.x == 2 &&
                                    item.inventoryData.itemList[0].inventoryCoordinate.y == 1;
                    item.inventoryData.EnsureOccupyData();
                    bool farCellFree = !item.inventoryData.IsOccupied(new Vector2Int(19, 25));
                    bool oldCellUsed = item.inventoryData.IsOccupied(new Vector2Int(2, 1));
                    Check($"PrepareBackpackInventory_{id}", upgraded && keptLoot && farCellFree && oldCellUsed,
                        $"size={item.inventoryData.inventorySizeX}x{item.inventoryData.inventorySizeY}, count={item.inventoryData.itemList.Count}, farFree={farCellFree}, oldUsed={oldCellUsed}");
                }

                foreach (int id in ids)
                {
                    var attr = im.GetItemAttrById(id);
                    var item = new ItemData { itemId = id };
                    var oldInv = new InventoryData();
                    oldInv.inventorySize = new Vector2Int(8, 4);
                    oldInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                    item.inventoryData = oldInv;

                    PortableApplianceTicker.ApplyInventoryLimit(item, attr);

                    bool upgraded = item.inventoryData.inventorySizeX == 20 && item.inventoryData.inventorySizeY == 26;
                    int limCount = item.inventoryData.itemFeatureLimit != null ? item.inventoryData.itemFeatureLimit.Count : 0;
                    Check($"ApplyInventoryLimit_{id}", upgraded, $"size={item.inventoryData.inventorySizeX}x{item.inventoryData.inventorySizeY}, limitCount={limCount}");
                }

                var parentInv = new InventoryData();
                parentInv.inventorySize = new Vector2Int(12, 20);
                parentInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                foreach (int id in ids)
                {
                    var child = new ItemData { itemId = id, inventoryCoordinate = new Vector2Int(0, 0) };
                    var childInv = new InventoryData();
                    childInv.inventorySize = new Vector2Int(8, 4);
                    childInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                    child.inventoryData = childInv;
                    parentInv.itemList.Add(child);
                }
                parentInv.RepairLoadedItems();

                bool allNestedOk = true;
                var nestedDetails = new StringBuilder();
                for (int i = 0; i < parentInv.itemList.Count; i++)
                {
                    var c = parentInv.itemList[i];
                    if (c.inventoryData.inventorySizeX != 20 || c.inventoryData.inventorySizeY != 26) allNestedOk = false;
                    nestedDetails.Append($"{c.itemId}:{c.inventoryData.inventorySizeX}x{c.inventoryData.inventorySizeY} ");
                }
                Check("RepairLoadedItems_Nested", allNestedOk, nestedDetails.ToString().Trim());

                var sortInv = new InventoryData();
                sortInv.inventorySize = new Vector2Int(20, 26);
                sortInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                sortInv.itemList.Add(new ItemData { itemId = 1, inventoryCoordinate = new Vector2Int(18, 24) });
                sortInv.itemList.Add(new ItemData { itemId = 1, inventoryCoordinate = new Vector2Int(15, 20) });
                sortInv.SetOccupyDataDirty();
                sortInv.EnsureOccupyData();
                bool sortOk = sortInv.TryAutoSortingWithoutOverflow(false, out _);
                Check("AutoSort_20x26", sortOk && sortInv.LastSortingOverflowCount == 0 && sortInv.itemList.Count >= 1,
                    $"sortOk={sortOk}, overflow={sortInv.LastSortingOverflowCount}, count={sortInv.itemList.Count}");

                // PART B: Stationary Battery Charger Attr & Prefab
                var chargerAttr = VerifyHelpers.FindBatteryChargerAttr(gc, out var chargerDetails);
                Check("StationaryCharger_AttrFound", chargerAttr != null,
                    chargerAttr == null ? "none" : string.Join("; ", chargerDetails));

                var prefabGO = chargerAttr != null ? gc.GetTerrainObjectPrefabById(chargerAttr.id) : null;
                var prefabComp = prefabGO != null ? prefabGO.GetComponent<TerrainObject_Production_BatteryCharger>() : null;
                Check("StationaryCharger_PrefabFound", prefabComp != null,
                    prefabComp == null ? "null" : $"vanillaSize={prefabComp.inventorySize.x}x{prefabComp.inventorySize.y}, totalBatterySoltNumber={prefabComp.totalBatterySoltNumber}, chargingStateSRs={prefabComp.chargingStateSRs?.Length}");

                bool hasBat = VerifyHelpers.FindBatteryItem(im, out int batteryId, out float batteryCap, out string batteryName);
                Check("BatteryItem_Found", hasBat, $"id={batteryId}, name='{batteryName}', cap={batteryCap}Wh");

                // PART C: SetBatteryState / GetBatteryState Bounds Safety
                var boundsPd = new ProductionData();
                boundsPd.SetBatteryState(0, 2);
                boundsPd.SetBatteryState(15, 1);
                boundsPd.SetBatteryState(16, 2);
                boundsPd.SetBatteryState(498, 2);
                boundsPd.SetBatteryState(-1, 2);
                int s0 = boundsPd.GetBatteryState(0);
                int s15 = boundsPd.GetBatteryState(15);
                int s16 = boundsPd.GetBatteryState(16);
                int s498 = boundsPd.GetBatteryState(498);
                int sNeg = boundsPd.GetBatteryState(-1);
                Check("ProductionData_BatteryState_BoundsSafe",
                    s0 == 2 && s15 == 1 && s16 == 0 && s498 == 0 && sNeg == 0,
                    $"s0={s0}, s15={s15}, s16={s16}, s498={s498}, sNeg={sNeg}");

                // PART D: Real UpdateBatteryCharger charging & overflow LEDs (no GameData required!)
                var pm = ProductionManager.instance ?? new ProductionManager();
                if (chargerAttr != null && hasBat)
                {
                    var chargePd = new ProductionData
                    {
                        productionObjectId = "headless_charger_test",
                        terrainObjectId = chargerAttr.id,
                        terrainObjectAttr = chargerAttr,
                        productionObjectType = ProductionObjectType.BatteryCharger,
                        craftFinishFlag = true,
                        powerInputSufficientFloat = 1.0f
                    };
                    var chargeInv = new InventoryData();
                    chargeInv.inventorySize = new Vector2Int(20, 26);
                    chargeInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                    var bNear = VerifyHelpers.MakeBattery(batteryId, 0, 0, 0.2f);
                    var bMid = VerifyHelpers.MakeBattery(batteryId, 10, 5, 0.2f);
                    var bFar = VerifyHelpers.MakeBattery(batteryId, 18, 24, 0.2f);
                    chargeInv.itemList.Add(bNear);
                    chargeInv.itemList.Add(bMid);
                    chargeInv.itemList.Add(bFar);
                    chargeInv.SetOccupyDataDirty();
                    chargeInv.EnsureOccupyData();
                    chargePd.inventoryData1 = chargeInv;

                    float beforeNear = ItemFeature_Battery.GetBatteryRemainingPower(bNear);
                    float beforeMid = ItemFeature_Battery.GetBatteryRemainingPower(bMid);
                    float beforeFar = ItemFeature_Battery.GetBatteryRemainingPower(bFar);

                    pm.UpdateBatteryCharger(chargePd, 0.0005f);

                    float afterNear = ItemFeature_Battery.GetBatteryRemainingPower(bNear);
                    float afterMid = ItemFeature_Battery.GetBatteryRemainingPower(bMid);
                    float afterFar = ItemFeature_Battery.GetBatteryRemainingPower(bFar);

                    Check("UpdateBatteryCharger_ChargesFarSlots",
                        afterNear > beforeNear && afterNear < batteryCap &&
                        afterMid > beforeMid && afterMid < batteryCap &&
                        afterFar > beforeFar && afterFar < batteryCap &&
                        chargePd.chargerHasPendingBattery,
                        $"near:{beforeNear:F3}->{afterNear:F3}, mid(10,5):{beforeMid:F3}->{afterMid:F3}, far(18,24):{beforeFar:F3}->{afterFar:F3}, pending={chargePd.chargerHasPendingBattery}");
                    Check("UpdateBatteryCharger_OverflowLEDs",
                        chargePd.GetBatteryState(0) == 2 && chargePd.GetBatteryState(1) == 2 && chargePd.GetBatteryState(2) == 2 && chargePd.GetBatteryState(3) == 0,
                        $"led0={chargePd.GetBatteryState(0)}, led1={chargePd.GetBatteryState(1)}, led2={chargePd.GetBatteryState(2)}, led3={chargePd.GetBatteryState(3)}");

                    // 16 full in slots 0..15 + 1 uncharged at (18, 24)
                    var fullPlusOneInv = new InventoryData();
                    fullPlusOneInv.inventorySize = new Vector2Int(20, 26);
                    fullPlusOneInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
                    for (int x = 0; x < 16; x++)
                        fullPlusOneInv.itemList.Add(VerifyHelpers.MakeBattery(batteryId, x, 0, batteryCap));
                    var farUncharged = VerifyHelpers.MakeBattery(batteryId, 18, 24, 0.2f);
                    fullPlusOneInv.itemList.Add(farUncharged);
                    chargePd.inventoryData1 = fullPlusOneInv;

                    float fBefore = ItemFeature_Battery.GetBatteryRemainingPower(farUncharged);
                    pm.UpdateBatteryCharger(chargePd, 0.0001f);
                    float fAfter = ItemFeature_Battery.GetBatteryRemainingPower(farUncharged);
                    Check("UpdateBatteryCharger_16FullPlus1Far",
                        fAfter > fBefore && fAfter < batteryCap && chargePd.chargerHasPendingBattery && chargePd.GetBatteryState(0) == 1 && chargePd.GetBatteryState(15) == 2,
                        $"far:{fBefore:F3}->{fAfter:F3}, led0={chargePd.GetBatteryState(0)}, led15={chargePd.GetBatteryState(15)}");

                    // Finish charging all -> pending=false, all LEDs == 1
                    ItemFeature_Battery.ChargeBattery(farUncharged, batteryCap);
                    pm.UpdateBatteryCharger(chargePd, 0.005f);
                    Check("UpdateBatteryCharger_AllFull",
                        !chargePd.chargerHasPendingBattery && chargePd.GetBatteryState(0) == 1 && chargePd.GetBatteryState(15) == 1,
                        $"pending={chargePd.chargerHasPendingBattery}, led0={chargePd.GetBatteryState(0)}, led15={chargePd.GetBatteryState(15)}");
                }
            }
            catch (Exception ex)
            {
                failed++;
                lines.Add($"EXCEPTION: {ex}");
            }

            VerifyHelpers.WriteReport(VerifyPlugin.HeadlessReportPath, lines, passed, failed);
            Application.Quit();
        }
    }

    public sealed class LiveWorldVerifyRunner : MonoBehaviour
    {
        public LiveWorldVerifyRunner(IntPtr ptr) : base(ptr) { }

        internal static bool WorldReady;
        private bool _loading;
        private bool _done;
        private float _worldReadyAt = -1f;
        private int _stage;
        private int _stageFrames;
        private readonly List<string> _lines = new List<string>();
        private int _passed;
        private int _failed;
        private GameObject _cloneGO;
        private ProductionData _clonePd;

        private void Check(string name, bool cond, string detail)
        {
            if (cond) { _passed++; _lines.Add($"PASS: {name} ({detail})"); }
            else { _failed++; _lines.Add($"FAIL: {name} ({detail})"); }
        }

        private void Finish(string fatalError)
        {
            if (!string.IsNullOrEmpty(fatalError))
            {
                _failed++;
                _lines.Add("FATAL: " + fatalError);
            }
            if (_cloneGO != null)
            {
                try { UnityEngine.Object.DestroyImmediate(_cloneGO); } catch { }
                _cloneGO = null;
            }
            if (_clonePd != null && ProductionManager.instance != null)
            {
                try { ProductionManager.instance.RemoveProductionData(_clonePd); } catch { }
                _clonePd = null;
            }
            VerifyHelpers.WriteReport(VerifyPlugin.LiveReportPath, _lines, _passed, _failed);
            Application.Quit();
        }

        private void Update()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            if (_done) return;

            if (Time.realtimeSinceStartup > 120f)
            {
                _done = true;
                Finish("Timeout waiting for live world");
                return;
            }

            try
            {
                if (!_loading)
                {
                    if (Time.realtimeSinceStartup < 4f || !GameController.initFinishFlag) return;
                    foreach (var p in new[] { GameConfigs.gameDataSavingPath, GameConfigs.mapDataSavingPath, GameConfigs.gameSettingsDataSavingPath })
                    {
                        if (!Path.GetFullPath(p).StartsWith(VerifyPlugin.Lab, StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Unredirected save path: " + p);
                    }
                    var saveFile = Directory.GetFiles(Path.Combine(VerifyPlugin.Lab, "GameData", "GameData"), "*.game")
                        .OrderByDescending(File.GetLastWriteTimeUtc).First();
                    var data = GameDataManager.instance.LoadGameDataFromPath(saveFile);
                    if (data == null) throw new Exception("Failed to load copied save: " + saveFile);
                    _loading = true;
                    GameController.instance.gameData = data;
                    GameController.instance.LoadScene("Gaming");
                    return;
                }

                var gc = GameController.instance;
                var igc = InGameController.instance;
                var pm = ProductionManager.instance;
                var im = ItemManager.instance;
                if (gc == null || gc.Pointer == IntPtr.Zero || gc.playerCharacter == null || gc.playerCharacter.Pointer == IntPtr.Zero ||
                    igc == null || igc.Pointer == IntPtr.Zero || pm == null || pm.Pointer == IntPtr.Zero)
                    return;

                WorldReady = true;
                Time.timeScale = 1f;
                if (_worldReadyAt < 0f)
                {
                    _worldReadyAt = Time.realtimeSinceStartup;
                    return;
                }
                if (Time.realtimeSinceStartup - _worldReadyAt < 4.5f)
                    return;

                var initPanel = GameInitPanel._instance;
                var loadPanel = LoadingInfoPanel._instance;
                if (((initPanel != null && initPanel.gameObject.activeInHierarchy) ||
                     (loadPanel != null && loadPanel.gameObject.activeInHierarchy)) &&
                    Time.realtimeSinceStartup - _worldReadyAt < 45f)
                    return;

                if (_stage == 0)
                {
                    RunLiveChecks(gc, pm, im);
                    _stage = 1;
                    _stageFrames = 0;
                    return;
                }

                // Keep tutorial popups ("Night"/"Torch") hidden & ensure MainCanvas alpha=1 during screenshot capture
                try
                {
                    var hp = HintInformationPanel._instance;
                    if (hp != null && hp.Pointer != IntPtr.Zero && hp.gameObject.activeInHierarchy)
                        hp.gameObject.SetActive(false);
                    var cp = CommonInfomationPanel._instance;
                    if (cp != null && cp.Pointer != IntPtr.Zero && cp.gameObject.activeInHierarchy)
                        cp.gameObject.SetActive(false);
                    var lpTmp = LootInventoryPanel._instance;
                    if (lpTmp != null && lpTmp.transform != null && lpTmp.transform.parent != null)
                    {
                        var cg = lpTmp.transform.parent.GetComponent<CanvasGroup>();
                        if (cg != null && cg.alpha < 0.99f) cg.alpha = 1f;
                    }
                }
                catch { }

                _stageFrames++;
                if (_stage == 1 && _stageFrames == 25)
                {
                    var lp = LootInventoryPanel._instance;
                    bool panelOpen = lp != null && lp.Pointer != IntPtr.Zero && lp.gameObject.activeInHierarchy;
                    var openInv = LootInventoryPanel.CurrentOpenInventoryData;
                    bool sizeOk = openInv != null && openInv.inventorySizeX == 20 && openInv.inventorySizeY == 26;
                    bool scrollPresent = lp != null && lp.gridScrollView != null;
                    var rt = lp != null ? lp.GetComponent<RectTransform>() : null;
                    string rtInfo = rt != null ? $"rect={rt.rect.width:F0}x{rt.rect.height:F0}, pos=({rt.anchoredPosition.x:F0},{rt.anchoredPosition.y:F0}), cellSize={InventoryPanel.cellSize}" : "rt=null";
                    var chainSb = new StringBuilder();
                    var curT = lp != null ? lp.transform : null;
                    while (curT != null)
                    {
                        var cg = curT.GetComponent<CanvasGroup>();
                        var cv = curT.GetComponent<Canvas>();
                        chainSb.Append($"{curT.name}[act={curT.gameObject.activeSelf}");
                        if (cg != null) chainSb.Append($",a={cg.alpha:F1}");
                        if (cv != null) chainSb.Append($",cv={cv.enabled}/ord={cv.sortingOrder}");
                        chainSb.Append("]->");
                        curT = curT.parent;
                    }
                    var pp = PlayerPanel._instance;
                    bool ppOpen = pp != null && pp.Pointer != IntPtr.Zero && pp.gameObject.activeInHierarchy;
                    Check("LiveUI_LootInventoryPanel_Open20x26", panelOpen && sizeOk && openInv.itemList.Count == 3,
                        $"open={panelOpen}, ppOpen={ppOpen}, size={(openInv != null ? $"{openInv.inventorySizeX}x{openInv.inventorySizeY}" : "null")}, scrollView={scrollPresent}, items={openInv?.itemList?.Count}, {rtInfo}, chain={chainSb}");
                    ScreenCapture.CaptureScreenshot(Path.Combine(VerifyPlugin.ScreenshotDir, "stationary_charger_20x26.png"));
                    _stage = 2;
                    _stageFrames = 0;
                    return;
                }

                if (_stage == 2 && _stageFrames == 6)
                {
                    // Close panel cleanly
                    var lp = LootInventoryPanel._instance;
                    if (lp != null && lp.Pointer != IntPtr.Zero)
                    {
                        try { lp.ClosePanel(); } catch { }
                    }
                    _done = true;
                    Finish(null);
                }
            }
            catch (Exception ex)
            {
                _done = true;
                Finish(ex.ToString());
            }
        }

        private void RunLiveChecks(GameController gc, ProductionManager pm, ItemManager im)
        {
            var chargerAttr = VerifyHelpers.FindBatteryChargerAttr(gc, out _);
            bool hasBat = VerifyHelpers.FindBatteryItem(im, out int batteryId, out float batteryCap, out _);
            Check("LiveWorld_Prereqs", chargerAttr != null && hasBat, $"chargerId={chargerAttr?.id}, batId={batteryId}");
            if (chargerAttr == null || !hasBat) return;

            // 1. Check any pre-existing BatteryChargers already in loaded save
            int existingCount = 0;
            int existingOk = 0;
            if (pm.productionDataList != null)
            {
                for (int i = 0; i < pm.productionDataList.Count; i++)
                {
                    var pd = pm.productionDataList[i];
                    if (pd != null && pd.productionObjectType == ProductionObjectType.BatteryCharger && pd.inventoryData1 != null)
                    {
                        existingCount++;
                        if (pd.inventoryData1.inventorySizeX >= 20 && pd.inventoryData1.inventorySizeY >= 26)
                            existingOk++;
                    }
                }
            }
            Check("LiveWorld_ExistingSaveChargers", existingCount == existingOk,
                $"existingChargers={existingCount}, expanded={existingOk}");

            // 2. Test AddProductionData on legacy 4x4 charger with loot at (1, 1)
            var legacyPd = new ProductionData
            {
                productionObjectId = "live_verify_legacy_4x4",
                terrainObjectId = chargerAttr.id,
                terrainObjectAttr = chargerAttr,
                productionObjectType = ProductionObjectType.BatteryCharger,
                craftFinishFlag = true
            };
            var legacyInv = new InventoryData();
            legacyInv.inventorySize = new Vector2Int(4, 4);
            legacyInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
            legacyInv.itemList.Add(VerifyHelpers.MakeBattery(batteryId, 1, 1, 0.5f));
            legacyPd.inventoryData1 = legacyInv;

            pm.AddProductionData(legacyPd);
            bool addOk = legacyPd.inventoryData1.inventorySizeX == 20 && legacyPd.inventoryData1.inventorySizeY == 26 &&
                         legacyPd.inventoryData1.itemList.Count == 1 &&
                         legacyPd.inventoryData1.itemList[0].inventoryCoordinate.x == 1 &&
                         legacyPd.inventoryData1.itemList[0].inventoryCoordinate.y == 1;
            legacyPd.inventoryData1.EnsureOccupyData();
            bool farFree = !legacyPd.inventoryData1.IsOccupied(new Vector2Int(19, 25));
            Check("LiveWorld_AddProductionData_LegacyUpgrade", addOk && farFree,
                $"size={legacyPd.inventoryData1.inventorySizeX}x{legacyPd.inventoryData1.inventorySizeY}, farFree={farFree}");
            pm.RemoveProductionData(legacyPd);

            // 3. Test OnLoadGame upgrade on a legacy 4x4 charger already in productionDataList
            var onloadPd = new ProductionData
            {
                productionObjectId = "live_verify_onload_4x4",
                terrainObjectId = chargerAttr.id,
                terrainObjectAttr = chargerAttr,
                productionObjectType = ProductionObjectType.BatteryCharger,
                craftFinishFlag = true
            };
            var onloadInv = new InventoryData();
            onloadInv.inventorySize = new Vector2Int(4, 4);
            onloadInv.itemList = new Il2CppSystem.Collections.Generic.List<ItemData>();
            onloadInv.itemList.Add(VerifyHelpers.MakeBattery(batteryId, 2, 2, 0.5f));
            onloadPd.inventoryData1 = onloadInv;
            pm.productionDataList.Add(onloadPd);
            pm.OnLoadGame();
            bool onloadOk = onloadPd.inventoryData1.inventorySizeX == 20 && onloadPd.inventoryData1.inventorySizeY == 26 &&
                            onloadPd.inventoryData1.itemList.Count == 1;
            Check("LiveWorld_OnLoadGame_LegacyUpgrade", onloadOk,
                $"size={onloadPd.inventoryData1.inventorySizeX}x{onloadPd.inventoryData1.inventorySizeY}");
            pm.RemoveProductionData(onloadPd);

            // Dismiss any startup HintInformationPanel ("Night") so UI is clean for screenshot
            try
            {
                var hints = UnityEngine.Object.FindObjectsOfType<HintInformationPanel>();
                if (hints != null)
                {
                    for (int i = 0; i < hints.Length; i++)
                    {
                        if (hints[i] != null && hints[i].gameObject.activeInHierarchy)
                            hints[i].gameObject.SetActive(false);
                    }
                }
            }
            catch { }

            // 4. Real native spawn via GameController.AddTerrainObject -> Init + OnBuildFinish + OnInit_ProductionObject + ResolveContainerInventory + UpdateBatteryCharger + LootInventory!
            var tod = new TerrainObjectData(chargerAttr.id);
            tod.productionObjectId = "live_verify_clone_charger";
            var spawnPos = gc.playerCharacter.transform.position + new Vector3(1.5f, 0f, 0f);
            var spawnedObj = gc.AddTerrainObject(tod, spawnPos, false);
            var cloneCharger = spawnedObj != null ? spawnedObj.TryCast<TerrainObject_Production_BatteryCharger>() : null;
            _cloneGO = spawnedObj != null ? spawnedObj.gameObject : null;
            _clonePd = cloneCharger?.objectData?.productionData;

            if (cloneCharger != null && _clonePd != null)
            {
                _clonePd.craftFinishFlag = true;
            }

            bool buildOk = cloneCharger != null && _clonePd != null && _clonePd.inventoryData1 != null &&
                           _clonePd.inventoryData1.inventorySizeX == 20 &&
                           _clonePd.inventoryData1.inventorySizeY == 26;
            Check("LiveWorld_NativeSpawn_Init_20x26", buildOk,
                _clonePd?.inventoryData1 == null
                    ? $"charger={(cloneCharger != null)}, pd={(_clonePd != null)}, inv=null"
                    : $"size={_clonePd.inventoryData1.inventorySizeX}x{_clonePd.inventoryData1.inventorySizeY}");

            if (cloneCharger == null || _clonePd == null || _clonePd.inventoryData1 == null)
                return;

            // Verify OnBuildFinish on fresh null inventory creates 20x26
            _clonePd.inventoryData1 = null;
            cloneCharger.OnBuildFinish();
            bool rebuildOk = _clonePd.inventoryData1 != null &&
                             _clonePd.inventoryData1.inventorySizeX == 20 &&
                             _clonePd.inventoryData1.inventorySizeY == 26;
            Check("LiveWorld_Prefab_OnBuildFinish_20x26", rebuildOk,
                _clonePd.inventoryData1 == null ? "null" : $"size={_clonePd.inventoryData1.inventorySizeX}x{_clonePd.inventoryData1.inventorySizeY}");

            cloneCharger.OnInit_ProductionObject();
            bool filterOk = _clonePd.inventoryData1?.itemFeatureLimit != null &&
                            _clonePd.inventoryData1.itemFeatureLimit.Contains(ItemFeatureType.Battery);
            Check("LiveWorld_Prefab_BatteryFilter", filterOk, $"limitCount={_clonePd.inventoryData1?.itemFeatureLimit?.Count}");

            // Simulate NetContainerSync.ResolveContainerInventory on legacy size
            _clonePd.inventoryData1.inventorySize = new Vector2Int(4, 4);
            var resolved = ZedZone.Net.NetContainerSync.ResolveContainerInventory(cloneCharger);
            Check("LiveWorld_NetResolveContainerInventory", resolved != null && resolved.inventorySizeX == 20 && resolved.inventorySizeY == 26,
                resolved == null ? "null" : $"size={resolved.inventorySizeX}x{resolved.inventorySizeY}");

            // Place discharged batteries across near (0,0) + mid (10,5) + far (18,24) cells and charge live!
            var bNear = VerifyHelpers.MakeBattery(batteryId, 0, 0, 0.2f);
            var bMid = VerifyHelpers.MakeBattery(batteryId, 10, 5, 0.2f);
            var bFar = VerifyHelpers.MakeBattery(batteryId, 18, 24, 0.2f);
            _clonePd.inventoryData1.itemList.Add(bNear);
            _clonePd.inventoryData1.itemList.Add(bMid);
            _clonePd.inventoryData1.itemList.Add(bFar);
            _clonePd.inventoryData1.SetOccupyDataDirty();
            _clonePd.inventoryData1.EnsureOccupyData();
            _clonePd.powerInputSufficientFloat = 1.0f;

            float b0 = ItemFeature_Battery.GetBatteryRemainingPower(bNear);
            float b1 = ItemFeature_Battery.GetBatteryRemainingPower(bMid);
            float b2 = ItemFeature_Battery.GetBatteryRemainingPower(bFar);

            pm.UpdateBatteryCharger(_clonePd, 0.0005f);
            cloneCharger.UpdateBatteryCharging();

            float a0 = ItemFeature_Battery.GetBatteryRemainingPower(bNear);
            float a1 = ItemFeature_Battery.GetBatteryRemainingPower(bMid);
            float a2 = ItemFeature_Battery.GetBatteryRemainingPower(bFar);

            bool ledColorsOk = cloneCharger.chargingStateSRs != null && cloneCharger.chargingStateSRs.Length == 16 &&
                               cloneCharger.chargingStateSRs[0].color == cloneCharger.batteryChargingColor &&
                               cloneCharger.chargingStateSRs[1].color == cloneCharger.batteryChargingColor &&
                               cloneCharger.chargingStateSRs[2].color == cloneCharger.batteryChargingColor &&
                               cloneCharger.chargingStateSRs[3].color == cloneCharger.noBatteryColor;
            Check("LiveWorld_ChargeAndSpriteLEDs",
                b0 < 0.5f && a0 > b0 && a1 > b1 && a2 > b2 && ledColorsOk,
                $"near:{b0:F3}->{a0:F3}, mid:{b1:F3}->{a1:F3}, far:{b2:F3}->{a2:F3}, ledColorsOk={ledColorsOk}");

            // Open actual in-game LootInventoryPanel via LootInventory(null)
            _clonePd.inventoryData1.inventorySize = new Vector2Int(4, 4); // verify LootInventory Prefix upgrades even un-upgraded container right before opening!
            cloneCharger.LootInventory(null);
        }
    }
}
