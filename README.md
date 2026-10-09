# 王国顾问 Kingdom Advisor 0.6.6

《王国：两位君主》游戏内辅助 Mod，由 BepInEx 自动加载。

## 兼容范围

已验收环境：Windows x64、游戏 2.4.2、Unity 6000.0.66f2、IL2CPP、BepInEx 6.0.0-be.738。需先安装适合此游戏的 BepInEx IL2CPP；本包不包含 BepInEx 或游戏程序集。

单机已由用户验收。联机功能保持可用，但双端同步与其他主题／DLC 的完整矩阵尚未验证。不能将其视为已验证支持。其他 Mod 可能改变费用或钱袋规则。

## 前置依赖与安装

需要 **BepInEx 6 · Unity IL2CPP · Windows x64**；本 Mod 已测试版本为 **6.0.0-be.738**。Mod 包不包含 BepInEx，需要先安装。不要选择 Mono、x86 或 BepInEx 5。

### 1. 安装 BepInEx

1. 打开 [BepInEx 官方构建下载页](https://builds.bepinex.dev/projects/bepinex_be)，找到 **#738**，下载 `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.738+af0cba7.zip`。
2. 在 Steam 中右键游戏 → 管理 → 浏览本地文件，找到 `KingdomTwoCrowns.exe` 所在目录。
3. 将 BepInEx 压缩包的**全部内容**解压到该目录，包含根目录文件与 `BepInEx` 文件夹，不要额外套一层文件夹。
4. 启动游戏一次，等待首次初始化完成后正常退出。首次启动可能较慢，会生成 `BepInEx/config` 等文件。

已安装上述适用版本的玩家可以跳过此步骤。[BepInEx 官方安装指南](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html)。

### 2. 安装王国顾问

1. 从 [0.6.6 Release](https://github.com/boyl/kingdom-advisor/releases/tag/v0.6.6) 下载 **`KingdomAdvisor-0.6.6.zip`**，不要下载源码包。
2. 确保游戏已退出。解压 Mod，将其中的 `BepInEx` 文件夹合并到游戏目录。
3. 核对插件路径为 `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`。
4. 正常启动游戏，Mod 自动加载。**F6** 打开设置，**F7** 打开图鉴；手柄默认短按**左摇杆 L3** 打开界面，无需独立启动器。

### 放到哪里：以游戏 EXE 所在文件夹为准

在 Steam 中右键《Kingdom Two Crowns》→ **管理 → 浏览本地文件**。打开后，能直接看到 `KingdomTwoCrowns.exe` 的文件夹就是游戏根目录。Steam 库可以在任何盘，不需要与下面的示例盘符相同。

示例根目录：`D:\SteamLibrary\steamapps\common\Kingdom Two Crowns\`

**放置示意：Mod 压缩包内的 `BepInEx` → 游戏根目录内的 `BepInEx`（合并文件夹）。** 已有其他 Mod 时保留它们，只覆盖王国顾问自己的 DLL。

压缩包内结构：

```text
KingdomAdvisor-0.6.6.zip
├─ BepInEx\
│  └─ plugins\
│     └─ KingdomAdvisor\
│        └─ KingdomAdvisor.dll     ← 需要放入游戏的插件
├─ README.md                      ← 中文安装说明
├─ README.en.md                   ← English guide
└─ CHANGELOG.md                   ← 更新说明
```

安装 BepInEx、放入王国顾问并首次运行后，目录应当是：

```text
Kingdom Two Crowns\                ← 游戏根目录
├─ KingdomTwoCrowns.exe            ← 以这个文件定位
├─ KingdomTwoCrowns_Data\
├─ winhttp.dll                     ← BepInEx 安装包带来的根目录文件
├─ doorstop_config.ini             ← BepInEx 安装包带来的根目录文件
├─ dotnet\                        ← BepInEx IL2CPP 安装包内容
└─ BepInEx\
   ├─ core\                       ← BepInEx 本体
   ├─ plugins\
   │  ├─ KingdomAdvisor\
   │  │  └─ KingdomAdvisor.dll     ← 王国顾问必须位于此处
   │  └─ 其他Mod…                  ← 保留已有插件
   └─ config\                     ← 首次运行后自动生成
      ├─ local.kingdom.advisor.cfg ← 顾问配置，更新时保留
      └─ KingdomAdvisor\          ← 岛屿记录及诊断文件
```

以上只列出定位所需文件；BepInEx 压缩包内其他文件也应完整解压，不能只复制树中列出的几项。`config` 和顾问记录目录首次运行后生成，不需要自行建立或从别人那里复制。

**正确路径：** `游戏根目录\BepInEx\plugins\KingdomAdvisor\KingdomAdvisor.dll`

常见放错位置（这些路径不会按本说明加载）：

```text
游戏根目录\KingdomAdvisor-0.6.6\BepInEx\…   × 多套了一层解压文件夹
游戏根目录\BepInEx\BepInEx\plugins\…      × 重复嵌套 BepInEx
游戏根目录\KingdomTwoCrowns_Data\…         × 放进了游戏资源目录
游戏根目录\KingdomAdvisor.dll              × DLL 放在根目录
```

安装后进入游戏按 **F7** 打开图鉴、**F6** 打开设置即可确认。若没有出现界面，先核对上述 DLL 的完整路径和 BepInEx 版本；`BepInEx\LogOutput.log` 可用于查看加载错误。
### 更新与卸载

更新：保存退出游戏，覆盖上述 DLL，保留已有配置。

卸载：保存退出游戏，删除 `BepInEx/plugins/KingdomAdvisor` 文件夹。配置文件可按需保留，不需要卸载其他 Mod 使用的 BepInEx。

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

## 坐骑预览版

0.7.0-preview.19 加入呆猫与呆狗；F6 → 自定义坐骑切换，使用原生坐骑技能输入。两者冷却0.5秒，动作不重叠。完整替代插件，与0.6.6二选一安装。预览版的真实敌群战斗、双端联机、最新原生喷火隔离完整实测及肩背骑姿未完成验收。

