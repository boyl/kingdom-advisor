# 官方 #738 首次安装兼容性排查

日期：2026-10-10。正式 Mod 基线：0.6.7。当前状态：已复现并定位，完整修复尚未验收，不得发布两 DLL 补丁为完整解决方案。

## 验收标准

1. 使用可校验的官方前置包，在没有既有 interop/cache 的情况下完成生成。
2. 原生游戏成功启动并加载王国顾问，无访问冲突。
3. 对照不加载任何插件，区分加载器与 Mod 故障。
4. 测试结束恢复原始 core、interop、cache、plugins、配置及存档。
5. 修复交付不得含游戏派生 interop 程序集；需标明前置、游戏版本与证据范围。

## 证据范围

本机游戏 2.4.2，Unity 6000.0.66f2，元数据版本 31。第二位反馈用户尚未提供日志，不能将该用户问题宣称为同一根因或已解决。

官方包：BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.738+af0cba7.zip。

ZIP SHA256：FAD35DF35209FF667512962BBF7874495BA124E4C1B5CD12096FC09B5B9B34B3。

GameAssembly.dll SHA256：41B67DF804DC5E0682A6E4AFBE82DC70BFE46A9883084AEEF7D5CD9B43FEED86。

global-metadata.dat SHA256：F73C97FD74C9756022011EB5F51FF8D71155C2B88DAF363D360FF6ED90C862BC。

## 结果

|检查|结果|
|---|---|
|原始官方 Cpp2IL 独立首次生成|失败：精确复现 AndroidManager+<_InitiateSignIn>d__21_Server / RawPropertyType 空引用|
|加入属性空值检查后独立生成|通过：101 个中间程序集、131 个 interop DLL；此进程为 pwsh/.NET 10，不代表原生启动|
|修补官方 #738 原生首次生成|通过：全新缓存，.NET 6.0.7 原生预加载完成生成|
|修补官方 #738 原生启动|失败：生成之后，插件加载之前退出；Windows Application 1000，coreclr.dll，0xc0000005，偏移 0x1d1fdd|
|移走全部插件对照启动|失败：2026-10-10 10:17:07，同一模块、异常及偏移；排除该崩溃依赖王国顾问或其他插件加载|
|环境恢复|完成：原始 core/interop/cache/plugins/配置/存档已恢复；正式 DLL SHA256 与 0.6.7 发布一致|

本机此前工作环境的预加载器标识为 #738，但 Cpp2IL 与 Il2CppInterop DLL 已不同于官方包，Interop 为 1.5.1.0。因此此前本机可运行不能证明纯官方 #738 首装兼容。

属性补丁检查无 setter、无参数、空参数数组、空属性类型，并跳过不存在的生成属性的自定义属性复制。只修改暂存的 LibCpp2IL.dll 和 Cpp2IL.Core.dll。其原生生成成功但启动失败，故不作为完整修复交付。

## 证据位置与后续

实验文件位于 C:/Users/lw/Documents/Codex/2026-10-09/wan/work：probe-738-full.log、probe-738-patched-full.log、patch-738-stage.ps1。

原生日志及恢复备份位于其 738-runtime-trial 子目录：patched-738-native.log、patched-738-no-plugins.log。保留实验备份用于复核，未修改正式发布包。

下一步验证更新的官方 IL2CPP 运行时与属性解析修复的完整组合。尚不能凭模块名称确定访问冲突的具体原生函数；尚未验证更新组合的原生启动、Mod 加载与游戏交互。

