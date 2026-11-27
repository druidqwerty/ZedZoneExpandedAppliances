# ZED ZONE: 便携电器扩容

**特别为玩家 `6VaN4!K96` 定制**

[![支持开发](https://raw.githubusercontent.com/druidqwerty/ZedZoneExpandedAppliances/e6c552329066c1bc794627cdbe63806fb2a3204f/docs/banner.png)](https://boosty.to/lrgpp)

免费开源。代码由 AI 编写，方向、测试和时间由我负责。如果这个模组让你的游戏体验更好：[在 Boosty 支持开发](https://boosty.to/lrgpp)。

[English](README.md) · [Русский](README_RU.md)

适用于 **ZED ZONE** (`1.0.20–1.0.23`) 的 BepInEx 6 (IL2CPP) 代码模组，默认将三款便携储物电器和基地固定式**电池充电台**的容器网格扩展至 **`20×26`（520 格）**，支持旧存档安全自动升级、全网格完整充电与独立配置。

## 扩容内容

| 电器 | ID | 原版尺寸 | 模组默认尺寸 |
|---|---|---|---|
| **便携迷你冰箱** | 物品 `1063` | `8×6`（48 格） | **`20×26`**（520 格） |
| **便携充电盒** | 物品 `1066` | `8×4`（32 格） | **`20×26`**（520 格） |
| **太阳能充电盒** | 物品 `1067` | `8×4`（32 格） | **`20×26`**（520 格） |
| **电池充电台（固定式）** *(v1.1.0)* | 建筑 `126` | `4×4`（16 格） | **`20×26`**（520 格） |

## 主要特性

- **旧存档安全升级：** 背包、箱子、车厢或队友物品栏中已有的便携电器，以及基地中已建造的**电池充电台**在读档或打开时自动扩容，不丢失、不移动任何物品。
- **全网格正常充电与指示灯同步：** 修复了原版 `ProductionData.SetBatteryState` 仅限 16 格的越界限制，放在 `20×26` 任意格子的电池均可正常充电，并将超出 16 格的充电状态同步显示到台面 16 颗 LED 指示灯上。
- **独立配置：** 可在 `BepInEx/config/com.druid.zedzone.expandedappliances.cfg` 中单独开关并自定义每款电器（含 `[StationaryBatteryCharger]`）的宽高。

## 安装

1. 安装第三方前置 [ZED ZONE Code Mod Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=3834614258) 并运行一次 `install.cmd`。
2. 通过 Steam 创意工坊订阅，或从 [GitHub Releases](https://github.com/druidqwerty/ZedZoneExpandedAppliances/releases) 解压 `ExpandedAppliances/` 到 `<ZED ZONE>\Mods\ExpandedAppliances\`。

© 2026 druidqwerty。MIT 许可证。免费开源；代码由 AI 编写，方向与测试由 druid 完成。
