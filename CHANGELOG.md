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

## 0.7.0-preview.19

- 加入呆猫爆桶与呆狗冲锋、原生输入映射、0.5秒冷却与独立联机元数据通道。
- 修正运行时像素资源保留、换乘后输入锁、狗冲锋停止；蜥蜴派生能力定向阻断。
- 去掉坐骑弹出提示，HUD仅保留技能名称。
- 作为独立预览文件发布，已验证范围和未完成实机项见坐骑验收记录。
