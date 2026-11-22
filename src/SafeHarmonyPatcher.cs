using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace ZedZone.ExpandedAppliances
{
    internal static class SafeHarmonyPatcher
    {
        public static int PatchAssemblySafely(Harmony harmony, Assembly assembly, ManualLogSource log)
        {
            int applied = 0;
            int failed = 0;
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null) continue;
                var attrs = type.GetCustomAttributes(typeof(HarmonyPatch), false);
                if (attrs == null || attrs.Length == 0) continue;

                try
                {
                    var processor = harmony.CreateClassProcessor(type);
                    var methods = processor.Patch();
                    int count = methods != null ? methods.Count : 0;
                    applied += count;
                    log.LogInfo($"[ExpandedAppliances] Patched {type.Name} ({count} target(s)).");
                }
                catch (Exception ex)
                {
                    failed++;
                    log.LogError($"[ExpandedAppliances] Failed to patch {type.Name}: {ex}");
                }
            }

            log.LogInfo($"[ExpandedAppliances] Patch summary: applied={applied}, failed={failed}.");
            return applied;
        }
    }
}
