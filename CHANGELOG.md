# 王国顾问 0.6.15 / Kingdom Advisor 0.6.15

- 修复可招募提醒点击区域与显示位置不一致：可独立拖动，拖动期间不吸回顾问，松手后保存位置。
- 修复英文和多行详情溢出、手柄提示遮挡；顾问默认单列，可切换两列，恢复行距与分组留白，可招募提醒默认与地图底部提示同行，以“地图／王国”标签和颜色区分，仍可独立拖动；空间不足时换行。透明度 0 时只保留文字、图标与昼夜进度。
- 新增“长按连续购买／升级”开关，默认关闭。持续按住投币键即可对同一目标连续购买、招募或升级，松手停止；覆盖市民之家和火焰桶，保留原生费用、库存与施工限制。支持单人、本地双人及房主本机角色，暂不支持联机加入者。
- 新增无限容量时隐藏原生钱袋图形的开关，默认关闭，保留顾问中的余额数字；修复无限钱袋乘船后金币被截为 1000 的问题。
- 分摊周期性建筑扫描，降低走动时的卡顿尖峰。

不合并不同隐士，不扩展装备队列。本次不含逐条顾问筛选。保留配置与存档，不新增依赖。全部 DLC、分辨率及双机联机场景未完整复核。

已有正常工作的 BepInEx 6 IL2CPP 前置请选择 **ModOnly**；首次安装请选择 **Complete**，内含既有修订版 BepInEx #788、两项 Cpp2IL 兼容补丁、许可证与对应源码。官方原版 #788 不含这些首次生成修复。

GitHub：安装 [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6)，保存并退出游戏，解压后双击 `INSTALL.cmd`，选择 Steam“浏览本地文件”打开的游戏根目录。Nexus：无需 PowerShell，按照随包说明手动复制。插件路径为 `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`。升级保留配置与存档；回退前备份旧 DLL 和 `BepInEx/config/local.kingdom.advisor.cfg`。卸载仅移除 KingdomAdvisor 插件目录，保留共享前置与其他 Mod。

[中文完整说明](https://github.com/boyl/kingdom-advisor/blob/v0.6.15/README.md) · [Nexus 手动安装与目录示意](https://github.com/boyl/kingdom-advisor/blob/v0.6.15/distribution/nexus/README.zh-CN.md)

## English

- Fixed recruitment-alert hit areas so the visible alert can be dragged independently, without snapping back during a drag, and its position is saved on release.
- Fixed English and multiline text overflow and overlapping controller hints. The advisor defaults to one column, with an optional two-column layout, more spacing, and independently draggable recruitment alerts alongside the map footer, distinguished by Map/Kingdom labels and colors; narrow layouts wrap. At 0 opacity, text, icons and the day-progress bar remain visible.
- Added **Hold to buy / upgrade**, off by default. Keep holding the native pay button to repeat purchases, recruitment or upgrades on the same target; release to stop. Includes citizen houses and fire barrels, while retaining native costs, stock limits and construction times. Supports solo play, local co-op and the host's local players; joining online clients are not yet supported.
- Added an option to hide the native purse graphic while unlimited capacity is enabled, off by default, while keeping balance numbers in the advisor. Fixed the 1,000-coin sailing cap with unlimited capacity.
- Spread periodic building scans across frames to reduce movement stutter spikes.

Different hermits remain separate. Equipment queues are unchanged, and per-entry advisor filtering is not included. Settings and saves are retained; no new dependencies. All DLC, resolutions and two-machine multiplayer scenarios have not been fully retested.

Choose **ModOnly** if you have a working BepInEx 6 IL2CPP installation, or **Complete** for a fresh installation. Complete retains the existing modified BepInEx #788 runtime, two Cpp2IL compatibility fixes, licenses and corresponding sources. Stock official #788 does not include these first-generation fixes.

GitHub: install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6), save and quit, extract the ZIP, then run `INSTALL.cmd` and select the game root via Steam's Browse local files. Nexus: no PowerShell is required; follow the included manual installation guide. Plugin path: `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`. Upgrades retain settings and saves. Back up the old DLL and `BepInEx/config/local.kingdom.advisor.cfg` for rollback. Uninstall only the KingdomAdvisor plugin folder, retaining the shared loader and other mods.

