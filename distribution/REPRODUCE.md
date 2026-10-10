# 解析器补丁复现 / Reproducing parser modifications

普通用户只需运行 INSTALL.cmd，不必执行此页步骤。

Users only need INSTALL.cmd; these steps are for reproducing the modifications.

1. 下载并解压原始官方包 / Download and extract:
   https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip
2. 原 ZIP SHA256 / Original ZIP hash:
   `F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A`
3. 将解压目录命名为 `bepinex-788-official` 并放在补丁脚本同目录，即 `sources/bepinex-788-official/BepInEx/core`。使用 PowerShell 7.6 执行：

```powershell
pwsh -NoProfile -File .\sources\patch-788-stage.ps1
```

Output is `sources/bepinex-788-patched/core`; it must not already exist. The script verifies exact original hashes, modifies a staging copy using Mono.Cecil, and never modifies the game installation. No separate .NET SDK is required. The patch is MIT-licensed as stated in the accompanying license.

输出为 `sources/bepinex-788-patched/core`，该目录必须尚不存在。脚本核对原 DLL 哈希，在暂存副本使用 Mono.Cecil 修改，不访问游戏安装目录。无需另装 .NET SDK。

修补后 / Patched hashes:

- LibCpp2IL.dll: `E4A458D6144993336DEE769A66968EC4004B86AED2E22221C1B6B2A38CE7D421`
- Cpp2IL.Core.dll: `2A6C78F3F8987DF17EAB19192AF1F0E19C41D3F0E274BF2F89AC2114426DF502`

上游代码 / Upstream code: Cpp2IL commit `558ddd98642010897d54316b51fbaa7889fda093`，Il2CppInterop `dbda1cb353b0f4253345dc45136d170b9e50a5a0`，BepInEx `5b766a3b7f6c164d4798924a93f3acf4db769d06`。随包源码 ZIP 保留原始代码及构建文件；Cpp2IL 源码包仅排除 TestFiles 中非代码的 Unity 测试二进制。
