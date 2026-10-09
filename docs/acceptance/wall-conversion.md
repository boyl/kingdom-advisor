# 城墙特殊改造说明补充（0.6.5候选）

问题：0.6.4附近说明只展示nextPrefab，遗漏passengerUpgrades，导致普通升级缺失时不显示仍然存在的隐士改造。

修正：只读检查实际passengerUpgrades目标，发现号角城墙时说明召集军队及需要带上号角隐士；普通墙不凭等级伪造改造。已建号角墙根据Wall.isHornUpgrade或PayableHorn识别，避免原名仍是Wall5时误显示普通墙。保持原生费用和锁定，不调用升级或支付接口。中文与英文同时补齐。

资料：[号角隐士](https://kingdomthegame.fandom.com/wiki/Horn_Hermit)。原生契约来自本机2.4.2互操作程序集离线读取：PayableUpgrade.passengerUpgrades为RequireTagUpgrade数组，目标prefab为GameObject；Wall.isHornUpgrade为bool；PayableHorn为原生交互类型。

验收：模型测试通过，覆盖普通墙/已改造墙、特殊目标与乘员条件、中英文。编译/API审计及全量翻译检查记录见本轮命令结果。实机读取该截图城墙的改造列表与候选安装仍待游戏退出后检查，不能以模型测试代替。

## 扩展核查与安装结果

同一根因涉及其它passengerUpgrades：修正扩展到全部原生乘员改造目标，六类已知隐士设施显示名称、用途、乘员条件；未知目标保留原生乘员标识，不编造已满足条件。普通nextPrefab也使用与当前建筑相同的组件识别逻辑，避免改造目标显示普通箭塔/农舍。已建号角墙不重复显示再次改造。

2026-10-09安装0.6.5，包与安装DLL SHA256均为389873EF65B3EDA9B682DC180AECBC50450AB832C33020F52E55A918BE096054。15项自动脚本通过，游戏API审计149项只读及已审查动作。BepInEx加载0.6.5并进入Playing，岛1第1天，faulted=False，movementLeakFrames=0；当前场景原生分类回归通过。未执行支付、传送、改造或存档还原操作。

目前场景未包含原截图的高阶城墙，也未逐一操作六类隐士改造；对应真实场景视觉验收仍待覆盖。Nexus/GitHub公开版保持0.6.4，本轮未发布0.6.5，不复用旧发布验收。
