# 王国顾问作用说明排查（2026-10-09）

## 已确认原因

截图位置与当前场景清单的 Statue Archer(Clone) 对应，原生类型为 Statue。旧目录只有通用 statue 条目，说明固定为“提供增益、任务或特殊交互。”。这属于语义缺失，不是字体、裁切、翻译缺失或安装版本漂移。修复前包与安装 DLL SHA256 均为 89344507133303DD01810748748B72BC875187451C8E53D4E7861708A8C8E52D；日志证明加载 0.6.2。

名称匹配还有两层缺陷：Statue Farmer 可被 farm 子串抢先识别；ResolveNative 又把 Statue 和 TimedStatue 一律覆盖回通用 statue。游戏接口直接提供 Statue.deity，有限值域为 Archer、Worker、Knight、Farmer、Time、Pike、Total。TimedStatue 继承 Statue；TimeStatue 独立继承 Payable，不应与普通增益雕像混淆。

## 修复与验收标准

|标准|结果与证据|
|---|---|
|四种常规雕像显示专属名称及用途|模型回归通过；test-statues.ps1 修复前真实失败，修复后通过|
|原生身份优先于冲突名称|Archer/Worker/Farmer/Knight × Statue/TimedStatue 通过|
|未知身份不伪造作用|Time/Pike/Total/Future 保留对象名称、明确尚未核实，通过|
|计时雕像不冒用农民作用|TimeStatue 与冲突名称用例通过|
|详情、地图、清单一致读取身份|三个入口改用 ResolvePayable；构建及二进制审计通过|
|中英文说明完整|全目录及全部显示文案翻译检查通过|
|现有行为无自动回归|12个测试/审计脚本全部通过；git diff --check 通过|
|新候选真实游戏显示|待验证；游戏仍运行旧版本，未覆盖或重启|
|其他 DLC 实机矩阵|未验证，不以类型测试替代|

用途文案仅描述明确效果，不写未核实倍率或持续时间。弓手提高精准度；工匠强化城墙；农民增加可支持地块；骑士获得突进攻击。Time/Pike 等仍需真实机制证据，当前明确显示未核实。

## 全量检查的边界与其他发现

当前对象清单覆盖22个非空原生类型。现有全目录检查只能证明文案非空和翻译完整，不能证明所有设施已具备主题、等级、隐士和坐骑子类的详细作用。雕像之外，坐骑、隐士、神祇任务设施等仍有通用文案，需要逐个机制核实；不能宣称全主题专属说明已齐全。

日志另有 BetterPayableUpgrade 2.4.2 的 MissingMethodException：ChallengeData.get_customSwapData。它在 AdjustCosts/OnGameStart 抛出，属于该 Mod 与当前游戏接口不兼容；不是本次固定通用说明的直接原因。费用来自 Payable.Price；不应据此保证第三方费用调整生效。本轮未修改第三方 Mod。

## 当前交付状态

源码修复、包重建、自动回归完成。未安装新候选、未实机验收、未提交、未远端发布；原 release-acceptance.json 哈希不匹配，原发布验收已失效，禁止复用它发布候选。新增雕像测试已加入发布门禁。

源码入口：C:/Users/lw/Documents/repo/kingdom-advisor。修复候选在该项目 package/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll。

说明核对资料：https://steamcommunity.com/sharedfiles/filedetails/?id=1588497381 与游戏本机 Assembly-CSharp 元数据；社区资料不替代当前 DLC 实机验收。
## 后续全量审查补充
本文件为首轮记录。Pike现已提供专属说明，TimeStatue单独识别为沙漏；未知Time/Total保留未核实边界。最终范围与证据以 game-content-accuracy.md、content-review-ledger.md 为准。截图与Archer对象的对应属于外观推断，未完成同现场实机验证。
