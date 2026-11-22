using BepInEx.Configuration;

namespace ZedZone.ExpandedAppliances
{
    internal static class ApplianceConfig
    {
        public const int PortableFridgeId = 1063;
        public const int PortableChargerId = 1066;
        public const int SolarChargerId = 1067;

        public const int DefaultWidth = 20;
        public const int DefaultHeight = 26;

        public static ConfigEntry<bool> Enabled;

        public static ConfigEntry<bool> FridgeEnabled;
        public static ConfigEntry<int> FridgeWidth;
        public static ConfigEntry<int> FridgeHeight;

        public static ConfigEntry<bool> PortableChargerEnabled;
        public static ConfigEntry<int> PortableChargerWidth;
        public static ConfigEntry<int> PortableChargerHeight;

        public static ConfigEntry<bool> SolarChargerEnabled;
        public static ConfigEntry<int> SolarChargerWidth;
        public static ConfigEntry<int> SolarChargerHeight;

        public static ConfigEntry<bool> AllowSafeShrink;

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Master toggle for Expanded Portable Appliances.");

            FridgeEnabled = config.Bind(
                "PortableFridge",
                "Enabled",
                true,
                "Expand Portable Mini Fridge (ID 1063, vanilla 8x6).");
            FridgeWidth = config.Bind(
                "PortableFridge",
                "Width",
                DefaultWidth,
                new ConfigDescription("Grid width for Portable Mini Fridge (vanilla: 8).", new AcceptableValueRange<int>(8, 40)));
            FridgeHeight = config.Bind(
                "PortableFridge",
                "Height",
                DefaultHeight,
                new ConfigDescription("Grid height for Portable Mini Fridge (vanilla: 6).", new AcceptableValueRange<int>(6, 100)));

            PortableChargerEnabled = config.Bind(
                "PortableCharger",
                "Enabled",
                true,
                "Expand Portable Charger Box (ID 1066, vanilla 8x4).");
            PortableChargerWidth = config.Bind(
                "PortableCharger",
                "Width",
                DefaultWidth,
                new ConfigDescription("Grid width for Portable Charger Box (vanilla: 8).", new AcceptableValueRange<int>(8, 40)));
            PortableChargerHeight = config.Bind(
                "PortableCharger",
                "Height",
                DefaultHeight,
                new ConfigDescription("Grid height for Portable Charger Box (vanilla: 4).", new AcceptableValueRange<int>(4, 100)));

            SolarChargerEnabled = config.Bind(
                "SolarCharger",
                "Enabled",
                true,
                "Expand Solar Charger Box (ID 1067, vanilla 8x4).");
            SolarChargerWidth = config.Bind(
                "SolarCharger",
                "Width",
                DefaultWidth,
                new ConfigDescription("Grid width for Solar Charger Box (vanilla: 8).", new AcceptableValueRange<int>(8, 40)));
            SolarChargerHeight = config.Bind(
                "SolarCharger",
                "Height",
                DefaultHeight,
                new ConfigDescription("Grid height for Solar Charger Box (vanilla: 4).", new AcceptableValueRange<int>(4, 100)));

            AllowSafeShrink = config.Bind(
                "Safety",
                "AllowSafeShrink",
                false,
                "If true, allows shrinking an already-expanded container down to the configured size ONLY when every stored item fits inside the new bounds. When false (default), existing containers only expand and never shrink.");
        }

        public static bool TryGetTargetSize(int itemId, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (Enabled != null && !Enabled.Value)
                return false;

            switch (itemId)
            {
                case PortableFridgeId:
                    if (FridgeEnabled != null && !FridgeEnabled.Value) return false;
                    width = Clamp(FridgeWidth != null ? FridgeWidth.Value : DefaultWidth, 8, 40);
                    height = Clamp(FridgeHeight != null ? FridgeHeight.Value : DefaultHeight, 6, 100);
                    return true;

                case PortableChargerId:
                    if (PortableChargerEnabled != null && !PortableChargerEnabled.Value) return false;
                    width = Clamp(PortableChargerWidth != null ? PortableChargerWidth.Value : DefaultWidth, 8, 40);
                    height = Clamp(PortableChargerHeight != null ? PortableChargerHeight.Value : DefaultHeight, 4, 100);
                    return true;

                case SolarChargerId:
                    if (SolarChargerEnabled != null && !SolarChargerEnabled.Value) return false;
                    width = Clamp(SolarChargerWidth != null ? SolarChargerWidth.Value : DefaultWidth, 8, 40);
                    height = Clamp(SolarChargerHeight != null ? SolarChargerHeight.Value : DefaultHeight, 4, 100);
                    return true;

                default:
                    return false;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
