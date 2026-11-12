# 跨主题静态审计

`structure-coverage.json` 保存本次原生类型及资源名称／组件的盘点，不包含游戏美术、音频或程序集。游戏构建和输入索引哈希记在 JSON 中。角色、物件、内部生成器和证据不足类有独立处置，不能把非 unknown 数量当成实机通过数量。

复核方法：

1. 使用独立 Python 环境安装 `UnityPy==1.20.26`。这是离线审计工具依赖，不是 Mod 运行依赖。
2. 运行 `tools/extract-structure-index.py`，参数依次为本机游戏 `KingdomTwoCrowns_Data` 和输出目录。工具遍历全部 `.assets` 与 `level数字` 文件，仅读取 GameObject 和 MonoBehaviour 公共头的脚本引用。确认 `asset-audit-summary.json` 的 errors 为空。
3. 通过 PowerShell 7 运行 `audit-structures.ps1 -AssetIndex <生成的asset-gameobjects.json> -NativeDump <同一游戏构建的dump.cs> -OutputDirectory <输出目录>`。游戏路径默认从 Steam 注册表解析，可用 `-GamePath` 指定。
4. 工具组合原生 Payable 继承链、组件式付费接口、专用设施和任务标签。出现未审查类型时失败，新增通用类必须明确处置。

限制：元数据存在不代表当前主题有实例；同一资源可有多个场景副本；人物、坐骑、商店和雕像部分身份还取决于运行时枚举。资源清单保留入口层面的静态分类，运行时专用读取器和回归测试另外验证。不能凭类名替玩家解锁未购买的 DLC。
