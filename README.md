# ZED ZONE: Expanded Portable Appliances

**Specially made by request for user: `6VaN4!K96`**

[![Support development](https://raw.githubusercontent.com/druidqwerty/ZedZoneExpandedAppliances/e6c552329066c1bc794627cdbe63806fb2a3204f/docs/banner.png)](https://boosty.to/lrgpp)

Free and open source. The code is written by an AI; the direction, the testing and the time are mine. If the mod made your game better: [support on Boosty](https://boosty.to/lrgpp).

[Русский](README_RU.md) · [简体中文](README_CN.md)

BepInEx 6 (IL2CPP) code mod for **ZED ZONE** (`1.0.20–1.0.23`) that expands the container grid capacity of the three portable storage appliances and the stationary base **Battery Charger Bench** to **`20×26` (520 slots)** by default — with automatic safe upgrades for existing saves, full expanded-grid charging support, and per-appliance configuration.

## What It Expands

| Appliance | ID | Vanilla Grid | Default Mod Grid |
|---|---|---|---|
| **Portable Mini Fridge** | Item `1063` | `8×6` (48 slots) | **`20×26`** (520 slots) |
| **Portable Charger Box** | Item `1066` | `8×4` (32 slots) | **`20×26`** (520 slots) |
| **Solar Charger Box** | Item `1067` | `8×4` (32 slots) | **`20×26`** (520 slots) |
| **Stationary Battery Charger Bench** *(v1.1.0)* | Object `126` | `4×4` (16 slots) | **`20×26`** (520 slots) |

## Features

- **Safe Existing Save Upgrade:** Already-owned portable appliances (in backpacks, chests, vehicle trunks, or companion inventories) and already-built base **Battery Charger Benches** automatically expand on load/open without losing or shifting stored items.
- **Full Expanded Charging & LEDs:** Fixes the native 16-slot LED bitfield limit in `ProductionData.SetBatteryState` so batteries placed anywhere across all 520 slots of the stationary charger bench charge properly and mirror their charging/charged status onto the 16 world LEDs.
- **Vanilla Mechanics Preserved:** Refrigeration, car-battery / solar / base-grid charging, battery-only charger filters, and auto-sorting work across the full expanded grid.
- **Configurable (`BepInEx/config/com.druid.zedzone.expandedappliances.cfg`):** Individually enable/disable each appliance (including `[StationaryBatteryCharger]`) and set custom width (`4–40` / `8–40`) and height (`up to 100`).
- **Safe Shrink Protection:** `AllowSafeShrink = false` by default (containers only expand). Even if enabled, containers never shrink below occupied item coordinates.

## Installation

1. Install third-party [ZED ZONE Code Mod Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=3834614258) and run `install.cmd` once.
2. Subscribe via Steam Workshop, or extract `ExpandedAppliances/` from the [latest release](https://github.com/druidqwerty/ZedZoneExpandedAppliances/releases) into `<ZED ZONE>\Mods\ExpandedAppliances\`.

## Building from Source

Requires `.NET 6 SDK` and ZED ZONE with `BepInEx/interop/` generated:

```powershell
dotnet build ExpandedAppliances.csproj -c Release
```

© 2026 druidqwerty. MIT License. Free and open source; code written by AI, direction and testing by druid.
