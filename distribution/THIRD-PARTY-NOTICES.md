# 第三方组件与修改声明 / Third-party components and modification notice

本整合包是兼容分发，不是 BepInEx 官方发布包。各组件保留各自许可证，不改变其原有版权与修改权利。

This is a compatibility distribution, not an official BepInEx release. Components retain their individual licenses, copyright and modification rights.

|组件 / Component|来源 / Source|许可证文件 / License|
|---|---|---|
|BepInEx #788|BepInEx/BepInEx commit 5b766a3b7f6c164d4798924a93f3acf4db769d06|BepInEx-LGPL-2.1.txt|
|Il2CppInterop 1.5.3|BepInEx/Il2CppInterop commit dbda1cb353b0f4253345dc45136d170b9e50a5a0|Il2CppInterop.txt|
|Cpp2IL / LibCpp2IL / StableNameDotNet / WasmDisassembler|SamboyCoding/Cpp2IL commit 558ddd98642010897d54316b51fbaa7889fda093|Cpp2IL-MIT.txt|
|UnityDoorstop 4.5.0|NeighTools/UnityDoorstop tag v4.5.0|UnityDoorstop.txt|
|.NET runtime 6.0.7|dotnet/runtime tag v6.0.7|dotnet-LICENSE.txt、dotnet-THIRD-PARTY-NOTICES.txt|
|Harmony|pardeike/Harmony|Harmony-MIT.txt|
|MonoMod|MonoMod/MonoMod|MonoMod-MIT.txt|
|Mono.Cecil|jbevain/cecil|Mono.Cecil-MIT.txt|
|AsmResolver|Washi1337/AsmResolver|AsmResolver-MIT.txt|
|AssetRipper.Primitives / CIL|AssetRipper/AssetRipper.Primitives、AssetRipper/AssetRipper.CIL|AssetRipper.Primitives-MIT.txt、uTinyRipper-MIT.txt、AssetRipper.CIL-MIT.txt|
|Iced|icedland/iced|Iced-MIT.txt|
|Dobby|BepInEx/Dobby release 1.0.5|Dobby-Apache-2.0.txt|
|Capstone / .NET binding|capstone-engine/capstone、9ee1/Capstone.NET|Capstone.txt、Gee.External.Capstone.txt|
|Disarm|SamboyCoding/Disarm|Disarm.txt|
|SemanticVersioning|adamreeve/semver.net|SemanticVersioning.txt|

许可证全文在 `licenses/`。BepInEx、Il2CppInterop、UnityDoorstop 的对应源码随包置于 `sources/`；Cpp2IL 的代码源码也包含在内，仅排除了上游 TestFiles 中的 Unity 测试二进制。源码包不安装进游戏。依赖可由对应项目的构建配置获取。该包不分发游戏程序集、Unity 基础库、用户配置或存档。

License texts are in `licenses/`. Corresponding BepInEx, Il2CppInterop and UnityDoorstop source archives are in `sources/`, alongside Cpp2IL code (upstream Unity binary fixtures in TestFiles excluded). Build dependencies are identified by upstream projects. Source archives are not installed into the game. No game-derived assemblies, Unity libraries, user configuration or saves are distributed.

## 本次改动 / Modifications (2026-10-10)

仅修改 #788 原包的 `LibCpp2IL.dll` 和 `Cpp2IL.Core.dll`：属性类型读取检查空 setter、空参数及空数组；生成时跳过没有可解析类型的属性；跳过没有生成属性对象的自定义属性复制。其余 `core`、`dotnet` 和根目录加载器来自原官方包，未经修改。可重复的二进制补丁源码是 `sources/patch-788-stage.ps1`，在 MIT 许可下提供。详见 `REPRODUCE.md`。

Only `LibCpp2IL.dll` and `Cpp2IL.Core.dll` were modified: validate null setter/parameters and empty parameter arrays, skip unresolvable properties during generation, and skip custom attributes when the generated property is absent. Other core/runtime/root loader files are unmodified official files. The reproducible binary-patch source `sources/patch-788-stage.ps1` is provided under the MIT license. See `REPRODUCE.md`.
