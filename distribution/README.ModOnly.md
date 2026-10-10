# 王国顾问：仅 Mod 资源包（0.6.7-r1）

这是 0.6.7 去掉操作结果提示后的修订包，游戏内版本仍显示 0.6.7。此包不含前置。Windows x64；已验收游戏 2.4.2 / Unity 6000.0.66f2。其他环境尚未验证。

## 前置

需要 BepInEx 6 Unity IL2CPP Windows x64。推荐配套整合包使用的 **#788 + 5b766a3 / Il2CppInterop 1.5.3 + Cpp2IL 属性解析修复**。这是修改过的兼容前置，不是官方原版 #788。官方 #738、#782、#785、#788 仍可能在首次生成时出现 `AndroidManager / RawPropertyType` 空引用；请勿认为直接下载官方 #788 即可解决。

已经正常游玩的玩家可保留现有前置，仅更新此 Mod。新安装或遇到上述错误，请选择配套的 `KingdomAdvisor-0.6.7-r1-Complete.zip`，按照整合包说明安装。Mod 包不会改变加载器。

## 安装位置与步骤

1. 在 Steam 中右键游戏 → 管理 → 浏览本地文件。安装位置是能直接看到 `KingdomTwoCrowns.exe` 的目录。
2. 正常保存并退出游戏。
3. 解压本包，打开 `payload`，将其中的 **BepInEx 文件夹**合并到游戏根目录，覆盖 `KingdomAdvisor.dll`。不要把压缩包外层目录或 `payload` 目录放进游戏。
4. 启动游戏，F7 打开图鉴，F6 打开设置。原有配置会保留。

也可双击 `INSTALL.cmd`，自动识别 Steam 游戏库并安装，仅替换 Mod DLL。需 PowerShell 7；未安装时入口会给出安装链接。自动定位失败时输入游戏目录。受保护目录可能出现 UAC 请求。仅 Mod 安装成功不代表前置已修复。

```text
解压包/payload/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll
                       ↓ 合并到游戏根目录
Kingdom Two Crowns/
├─ KingdomTwoCrowns.exe
├─ winhttp.dll                         ← 已有前置
├─ doorstop_config.ini                 ← 已有前置
├─ dotnet/                             ← 已有前置
└─ BepInEx/
   ├─ core/                            ← 已有前置
   ├─ config/                          ← 保留配置
   ├─ interop/                         ← 前置自动生成
   └─ plugins/KingdomAdvisor/
      └─ KingdomAdvisor.dll            ← 本包文件
```

## 更新、卸载、回滚与日志

更新：退出游戏后覆盖同名 DLL。卸载本 Mod：退出游戏后移走 `BepInEx/plugins/KingdomAdvisor`，不卸载其他插件和前置。

脚本安装会生成 `游戏目录/.install-backups/KingdomAdvisor-日期-编号`。使用 PowerShell 7 回滚本次安装：

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns' -Rollback 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns\.install-backups\KingdomAdvisor-日期-编号'
```

安装后的文件有变化时脚本会停止回滚，避免覆盖后续更新。请保留备份直到确认新版本正常。

故障反馈附 `BepInEx/LogOutput.log`，以及游戏版本、游戏来源、所用前置包。不要附 API Key、存档或账号信息。尚未单独核验所有其他 Mod 的组合兼容性。