一手参考：[Cpp2IL #471](https://github.com/SamboyCoding/Cpp2IL/issues/471)，记录相同属性问题及 Unity 6 游戏复现；[社区实现](https://github.com/SeaAndStars/KTC-mod/commit/2dc7947f2c7bb22fd99ff5a8b5555aa7e3e67c4c)仅作为候选路线，不能代替本项目验收。

## 其他纯官方包的迁移门禁

用户授权：测试其他官方包，通过后迁移。保持游戏输入数据一致；各候选在单独 pwsh 进程加载自身完整 core，使用空的新输出目录，不复用已安装 interop。此轮未替换游戏目录。独立生成是预筛选门禁，未通过者不进入原生启动、Mod 编译及迁移。

|官方 Windows IL2CPP x64 包|ZIP SHA256|独立首次生成|
|---|---|---|
|#782 + ac4f64a|5E6FF9D5917BA0D59381512E0512934A734915B347DE28CE43C2688977EA214A|失败：AndroidManager / RawPropertyType 空引用|
|#784 + 0523d6f|48774EE7FE51B5ED8C945FE3FEFD7C281C22371B80F22EA9CE03386FA251055D|失败：AsmResolver.DotNet.ModuleDefinition(string, AssemblyReference) 方法不存在|
|#785 + 6abdba4|2A7CBF74D26ABE4765C3E662DB1721B923BAC39849EBFEF2CA5DC7DE7E2D9B7F|失败：AndroidManager / RawPropertyType 空引用|
|#788 + 5b766a3|F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A|失败：AndroidManager / RawPropertyType 空引用|

日志：上述 work 目录的 probe-782-official.log、probe-784-official.log、probe-785-official.log、probe-788-official.log。脚本原有 catch 使用 exit 1；外层 shell 为展示日志继续执行，因此 shell 最终退出码不代表生成通过，判定依据日志中的 FAILED 与完整异常，未出现 SUCCESS。

官方构建页记录 #784 降级至 Cpp2IL pre-release.20，#785 因回归回退至 development.1452；#788 为本轮查询到的最新构建。候选覆盖回退前后解析器，不能据此声称所有官方历史版本均失败。

结论：四个候选均未通过迁移首项门禁，不迁移、不重新发布二进制。后续需要单独验证带属性解析修复的完整运行时组合；它应明确标为修改过的前置，不能称为纯官方包。当前未完成这条路线。

## #788 加解析器修复的候选验证

用户随后授权尝试新版运行时与解析器修复。基底 #788 + 5b766a3，Interop 1.5.3.0；仅改变 LibCpp2IL.dll 与 Cpp2IL.Core.dll，目录逐文件哈希对比确认其余 core 文件不变。补丁按新版属性上下文结构适配，不混用 #738 的解析器 DLL。

补丁 LibCpp2IL SHA256：E4A458D6144993336DEE769A66968EC4004B86AED2E22221C1B6B2A38CE7D421。

补丁 Cpp2IL.Core SHA256：2A6C78F3F8987DF17EAB19192AF1F0E19C41D3F0E274BF2F89AC2114426DF502。

|验收项|候选结果|
|---|---|
|独立首次生成|通过：101 中间程序集、131 桥接 DLL，约 76 秒|
|整套前置原生首次启动|通过：完整替换 BepInEx、dotnet、Doorstop 根文件，未复用旧 interop/cache，成功生成并加载 0.6.7|
|游戏状态与健康记录|通过：2026-10-10T02:37:51Z Playing，faulted=False，releaseGuardCount=0，movementLeakFrames=0|
|新程序集编译当前 Mod 源码|通过；候选仅存实验目录，正式包 DLL 已恢复|
|图鉴打开关闭、移动、第二次启动|待验证：游戏窗口最小化，工具恢复后仍未能观察界面，已请求用户恢复窗口|
|迁移及用户验收|尚未完成，不宣称完整修复或发布|

备份及证据：work/788-runtime-trial（相对本聊天目录），含 original-BepInEx、original-dotnet、原始根文件、save-before、fresh-start.log、fresh-runtime.txt、build-788.log。生成日志 work/probe-788-patched.log，可重复补丁脚本 work/patch-788-stage.ps1。不得分发该目录内的游戏派生程序集或存档。

启动过程中窗口尚未创建、日志缓冲未落盘时曾误判退出；后续查询确认同一 PID 3964 自 10:35:47 持续运行。没有证据表明 #788 候选发生启动崩溃。

### 后续实机验收

第二次启动成功，second-start.log 确认 #788 + 5b766a3，跳过重新生成并正常加载 0.6.7。F7 打开图鉴，catalog-open.txt 的 overlayOpen=True；F9 截图 catalog.png 已视觉检查，图标、中文说明及列表正常显示。F7 再次关闭，second-runtime.txt（2026-10-10T02:57:21Z）记录 Playing、overlayOpen=False、faulted=False、releaseGuardCount=0、disabledMovementMaps=0、movementLeakFrames=0。

Esc 关闭第一次打开的图鉴后游戏进入 Menu，再次 Esc 返回 Playing；不将该路径当作“关闭后仍 Playing”的证据。F7 路径单独验证通过。尚未进行有明确位移测量的移动验收、鼠标关闭、手柄、英文界面及其他用户环境验收。

通过 Alt+F4 正常退出并确认进程不存在，恢复测试前存档与配置。用户已授权通过后迁移，故本机保留完整 #788 + 两解析器补丁前置，正式 0.6.7 DLL SHA256 保持 35B792B099DFCFF5B919E793AF3A5DE851BA9A85A664C6F2EFAC3CF9307E2BCF。原前置完整备份仍保留。其他原有插件已恢复，但本轮启动只加载王国顾问，其他插件组合不标通过。

结论：本机首次生成、二次启动、中文图鉴及 F7 开关冒烟验收通过；尚不宣称跨用户完整验收或已发布兼容前置包。
