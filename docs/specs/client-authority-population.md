# 客户端 AI 控制权与人口统计修复

状态：实施中。版本 0.6.10。用户已授权修复并发布；沿用此前正式发布及 Nexus API 上传要求。

## 目标与兼容约束

修复 0.6.9 将客户端禁用的职业 AI 误判为不存在的问题。保持单人/房主读取、配置、存档、快捷键和平台项目 ID 不变。不启用 AI，不修改游戏数据，不新增网络协议。

## 原生证据与方案

- 本机 2.4.3 / Revision 23537，IL2CPP metadata 31。Worker.Awake 0x8299D9 读取 HasWorldAuth，0x829A2C 设置 enabled；禁用不代表实体不存在。
- Character.Promote 0x986960 调用 ReplaceBy；ReplaceBy 0x987360 通过 Holder.GetCharacterByTag 获取新职业，再 Pool.Spawn；Promote 在 0x986BAE Pool.Despawn 旧实体。按活动实体的职业组件分类，不依赖组件 enabled；Ninja/Pikeman 优先于 Fisher。
- Kingdom.ArcherCount 0x5AE3B0 在客户端读取 _receivedArcherCount（0x398）；保留原生同步计数。KnightCount 同样用于未细分领队数量。
- 客户端营地 `_beggars` 不能当成已同步人数：保留位置，人数使用已有 -1 未知语义，避免显示错误的 0。
- 敌人与营地继续沿用 0.6.9 的客户端场景扫描；本次不扩展其他地图类别。

## 任务与验收

- [x] 在旧代码上加入 disabled 职业组件场景，确认测试失败。
- [x] 客户端按组件身份分类，活动实体过滤、去重和职业优先级保留。
- [x] 保留弓箭手同步计数及未细分领队；客户端营地不伪报人数。
- [x] 19 项自动回归、构建、150 个 API 调用审计；安装包检查见后续记录。
- [ ] 版本、说明、提交与制品一致；GitHub / Nexus 原位正式发布后核验。
- [ ] 双机实测、重连、换岛及用户原游戏版本验证（当前无第二客户端，不声明通过）。

## 验证记录

2026-10-24：旧 ReadClientPopulation 在 disabled Worker/Ninja/Pikeman/Knight 样本下失败；改为组件身份后相同场景通过。额外覆盖全部职业、对象池旧实体、侍从、重复实体、空场景。自动测试无法替代游戏内同步完整性验收。此前用户已接受缺少双机实测的正式发布，本次仍明确保留此限制，不复用旧版实机通过状态。

原生 KnightCount（0x5AE860）也确认在客户端读取 _receivedKnightCount（0x39C）。候选 DLL SHA256：426068D067F69D21EEFF2AB34700D6BF198DB67D9B46F9928397A85017CCCEB5。
