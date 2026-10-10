# 王国顾问 Kingdom Advisor 0.6.7-r1

《王国：两位君主》游戏内辅助 Mod。插件版本为 0.6.7；r1 是安装包修订，移除操作结果提示，并提供首次安装兼容修复。

## 下载与安装

[GitHub 正式下载](https://github.com/boyl/kingdom-advisor/releases/tag/v0.6.7-r1) · [Nexus Mods 原有条目](https://www.nexusmods.com/kingdomtwocrowns/mods/43?tab=files)

Nexus 当前使用 0.6.7-r2 手动安装包（不含脚本、不需要 PowerShell 7）：[中文手动说明](distribution/nexus/README.zh-CN.md) · [安装示意图](distribution/nexus/INSTALLATION.png)。下表与一键安装步骤仅适用于 GitHub 0.6.7-r1 资源。

两种包任选其一，不要下载 GitHub 自动生成的 Source code 包：

| 资源 | 内容与适用情况 | 安装说明 |
|---|---|---|
| `KingdomAdvisor-0.6.7-r1-ModOnly.zip` | 仅本 Mod，适合已有正常工作前置的玩家；不含 BepInEx | [中文](distribution/README.ModOnly.md) · [English](distribution/README.ModOnly.en.md) |
| `KingdomAdvisor-0.6.7-r1-Complete.zip` | Mod＋完整 #788 前置＋解析器兼容补丁，适合首次安装或遇到 `AndroidManager / RawPropertyType` 启动错误的玩家 | [中文](distribution/README.Complete.md) · [English](distribution/README.Complete.en.md) |

整合包是基于官方 BepInEx 6.0.0-be.788 的修订包，**并非未修改的官方包**。仅替换 `LibCpp2IL.dll` 与 `Cpp2IL.Core.dll`，包含对应源码、补丁脚本和许可证。纯官方 #738、#788 的首次生成错误已在本机复现；不要将整合包内的修复误认为官方原版已解决。[检测记录](docs/acceptance/bepinex-738-compatibility.md)。

### 一键安装

1. 正常退出游戏，解压所选 ZIP 到一个普通文件夹。
2. 安装 [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest)（脚本前置，整合包不包含它）。
3. 双击 `INSTALL.cmd`，按提示选择含 `KingdomTwoCrowns.exe` 的游戏目录。可以在 Steam → 管理 → 浏览本地文件中找到。
4. 安装器自动备份、复制并校验文件；受保护目录会请求 Windows 管理员权限。整合包会备份并更换旧核心和运行时、移走旧生成缓存，保留配置、存档及其他插件。
5. 正常启动游戏。首次生成程序集可能需要数分钟，请等待；**F6** 打开设置，**F7** 打开图鉴。

手动安装 ModOnly：将 `payload/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll` 放到下列路径。整合包优先使用安装器，避免新旧核心混用。

```text
Kingdom Two Crowns/                  ← 含游戏 EXE 的文件夹
├─ KingdomTwoCrowns.exe
├─ KingdomTwoCrowns_Data/
├─ winhttp.dll                      ← 整合包／前置
├─ doorstop_config.ini              ← 整合包／前置
├─ dotnet/                          ← 整合包／前置
├─ .install-backups/                ← 安装器生成的备份
└─ BepInEx/
   ├─ core/                         ← 整合包／前置
   ├─ plugins/
   │  ├─ KingdomAdvisor/
   │  │  └─ KingdomAdvisor.dll      ← 本 Mod 的放入位置
   │  └─ 其他插件/                  ← 保留
   ├─ config/                       ← 首次运行生成，更新保留
   └─ interop/                      ← 本机首次生成，不随包分发
```

不要将 `payload` 文件夹本身放入游戏目录，也不要形成 `BepInEx/BepInEx`。两个包的中英文说明包含完整放置示意、命令行安装和备份回滚方法。

## 已验收范围

Windows x64、游戏 2.4.2、Unity 6000.0.66f2、IL2CPP。修复后的 #788 完成首次生成、重启进入游戏及用户验收；本次提示移除也经用户验收。安装脚本的隔离环境安装、哈希验证、失败恢复与回滚检查通过。UAC 实际交互、其他游戏版本、全部 DLC 与联机矩阵未逐项验证。其他插件仍保留，其组合兼容性需要各插件自身支持。

## 功能与默认值

- 顶部透明地图：名称、数量、敌群移动；聚焦显示状态。普通地图短按查看，开启传送后长按0.8秒传送。
- 左下王国顾问默认开启，附近详情默认开启，放在顾问旁；提供资源、人口、科技和坐骑图标。
- 顾问、提醒、附近详情默认透明，主界面默认面板；均可配置。卡片可拖动。
- 图鉴、分类搜索、地点、岛屿记录、分类设置和下拉选项。
- 无限钱袋、传送、无限耐力默认关闭；速度默认1倍。无限容量关闭即恢复原生拾取及溢出行为。
- 手动添加资源和直接清空所选资源，无二次确认。普通钱袋显示资源合计，不将原生容量字段误作物理容积。

## 操作

|输入|操作|
|---|---|
|F4|收起／显示|
|F6|设置|
|F7|图鉴|
|鼠标|指向、点击、拖动；长按地图传送|
|按下左摇杆|短按打开／关闭面板；长按0.8秒显示／隐藏整个HUD，可配置|
|左摇杆|角色移动；面板内导航|
|右摇杆|常驻光标；启用光标时不控制角色|
|LB／RB|切页签|
|LT／RT|翻列表或多页详情；设置按页码翻页|
|方向键、A、B|导航、确认、返回|

键鼠与手柄提示随输入切换，使用统一高对比大光标。

## 配置与诊断

0.6.7 正式版以正式 0.6.6 为基线：修正关闭面板后的输入释放，键鼠无需等待手柄回中，手柄松开或读取失败后解除保护。自动回归、构建和安装验证已通过，2026-10-10 已获用户验收正常；手柄专项及全部关闭路径未逐项实测。不包含自定义坐骑或 Cpp2IL 加载器修补。

配置：BepInEx/config/local.kingdom.advisor.cfg。岛屿记录和诊断：BepInEx/config/KingdomAdvisor/。首次进入游戏保存诊断截图，F9 可手动截图；诊断设置默认关闭自动展示页面。

本版保持插件标识 local.kingdom.advisor 和已有配置键。不会打包用户配置、存档、游戏程序集或其他 Mod。

## 中英双语

默认自动跟随游戏语言：中文变体使用简体中文，英文与其他语言使用英文。游戏语言未就绪时读取系统语言，无法识别时回退英文。F6 → 图鉴与进度 → 语言，可切换自动／简体中文／English，即时生效。保留原有配置键和记录格式。图鉴在两种显示语言下均可使用中英文搜索。

英文说明：[English README](README.en.md)。

## 验收与发布

0.5.8 已获用户最终验收。0.6.0 增加语言解析、显示文案覆盖检查和双语言实景布局验证；源码、已安装DLL与上传包绑定校验。联机保持开放，双端及全部DLC尚未验证。

下载与更新：[GitHub Releases](https://github.com/boyl/kingdom-advisor/releases)。Nexus Mods 更新使用官方上传 API。

0.6.5 已获用户最终验收，建筑详情与人口统计的验证证据见 [验收文档](docs/acceptance/building-details-audit.md)。

## 图鉴与地点联动

全部104条图鉴使用按身份区分的像素示意图标，左侧主体、右侧用途或特征；不同条目的完整图案不同。选择本岛已有条目可查看地点；地点显示方向、距离、用途和当前交互状态，可打开对应图鉴或在地图定位。同名建筑分别保留，当前骑乘坐骑不列作购买地点。地图定位不会自动传送。
