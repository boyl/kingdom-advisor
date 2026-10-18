# 王国顾问 0.6.9 / Kingdom Advisor 0.6.9

联机加入者的人口、难民营和敌人改从已同步的场景实体读取，避免依赖可能仅在房主端维护的管理器列表。人口按启用职业组件分类并去重，敌人列表每秒刷新，位置持续更新。房主与单人保留管理器数据来源。诊断文件新增 entityReadSource。

此版本经用户授权作为正式版发布。构建、API 审计和自动回归通过；双机联机、完整 DLC 与真实场景回归尚未完成，仍需玩家反馈确认效果。客户端未同步的实体无法由本地扫描恢复；营地内部人数仍沿用游戏列表。

保留现有前置、配置和存档格式。已有正常前置选择 ModOnly；首次安装选择 Complete。GitHub 包含 PowerShell 7 安装入口，Nexus 为无脚本手动包；包内附中英文安装、目录和回滚说明。若加入者仍异常，请在保持联机游玩时保存 BepInEx/LogOutput.log、BepInEx/config/KingdomAdvisor/runtime.txt 并附截图。

Online joiners now read population, beggar camps and enemies from synchronized scene entities instead of relying on manager registries that may be host-only. Population is deduplicated and classified by active role components. Enemy discovery refreshes once per second while positions continue updating. Host and single-player manager sources are retained. Diagnostics include entityReadSource.

Published as a stable release at the user's explicit request. Build, API audit and automated regressions passed; two-machine multiplayer, full DLC coverage and real-game regression testing remain unverified. This is a speculative compatibility fix: scene scanning cannot recover entities the game does not synchronize. Camp occupant counts still use the game's existing list.

Existing loader, configuration and save formats are preserved. Choose ModOnly for a working loader or Complete for a fresh installation. GitHub bundles include a PowerShell 7 installer; Nexus bundles use manual installation without scripts. Bilingual installation and rollback instructions are included. For persistent joiner issues, collect BepInEx/LogOutput.log and BepInEx/config/KingdomAdvisor/runtime.txt while still playing online, with a screenshot.
