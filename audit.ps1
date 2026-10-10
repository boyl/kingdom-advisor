#Requires -Version 7.0
param([string]$GamePath)
$ErrorActionPreference='Stop'
if(!$GamePath){$steamPath=(Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath;$GamePath=Join-Path $steamPath 'steamapps/common/Kingdom Two Crowns'}
Add-Type -Path (Join-Path $GamePath 'BepInEx/core/Mono.Cecil.dll')
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'package/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'))
$calls=[Collections.Generic.HashSet[string]]::new()
$approvedWrites=@(
 'System.Void Wallet::set_TotalCapacity(System.Int32)',
 'System.Void Wallet::set_showCurrencyBag(System.Boolean)',
 'System.Void CurrencyBag::Clear()',
 'System.Void CurrencyBag::DespawnCurrency(BagCurrency)',
 'System.Void CurrencyBag::HideImmediate()',
 'System.Void CurrencyBag::Refresh()',
 'System.Void PositionSync::SendFullPos(System.Boolean)',
 'System.Void Player::set_DebugInfiniteStamina(System.Boolean)',
 'System.Void Wallet::AddCurrency(CurrencyType,System.Int32)',
 'System.Void Wallet::SetCurrency(CurrencyType,System.Int32)',
 'System.Void Player::SendWalletCurrencyCount(CurrencyType)',
 'System.Void World::SetTimeScale(System.Single,System.Boolean)',
 'System.Void World::SendTimescale()',
 'System.Void Mover::Stop()'
)
function Inspect-Type($Type) {
    foreach($method in $Type.Methods){
        if(!$method.HasBody){continue}
        foreach($instruction in $method.Body.Instructions){
            $operand=$instruction.Operand
            if($operand -is [Mono.Cecil.MethodReference] -and $operand.DeclaringType.FullName -eq 'Rewired.ActionElementMap' -and $operand.Name.StartsWith('set_')){
                if($operand.Name -ne 'set_enabled' -or $Type.FullName -ne 'KingdomAdvisor.AdvisorController' -or $method.Name -notin @('ReservePointerAxes','RestorePadMappings')){throw "手柄映射写入越界：$method : $operand"}
            }
            if($operand -is [Mono.Cecil.MethodReference] -and $operand.DeclaringType.FullName -eq 'KingdomAdvisor.GameActions' -and $operand.Name -eq 'ClearResources' -and ($Type.FullName -ne 'KingdomAdvisor.AdvisorController' -or $method.Name -ne 'Activate')){throw "资源清空只能由显式确认键或按钮激活，禁止导航触发：$method"}
            if($operand -is [Mono.Cecil.MethodReference] -and $operand.DeclaringType.FullName -eq 'UnityEngine.GUI' -and $operand.Name -in @('SetNextControlName','GetNameOfFocusedControl','TextField','DoTextField')){throw "本机 IL2CPP 未还原的文本编辑接口：$operand"}
            if($operand -is [Mono.Cecil.MethodReference] -and $operand.DeclaringType.Scope.Name -eq 'Assembly-CSharp'){
                [void]$calls.Add($operand.FullName)
                                if($operand.Name.StartsWith('set_') -or ($operand.Name -notmatch '^get_|^IsLocked$|^ReleaseInput$|^GetCurrency$')){
                    if($Type.FullName -ne 'KingdomAdvisor.GameActions' -or $operand.FullName -notin $approvedWrites){throw "超出动作边界或未审查的游戏调用：$operand"}
                }
            }
            if($operand -is [Mono.Cecil.MethodReference] -and $operand.DeclaringType.FullName -in @('UnityEngine.Rigidbody2D','UnityEngine.Transform','UnityEngine.Time') -and $operand.Name.StartsWith('set_')){
                if($Type.FullName -ne 'KingdomAdvisor.GameActions' -or $operand.FullName -notin @('System.Void UnityEngine.Time::set_timeScale(System.Single)','System.Void UnityEngine.Rigidbody2D::set_position(UnityEngine.Vector2)','System.Void UnityEngine.Rigidbody2D::set_linearVelocity(UnityEngine.Vector2)','System.Void UnityEngine.Transform::set_position(UnityEngine.Vector3)')){throw "超出动作边界的坐标写入：$operand"}
            }
            if($instruction.OpCode.Code -in @([Mono.Cecil.Cil.Code]::Stfld,[Mono.Cecil.Cil.Code]::Stsfld) -and $operand -is [Mono.Cecil.FieldReference] -and $operand.DeclaringType.Scope.Name -eq 'Assembly-CSharp'){throw "直接写入游戏字段：$operand"}
        }
    }
    foreach($nested in $Type.NestedTypes){Inspect-Type $nested}
}
foreach($type in $assembly.MainModule.Types){Inspect-Type $type}
"游戏 API 二进制审计通过：$($calls.Count) 个唯一调用，属性读取与已审查动作；玩法写入仅限 GameActions。"
