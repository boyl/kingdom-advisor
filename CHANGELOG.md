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
