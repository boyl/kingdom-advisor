# 王国顾问：前置与 Mod 整合包（0.6.7-r1）

包含 **BepInEx 6.0.0-be.788 + 5b766a3、Il2CppInterop 1.5.3、Cpp2IL 属性解析修复、王国顾问 0.6.7-r1**。游戏内 Mod 版本仍显示 0.6.7。修改过的两个解析器 DLL 已在 `THIRD-PARTY-NOTICES.md` 标明，不能称为官方原版。

已验收：Windows x64、游戏 2.4.2、Unity 6000.0.66f2；空缓存首次生成、重启、Mod 加载、图鉴及操作提示移除。其他游戏版本、系统、联机与所有插件组合尚未完整验证。

### 安装 PowerShell 7（GitHub 一键安装版需要）

Windows 10/11 自带的 Windows PowerShell 5.1 不能运行本安装脚本；Windows Terminal 也不代表已安装 PS7。Nexus 手动包不需要此步骤。

1. **直接下载微软官方 Windows x64 安装包：** [PowerShell-7.6.6-win-x64.msi](https://github.com/PowerShell/PowerShell/releases/download/v7.6.6/PowerShell-7.6.6-win-x64.msi)。这是 2026-10-10 核对的稳定版。
2. 备用入口：[PowerShell 官方发布页](https://github.com/PowerShell/PowerShell/releases/latest) · [微软 Windows 安装指南](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-windows)。在官方 Assets 中选择稳定版 `PowerShell-7.x.x-win-x64.msi`；本 Mod 适用 Windows x64，不要选 ARM64、x86、Preview 或 Source code。
3. 双击下载的 MSI，按安装向导完成安装，保留默认安装位置及“Add PowerShell to Path”选项。安装 PS7 不会替换系统 PS5.1。
4. 安装完成后关闭之前提示缺少 PS7 的窗口，回到已解压的 Mod 文件夹，重新双击 **INSTALL.cmd**。不要直接双击 `install.ps1`，也不要在 ZIP 内运行。
5. 如果仍提示缺少 PS7，确认开始菜单有“PowerShell 7”，或检查 `C:\Program Files\PowerShell\7\pwsh.exe`；本入口会查找 PATH 和默认安装位置。仍有问题请附错误提示。

## 一键安装

1. 正常保存并退出游戏。建议另行备份存档；脚本不访问存档目录。
2. 将整个 ZIP 解压到任意普通文件夹，例如 `下载/KingdomAdvisor-Complete`。不要直接在 ZIP 内运行，也不要先手动把 `payload` 合并进旧前置。
3. 双击 **INSTALL.cmd**。需要 PowerShell 7；缺少时按窗口中的链接安装后重新运行。入口不会使用 Windows PowerShell 5.1。
4. 脚本自动查找 Steam 默认库及其他库。找不到或找到多个目录时，请输入能直接看到 `KingdomTwoCrowns.exe` 的目录。权限不足会请求 UAC；取消则安装失败。
5. 等待显示“安装成功，SHA256 校验通过”。启动游戏，首次启动会联网下载对应 Unity 基础库并生成桥接文件；本机约一至数分钟，用户机器可能更久。请等待，不要把没有立即出现窗口当成崩溃。
6. F7 打开图鉴，F6 打开设置。若启动失败，保留日志并按下方方式回滚。

脚本会先核对全部安装文件哈希，备份旧文件，再替换整套 `BepInEx/core`、`dotnet`，并将旧 `interop`、`cache` 移入备份，让新前置重新生成。保留原配置、其他插件、存档及游戏文件；覆盖王国顾问 DLL 和根目录加载器文件。失败时自动恢复，若恢复不完整会明确报错并给出备份位置。

## 文件放在哪里

```text
解压目录/
├─ INSTALL.cmd                        ← 双击它
├─ install.ps1
├─ manifest.json                      ← 安装文件与 SHA256
├─ README.md / README.en.md
├─ licenses/ / sources/               ← 第三方声明和源码，不装进游戏
└─ payload/                           ← 脚本使用，勿整层放入游戏
   ├─ winhttp.dll
   ├─ doorstop_config.ini
   ├─ .doorstop_version
   ├─ changelog.txt
   ├─ dotnet/
   └─ BepInEx/
      ├─ core/
      └─ plugins/KingdomAdvisor/KingdomAdvisor.dll
```

```text
安装完成后的游戏根目录/
├─ KingdomTwoCrowns.exe
├─ KingdomTwoCrowns_Data/              ← 游戏原有，不改动
├─ winhttp.dll
├─ doorstop_config.ini
├─ .doorstop_version
├─ dotnet/
├─ .install-backups/                   ← 原前置及本次安装记录
└─ BepInEx/
   ├─ core/                            ← #788＋两个解析器补丁
   ├─ config/                          ← 原配置保留；首次启动可新建
   ├─ interop/                         ← 首次启动自动生成，不随包分发
   ├─ unity-libs/                      ← 首次启动联网获取，不随包分发
   ├─ LogOutput.log                    ← 启动日志
   └─ plugins/
      ├─ 其他插件/                     ← 保留，组合兼容性需自行确认
      └─ KingdomAdvisor/KingdomAdvisor.dll
```

## 指定目录、回滚与卸载

高级用户使用 PowerShell 7：

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns'
```

退出游戏后，以安装成功时显示的备份目录回滚：

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns' -Rollback 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns\.install-backups\KingdomAdvisor-日期-编号'
```

回滚前核对当前安装文件；若已被后续版本更改则停止，避免覆盖新版本。回滚恢复之前的核心、运行时、缓存、加载器与 Mod，测试期间生成的目录移入该备份的 `after`，不会删除存档。不要删除正在使用的备份。

只卸载王国顾问：退出游戏后移走 `BepInEx/plugins/KingdomAdvisor`。想撤销整套安装请使用回滚，避免手动删除其他 Mod 依赖的前置。

如报错，请附 `BepInEx/LogOutput.log`，游戏版本与来源，以及整合包名称。`sources/patch-788-stage.ps1` 仅用于复现解析器改动，不是用户安装入口。