[Full English guide](https://github.com/boyl/kingdom-advisor/blob/v0.6.15/README.en.md) · [Nexus manual installation and directory layout](https://github.com/boyl/kingdom-advisor/blob/v0.6.15/distribution/nexus/README.en.md)


# 王国顾问 0.6.14 / Kingdom Advisor 0.6.14

- 修复英文和多行详情溢出、手柄提示遮挡；顾问默认单列，可切换两列，恢复行距与分组留白，可招募提醒放在上方。透明度 0 时只保留文字、图标与昼夜进度。
- 新增“长按连续购买／升级”开关，默认关闭。持续按住投币键即可对同一目标连续购买、招募或升级，松手停止；覆盖市民之家和火焰桶，保留原生费用、库存与施工限制。支持单人、本地双人及房主本机角色，暂不支持联机加入者。
- 新增无限容量时隐藏原生钱袋图形的开关，默认关闭，保留顾问中的余额数字；修复无限钱袋乘船后金币被截为 1000 的问题。
- 分摊周期性建筑扫描，降低走动时的卡顿尖峰。

不合并不同隐士，不扩展装备队列。本次不含逐条顾问筛选。保留配置与存档，不新增依赖。全部 DLC、分辨率及双机联机场景未完整复核。

已有正常工作的 BepInEx 6 IL2CPP 前置请选择 **ModOnly**；首次安装请选择 **Complete**，内含既有修订版 BepInEx #788、两项 Cpp2IL 兼容补丁、许可证与对应源码。官方原版 #788 不含这些首次生成修复。

GitHub：安装 [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6)，保存并退出游戏，解压后双击 `INSTALL.cmd`，选择 Steam“浏览本地文件”打开的游戏根目录。Nexus：无需 PowerShell，按照随包说明手动复制。插件路径为 `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`。升级保留配置与存档；回退前备份旧 DLL 和 `BepInEx/config/local.kingdom.advisor.cfg`。卸载仅移除 KingdomAdvisor 插件目录，保留共享前置与其他 Mod。

[中文完整说明](https://github.com/boyl/kingdom-advisor/blob/v0.6.14/README.md) · [Nexus 手动安装与目录示意](https://github.com/boyl/kingdom-advisor/blob/v0.6.14/distribution/nexus/README.zh-CN.md)

## English

- Fixed English and multiline text overflow and overlapping controller hints. The advisor defaults to one column, with an optional two-column layout, more spacing, and recruitment alerts above it. At 0 opacity, text, icons and the day-progress bar remain visible.
- Added **Hold to buy / upgrade**, off by default. Keep holding the native pay button to repeat purchases, recruitment or upgrades on the same target; release to stop. Includes citizen houses and fire barrels, while retaining native costs, stock limits and construction times. Supports solo play, local co-op and the host's local players; joining online clients are not yet supported.
- Added an option to hide the native purse graphic while unlimited capacity is enabled, off by default, while keeping balance numbers in the advisor. Fixed the 1,000-coin sailing cap with unlimited capacity.
- Spread periodic building scans across frames to reduce movement stutter spikes.

Different hermits remain separate. Equipment queues are unchanged, and per-entry advisor filtering is not included. Settings and saves are retained; no new dependencies. All DLC, resolutions and two-machine multiplayer scenarios have not been fully retested.

Choose **ModOnly** if you have a working BepInEx 6 IL2CPP installation, or **Complete** for a fresh installation. Complete retains the existing modified BepInEx #788 runtime, two Cpp2IL compatibility fixes, licenses and corresponding sources. Stock official #788 does not include these first-generation fixes.

GitHub: install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6), save and quit, extract the ZIP, then run `INSTALL.cmd` and select the game root via Steam's Browse local files. Nexus: no PowerShell is required; follow the included manual installation guide. Plugin path: `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`. Upgrades retain settings and saves. Back up the old DLL and `BepInEx/config/local.kingdom.advisor.cfg` for rollback. Uninstall only the KingdomAdvisor plugin folder, retaining the shared loader and other mods.

[Full English guide](https://github.com/boyl/kingdom-advisor/blob/v0.6.14/README.en.md) · [Nexus manual installation and directory layout](https://github.com/boyl/kingdom-advisor/blob/v0.6.14/distribution/nexus/README.en.md)


# 王国顾问 0.6.13 / Kingdom Advisor 0.6.13

- 修复倒计时跳动，继续跟随游戏暂停与变速。
- 岛屿、天数、阶段、游戏时间和季节保持单行显示。
- 建筑遗漏与错误识别：盲修复。

单人已实测正常速度、2倍、短时4倍、持续暂停与恢复、入夜／黎明切换；单行信息栏已实机确认。双机联机及全部语言／字号／分辨率组合尚未完整复核。倒计时不代表敌人到达或安全返程时间，不含返程估算。

已有正常工作的 BepInEx 6 IL2CPP 前置请选择 **ModOnly**；首次安装请选择 **Complete**，内含既有的修订版 BepInEx #788、两项 Cpp2IL 兼容补丁、许可证与对应源码，不新增依赖。官方原版 #788 不能替代此兼容包的首次生成修复。

GitHub 包：先安装 [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6)，退出游戏，解压后双击 INSTALL.cmd，选择 Steam“浏览本地文件”打开的游戏根目录。Nexus 包：无需 PowerShell，退出游戏，把包内 BepInEx 文件夹合并到游戏根目录；Complete 按随包 README 同时复制前置文件。Mod 路径为 `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`。升级保留配置与存档；回退前备份旧 DLL 和 `BepInEx/config/local.kingdom.advisor.cfg`；卸载只移除 KingdomAdvisor 插件目录，保留共享前置与其他 Mod。

[完整中文安装说明](https://github.com/boyl/kingdom-advisor/blob/v0.6.13/README.md) · [Nexus 手动安装说明及目录示意](https://github.com/boyl/kingdom-advisor/blob/v0.6.13/distribution/nexus/README.zh-CN.md)

## English

- Fixed countdown jitter while retaining pause and speed tracking.
- Kept island, day, phase, game time and season on one line.
- Building omissions and misidentification: blind fixes.

Single-player tests covered normal speed, 2x, a short 4x run, sustained pause/resume and nightfall/dawn; the single-line information row was checked in game. Two-machine multiplayer and all language/font/resolution combinations remain incompletely verified. The countdown is not an enemy-arrival or safe-return estimate; return travel is not estimated.

Choose **ModOnly** for an existing working BepInEx 6 IL2CPP loader, or **Complete** for a fresh installation. Complete retains the existing patched BepInEx #788 runtime, two Cpp2IL compatibility patches, licenses and corresponding sources; no new dependencies are introduced. Stock official #788 does not include these first-generation compatibility fixes.

For GitHub packages, install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6), quit the game, extract the ZIP, then run INSTALL.cmd and select the game root using Steam's Browse local files. Nexus packages require no PowerShell: quit, merge the included BepInEx folder into the game root, and follow the included README for Complete's additional loader files. Plugin location: `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`. Keep settings and saves on upgrade. Back up the previous DLL and `BepInEx/config/local.kingdom.advisor.cfg` for rollback. Uninstall only the KingdomAdvisor plugin folder, retaining the shared loader and other mods.

[Full English installation guide](https://github.com/boyl/kingdom-advisor/blob/v0.6.13/README.en.md) · [Nexus manual installation and directory layout](https://github.com/boyl/kingdom-advisor/blob/v0.6.13/distribution/nexus/README.en.md)

# 王国顾问 0.6.12 / Kingdom Advisor 0.6.12

0.6.12 新增昼夜时钟：显示黎明、白昼、黄昏、夜晚阶段、游戏内几点几分、距入夜／黎明的现实时间倒计时，以及昼夜进度条。钟点默认显示，可在 F6 → 移动与时间中关闭；阶段、倒计时和进度条保留。读数跟随游戏暂停和变速，明确区分“游戏时间”与“现实时间”。

- 正常速度依然使用游戏自身压缩的昼夜时间，并非与现实钟表同步；倒计时按当前速度换算实际等待时长。
- 已实机验证单人正常／2倍／4倍速度、暂停保持与恢复、入夜／黎明切换，以及关闭和恢复钟点。新增读取只读，不改变游戏时间或存档。
- 双机联机、完整 DLC 和所有语言／字号／分辨率组合尚未实机验证。长日期行可能自动换行；倒计时不代表敌人到达或安全返程时间。本次不含返程估算。

已有正常工作的 BepInEx 6 IL2CPP 前置请选择 **ModOnly**；首次安装请选择 **Complete**，内含既有的修订版 BepInEx #788、两项 Cpp2IL 兼容补丁、许可证与对应源码，不新增依赖。官方原版 #788 不能替代此兼容包的首次生成修复。

GitHub 包：先安装 [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6)，退出游戏，解压后双击 INSTALL.cmd，选择 Steam“浏览本地文件”打开的游戏根目录。Nexus 包：无需 PowerShell，退出游戏，把包内 BepInEx 文件夹合并到游戏根目录；Complete 按随包 README 同时复制前置文件。Mod 路径为 `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`。升级保留配置与存档；回退前备份旧 DLL 和 `BepInEx/config/local.kingdom.advisor.cfg`；卸载只移除 KingdomAdvisor 插件目录，保留共享前置与其他 Mod。

[完整中文安装说明](https://github.com/boyl/kingdom-advisor/blob/v0.6.12/README.md) · [Nexus 手动安装说明及目录示意](https://github.com/boyl/kingdom-advisor/blob/v0.6.12/distribution/nexus/README.zh-CN.md)

## English

0.6.12 adds a day/night clock: dawn, day, evening and night phases, in-game hours and minutes, a real-time countdown to nightfall or dawn, and a day-progress bar. The clock is enabled by default and can be hidden under F6 → Movement & time while retaining the phase, countdown and progress bar. Readings follow game pauses and speed changes, with explicit Game time and real time labels.

- Normal speed still uses the game's compressed day/night cycle; it does not match a wall clock. The countdown estimates actual waiting time at the current speed.
- Single-player testing covered normal/2x/4x speed, sustained pause/resume, nightfall/dawn and hiding/restoring clock digits. The new clock reader does not change game time or saves.
- Two-machine multiplayer, all DLC and every language/font/resolution combination remain unverified. Long date lines may wrap. The countdown is not an enemy-arrival or safe-return estimate; no return-travel estimate is included.

Choose **ModOnly** for an existing working BepInEx 6 IL2CPP loader, or **Complete** for a fresh installation. Complete retains the existing patched BepInEx #788 runtime, two Cpp2IL compatibility patches, licenses and corresponding sources; no new dependencies are introduced. Stock official #788 does not include these first-generation compatibility fixes.

For GitHub packages, install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6), quit the game, extract the ZIP, then run INSTALL.cmd and select the game root using Steam's Browse local files. Nexus packages require no PowerShell: quit, merge the included BepInEx folder into the game root, and follow the included README for Complete's additional loader files. Plugin location: `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`. Keep settings and saves on upgrade. Back up the previous DLL and `BepInEx/config/local.kingdom.advisor.cfg` for rollback. Uninstall only the KingdomAdvisor plugin folder, retaining the shared loader and other mods.

[Full English installation guide](https://github.com/boyl/kingdom-advisor/blob/v0.6.12/README.en.md) · [Nexus manual installation and directory layout](https://github.com/boyl/kingdom-advisor/blob/v0.6.12/distribution/nexus/README.en.md)

# 王国顾问 0.6.11 / Kingdom Advisor 0.6.11

0.6.11 显示自定义更新：地图标记支持按类型勾选、常用清单和分类批量操作；地图、顾问、附近详情、提醒及主界面的背景模式和独立不透明度集中在“显示与外观”，支持0～100%滑动条和统一应用。主界面默认不透明度71%，其他信息面板默认无背景。采用暖炭灰与旧金色配色，修正单项动作菜单、默认数值展示及地图提示重复底色。

- F6 → 地图标记：单击切换城墙、塔、农舍、浆果丛等类型；隐藏不影响防线提醒，仍可从地点列表临时定位。
- F6 → 显示与外观：鼠标拖动或键盘／手柄左右微调1%；恢复默认与显示数值一致。
- 升级保留现有配置和存档，旧大类筛选自动迁移；更新前建议备份 `BepInEx/config/local.kingdom.advisor.cfg`，回退旧版时一并还原。已有显式透明度值不被新的71%默认覆盖。

本次显示功能已实机验收；22项自动回归及API审计通过。双机联机、完整DLC及全部分辨率/手柄组合未重新完整验收。前置保持原有修订版BepInEx #788，不新增依赖。

已有正常前置选择 **ModOnly**，首次安装选择 **Complete**。GitHub包附PowerShell 7安装入口；Nexus包为无脚本手动安装。退出游戏后按照包内中英文README操作；Mod路径为 `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`。卸载只移除该Mod目录，保留共享前置和其他插件。

## English

0.6.11 adds individual map-marker checkboxes, a Common list and category-wide selection. Display & appearance groups independent 0–100% background-opacity sliders for the map, advisor, nearby details, alerts and main interface, with an apply-to-all option. The main interface defaults to 71% opacity; information overlays remain background-free by default. Includes a charcoal-and-muted-gold palette and fixes for action menus, default-value displays and the map tooltip background.

- F6 → Map markers: toggle walls, towers, farmhouses, berry bushes and other types with one click. Hidden markers remain available in Locations; defense alerts are unaffected.
- F6 → Display & appearance: drag sliders or adjust in 1% steps with keyboard/gamepad.
- Existing saves and settings are retained; old category filters migrate automatically. Back up `BepInEx/config/local.kingdom.advisor.cfg` before upgrading and restore it with the old DLL when rolling back. Existing explicit opacity values are retained.

The display changes have been tested in-game; 22 automated regressions and the API audit passed. Two-machine multiplayer, all DLC and every resolution/controller combination have not been revalidated. The existing patched BepInEx #788 runtime is unchanged.

Choose **ModOnly** for a working loader, or **Complete** for a fresh install. GitHub includes PowerShell 7 installers; Nexus uses script-free manual packages. Quit the game and follow the included bilingual README. The plugin path is `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`. Uninstall only the KingdomAdvisor plugin folder, retaining the shared loader and other mods.

# 王国顾问 0.6.10 / Kingdom Advisor 0.6.10

修正联机加入者因职业 AI 被游戏禁用而漏计工人等人口的问题。职业判断不再依赖 AI 启用状态，仍过滤回收实体并避免派生职业重复统计；保留游戏原生同步的弓箭手数量和未细分领队数量。客户端营地位置继续显示，未同步的营地人数不再误报为 0。敌人与营地继续使用场景实体读取。

构建、19 项自动回归及只读 API 审计通过。逆向核对基于游戏 2.4.3；双机联机、重连、换岛及完整 DLC 实机验证尚未完成。保留原有前置、配置、存档和单人/房主读取方式。已有前置选 ModOnly，首次安装选 Complete；GitHub 提供 PowerShell 7 安装包，Nexus 提供无脚本手动包。升级前退出游戏并保留配置，完整安装与卸载步骤见包内中英文 README。

Fixes missing joiner population counts when the game disables local role AI. Classification no longer depends on AI enable state; inactive pooled entities and overlapping derived roles remain excluded. Native synchronized archer and unclassified leader counts are preserved. Client camp locations remain visible, while unsynchronized occupant counts are no longer shown as zero. Enemy and camp scene discovery is retained.

Build, 19 automated regressions and the API audit passed. Native analysis used game 2.4.3; two-machine multiplayer, reconnects, island transitions and full DLC runtime coverage remain unverified. Existing loader, settings, saves and host/single-player sources are preserved. Choose ModOnly for a working loader or Complete for a fresh install. GitHub offers PowerShell 7 installers; Nexus offers script-free manual packages. Quit the game before upgrading and keep your settings. Bilingual installation and removal instructions are included.

# 王国顾问 0.6.9 / Kingdom Advisor 0.6.9

联机加入者的人口、难民营和敌人改从已同步的场景实体读取，避免依赖可能仅在房主端维护的管理器列表。人口按启用职业组件分类并去重，敌人列表每秒刷新，位置持续更新。房主与单人保留管理器数据来源。诊断文件新增 entityReadSource。

此版本经用户授权作为正式版发布。构建、API 审计和自动回归通过；双机联机、完整 DLC 与真实场景回归尚未完成，仍需玩家反馈确认效果。客户端未同步的实体无法由本地扫描恢复；营地内部人数仍沿用游戏列表。

保留现有前置、配置和存档格式。已有正常前置选择 ModOnly；首次安装选择 Complete。GitHub 包含 PowerShell 7 安装入口，Nexus 为无脚本手动包；包内附中英文安装、目录和回滚说明。若加入者仍异常，请在保持联机游玩时保存 BepInEx/LogOutput.log、BepInEx/config/KingdomAdvisor/runtime.txt 并附截图。

Online joiners now read population, beggar camps and enemies from synchronized scene entities instead of relying on manager registries that may be host-only. Population is deduplicated and classified by active role components. Enemy discovery refreshes once per second while positions continue updating. Host and single-player manager sources are retained. Diagnostics include entityReadSource.

Published as a stable release at the user's explicit request. Build, API audit and automated regressions passed; two-machine multiplayer, full DLC coverage and real-game regression testing remain unverified. This is a speculative compatibility fix: scene scanning cannot recover entities the game does not synchronize. Camp occupant counts still use the game's existing list.

Existing loader, configuration and save formats are preserved. Choose ModOnly for a working loader or Complete for a fresh installation. GitHub bundles include a PowerShell 7 installer; Nexus bundles use manual installation without scripts. Bilingual installation and rollback instructions are included. For persistent joiner issues, collect BepInEx/LogOutput.log and BepInEx/config/KingdomAdvisor/runtime.txt while still playing online, with a screenshot.

# 0.6.8 更新说明 / Release notes

修正联机加入者控制的二号玩家可能被双人模式标志漏读的问题，加入者使用主视口，本地双人仍保留分屏视口。摄像机缺失、停用或零尺寸时使用完整默认视口。世界、敌人、玩家详情及诊断对象清单读取分别隔离；可选数据连续失败三次后只停用对应通道，换场景或 F6/F7 显式重试可恢复。远端角色不参与本机输入阻断。

2026-10-11 用户验收并授权发布。0.6.8 与验收候选 0.6.8-rc.1 仅版本标记不同；自动玩家路由、故障恢复及原有回归通过。本机运行时证据为单人 Playing；未独立完成双机或完整 DLC 矩阵验证。若加入者仍异常，请提供其联机期间的 BepInEx/LogOutput.log 和 BepInEx/config/KingdomAdvisor/runtime.txt。

沿用兼容前置，不新增依赖或修改配置/存档格式。GitHub 提供 ModOnly 与 Complete（含安装脚本，需要 PowerShell 7）；Nexus 两种包均为手动安装，无自动脚本，附中英文安装说明及目录示意图。

Fixes the local second player being omitted by a two-player mode flag and selects the main viewport for an online joiner, while retaining local split-screen. Missing, disabled or zero-size cameras use the default full viewport. World, enemy, player-detail and object-census reads are isolated; three consecutive failures disable only that channel until a scene change or an explicit F6/F7 retry. Remote actors are excluded from local input blocking.

Accepted by the user for release on October 11, 2026. The final plugin differs from 0.6.8-rc.1 only in its version marker. Automated routing, recovery and existing regressions passed; local runtime evidence is single-player, with independent two-machine/full-DLC testing still outstanding. For persistent joiner issues, provide that client's BepInEx/LogOutput.log and BepInEx/config/KingdomAdvisor/runtime.txt from the online session.

Uses the existing compatible loader and preserves settings/saves. GitHub includes ModOnly and Complete installers requiring PowerShell 7; Nexus uses script-free manual packages with bilingual placement guides and an installation diagram.

# 0.6.7-r2 Nexus 手动资源包 / Nexus manual packages

Nexus 两种资源移除自动安装脚本，直接提供可复制到游戏目录的文件，普通玩家不再需要 PowerShell 7。中英文手动说明、安装后目录和 PNG 示意图随包提供，Complete 保留许可证并链接公开固定版本源码，包中无嵌套归档。GitHub 0.6.7-r1 一键安装包保留。插件及前置二进制与已验收版本相同，游戏内版本仍为 0.6.7。Nexus 主页另提供双语 BBCode，由作者手动粘贴更新。

Both Nexus packages now use manual installation without scripts or a PowerShell requirement. Includes bilingual placement instructions and PNG diagram, with licenses and public pinned source links for Complete. No nested archives. GitHub 0.6.7-r1 one-click assets remain available. Accepted plugin/runtime binaries are unchanged; plugin version remains 0.6.7. A bilingual homepage BBCode draft is provided for the author to paste manually.

# 0.6.7-r1 安装包修订 / Distribution revision

移除添加、清空资源等操作结果提示。提供 ModOnly（需已有正常前置）和 Complete（本 Mod＋修复后的 BepInEx #788）两种包，均包含中英文目录示意及安装脚本。插件版本仍为 0.6.7。

Complete 修复游戏 2.4.2 首次生成时 AndroidManager / RawPropertyType 空引用；仅修改两个 Cpp2IL 解析器 DLL，分发源码、补丁脚本和许可证。首次生成、重启及用户验收正常。安装器校验源与目标文件，备份旧核心、运行时和缓存，保留配置、存档及其他插件，并支持失败恢复与手动回滚。PowerShell 7 需单独安装。

Removes action-result notifications. Offers ModOnly for existing working loaders and Complete with the patched BepInEx #788 runtime. Both include bilingual placement diagrams and an installer. The plugin version remains 0.6.7.

Complete addresses the AndroidManager / RawPropertyType error during fresh generation on game 2.4.2. Only two Cpp2IL parser DLLs are modified; sources, patch script and licenses are included. Fresh generation, restart and user acceptance passed. The installer verifies source/target hashes, backs up old runtime/cache files, preserves settings, saves and other plugins, and supports failure recovery and manual rollback. Install PowerShell 7 separately.

# 0.6.7 更新说明 / Release notes

修复关闭顾问面板后键鼠输入可能无法恢复的问题。键鼠关闭不再等待手柄回中；未连接手柄、设备读取失败或松开输入时清除遗留拦截。保留手柄按住关闭时的防误触保护。以正式 0.6.6 为基线，保留配置和存档，无新增依赖；不包含自定义坐骑。

2026-10-10 用户验收输入恢复正常。自动输入回归与现有功能检查通过；实体手柄专项、全部关闭路径、双人联机和完整 DLC 矩阵未逐项实测。此更新不修复 Cpp2IL RawPropertyType 启动异常。

Fixes keyboard/mouse input remaining blocked after closing the Advisor panel. Closing with keyboard/mouse no longer waits for a gamepad to return to neutral; stale release guards clear when a gamepad is absent, unavailable or released. Keeps held-gamepad input protection. Based on released 0.6.6, preserving settings and saves with no new dependencies. Custom mounts are not included.

User accepted normal input recovery on October 10, 2026. Automated regression checks passed; physical gamepad scenarios, every close path, multiplayer and the full DLC matrix have not all been individually tested. This update does not fix the Cpp2IL RawPropertyType startup error.

Requires BepInEx 6 Unity IL2CPP Windows x64; tested with game 2.4.2 and BepInEx 6.0.0-be.738. BepInEx is not bundled. Extract the package into the folder containing KingdomTwoCrowns.exe; the final plugin path is BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll. Installation directory diagrams are included in both README files.


# 0.6.6 更新说明 / Release notes

全部104条图鉴按身份明确映射主体＋用途／特征像素图标，完整图案互异；兴趣点改为地点，补充方向、距离、作用与原生交互状态，以及图鉴、地点和地图之间的联动。排除玩家及当前骑乘坐骑的冗余地点。中英文同步。

Adds explicit identity-based icons for all 104 catalog entries, with distinct object/purpose combinations and catalog/location/map links. Locations include direction, distance, purpose and native interaction state. Excludes monarchs and ridden mounts from redundant locations. Chinese and English included.

# 0.6.5 更新说明 / Release notes

补齐城墙、箭塔、农舍等设施的隐士特殊改造说明。读取游戏实际passengerUpgrades，显示改造用途及乘员条件；普通升级与改造目标共用组件识别，已建号角墙优先依据原生标记识别。中文与英文同步处理。保留已有配置和存档，无新增依赖。

15项自动检查通过，149项游戏API审计通过。已安装0.6.5启动并进入Playing，faulted=False。六类改造尚未逐项实机操作及视觉验收，全部DLC和联机矩阵未完整验证。

Adds hermit conversion descriptions for walls, towers, and farms from actual passenger upgrade options. Shows their purpose and passenger requirements, uses component identities for upgrade targets, and recognizes existing horn walls through native markers. Chinese and English included; existing settings and saves are preserved, with no new dependencies.

15 automated checks and 149 game API calls audited. Version 0.6.5 loaded and entered gameplay without an advisor fault. All six conversion types have not been individually operated or visually verified; the full DLC and multiplayer matrix remains unverified.

# 0.6.4 更新说明 / Release notes

修正具体交互对象被通用名称覆盖的问题：雕像、锻造商品、隐士与升级结果优先采用游戏原生身份。侍从与骑士分别统计，盾旗招募、剑升级与普通盾牌装备分别说明。

熊洞现显示熊的解锁与能力说明。图鉴扩展至104条，29类坐骑覆盖当前游戏39个有效坐骑身份，补齐6类特殊任务对象。新增内容均提供中文及英文，保留未知身份的明确提示。更新保留已有配置与存档，不增加必需依赖。

14项自动检查通过；已安装0.6.4的熊洞与图鉴完成中英文、字体18/28实机检查。其它DLC能力、原截图雕像/锻造现场及非零侍从招募尚未逐项实机验收，联机完整矩阵仍未验证。

Fixes generic descriptions overriding native identities for statues, forge items, hermits, and upgrade results. Squires and knights are counted separately; squad recruitment, promotion equipment, and ordinary shields have distinct descriptions.

Bear caves now describe the bear unlock and abilities. The catalog contains 104 entries, including 29 mount types covering 39 native mount identities and 6 additional special quest objects. Chinese and English text are included. Existing settings and saves are preserved; no new dependencies are required.

14 automated checks passed. Bear-cave and catalog screens were checked in-game in both languages at font sizes 18 and 28. Other DLC abilities, the original statue/forge scenes, nonzero squire recruitment, and the full multiplayer matrix remain unverified.

# 0.6.2 更新说明 / Release notes

- 补齐购买后生成的炸弹名称与作用，中英文均可显示。
- 32种明确原生对象类型按实际身份识别，减少模型名称变化导致的遗漏。
- 发布门禁检查当前场景未知交互物；保留原有传送及失冠后的游戏行为。

自动分类、本地化与回归检查通过；当前岛屿62名角色计数核对通过。用户授权发布。炸弹修正经过真实对象清单与分类测试验证，修正后的炸弹画面未单独留证；全部主题、DLC及联机矩阵未重新验证。

Adds bilingual descriptions for spawned bombs, classifies 32 native object types, and blocks release when the checked scene contains unknown interactables. Teleport and native crown-loss behavior remain unchanged. Automated checks and current-scene population verification passed; the full DLC and multiplayer matrix was not retested.
# 0.6.1 更新说明 / Release notes

- 修正渔夫重复计入长枪兵；补充人口分类，并增加逐角色核对。
- 附近建筑详情默认开启，放在顾问旁；统一文字、图标列与行距，保留简短作用说明并取消分页。
- 排除玩家本人和当前坐骑，收紧建筑详情距离；补充满级、弩炮及面包改造设施读取。
- 补齐 Keep、左右炸弹旗与船只残骸的名称映射。
- 固定地图标签优先级及聚合锚点，减少跑动时重新排布。

本轮已通过自动测试、二进制审计和当前存档98角色实机计数核对。后期岛屿跑动、到达/离开建筑、满级改造建筑及中英文新布局已获用户最终验收；联机及全部DLC矩阵未重新验证。

Fixes duplicate fisher/pikeman counts, improves nearby building detection and descriptions, aligns the detail card with the advisor, and stabilizes map grouping and label priorities. Current-save population verification passed for 98 characters. Final movement, proximity and bilingual layout checks passed user acceptance. Multiplayer and the full DLC matrix were not retested.
# 0.6.0 更新说明 / Release notes

增加完整中英双语界面：图鉴、名称、说明、锁定原因、地图状态、顾问、设置、资源操作和手柄提示。
默认跟随游戏语言；中文显示简体中文，其他语言回退英文。可在F6语言选项中手动覆盖，修改即时生效。
翻译后再测量卡片、地图标签与分页；英文设置使用更宽分类栏和更高行距。中英文关键词搜索均支持。
保持0.5.8已验收的输入隔离、HUD显隐、地图坐标、资源与玩法规则，不更换原有配置键。

Adds Chinese and English localization throughout the HUD, catalog, object descriptions, lock reasons, map states, settings, resources and controller help.
Auto follows the game language: Chinese variants use Simplified Chinese; all other languages use English. Manual overrides apply immediately.
Translated text is measured before layout and pagination. English settings allow wider categories and taller rows. Catalog search accepts both languages.
Preserves the verified 0.5.8 input isolation, HUD toggle, map coordinates and gameplay behavior, including existing configuration keys.

修正中英文关闭按钮、快捷入口和翻页按钮裁切；设置页按说明实测高度预留空间，大字号下拉菜单支持两列。诊断模式覆盖中英两种语言、18/28字号、全部设置分类和主要页面。
