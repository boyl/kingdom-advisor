#Requires -Version 7.0
param(
    [string]$GamePath,
    # 可指定只读的 interop 副本，供补丁前后对照和失败门禁验证。
    [string]$AssemblyPath
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(!$GamePath){
    $steamPath=(Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath
    $GamePath=Join-Path $steamPath 'steamapps/common/Kingdom Two Crowns'
}
if(!$AssemblyPath){$AssemblyPath=Join-Path $GamePath 'BepInEx/interop/Assembly-CSharp.dll'}
$cecilPath=Join-Path $GamePath 'BepInEx/core/Mono.Cecil.dll'
foreach($path in @($cecilPath,$AssemblyPath)){
    if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw "原生覆盖门禁缺少文件：$path；其他 Steam 库请指定 -GamePath。"}
}

# 这是本机 2.4.2 已审核的类型契约清单，不是将全部类型开放的白名单。
# 新类型、已审核类型缺失及继承变化均失败，必须审查语义后更新清单。
# 分类依据包括原生 CanPay/Pay、已加载对象 census 和原生类型声明；
# 不按预制体名字推断行为，也不以“可反复付款”替代普通购买/升级的范围。
$payableRows=@'
PayableShop|Payable|普通库存商品；更换角色与能力的派生类另行排除
PayableSidedShop|PayableShop|普通单侧装备；更换用途须按实际分支拒绝
CitizenHousePayable|Payable|普通招募；遵循原生可招募人数
PayableWorkshopBarrel|Payable|油桶补给；投石车有效且建成超过十秒，未见桶数上限
PayableWorkshop|Payable|投石车生产；已有存活投石车时原生禁止再造
PayableShield|Payable|普通盾牌；仅空缺或破损且城堡未施工时可购买
PayableForge|Payable|仅 Side.Left 且 armorPrefab 存在为装备；右侧炸弹排除
PayableUpgrade|Payable|仅已审核普通建筑及原生后继；隐士改造和特殊任务不泛化
ChangeItemOfPowerShop|PayableShop|排除：更换能力或神器
ChangeRulerShop|PayableShop|排除：更换角色
PayableComponent|Payable|按 owner 语义分类；代理类整体不可开放
Wharf|Payable|仅未下水且仍有可买零件；零件耗尽停止，禁止跨入 LaunchBoat
Merchant|Payable|排除：跨日派遣商人
PayableBush|Payable|排除：季节采集周期
Statue|Payable|排除：神祇激活与解锁
TimedStatue|Statue|排除：续祭和补时不属普通购买
TimeStatue|Payable|排除：挑战机制及补时
BoatSailPosition|Payable|排除：出航阶段
BoatSummoningBell|Payable|排除：人员召集命令
PayableBoat|Payable|非付款入口：本机 CanPay 恒 false 且 Pay 为空
Bomb|Payable|排除：炸弹任务与进攻阶段
PayableBombPurchase|Payable|排除：炸弹购买抽象基类
PayableBombLeft|PayableBombPurchase|排除：炸弹军队准备
PayableBombRight|PayableBombPurchase|排除：炸弹军队准备
PayableBorder|Payable|排除：军队派遣
PayableHorn|Payable|排除：军事命令
PayableShieldWallActivator|Payable|排除：盾墙集结，非盾牌库存
PayableTeleporter|Payable|排除：传送和出口选择
PayablePlayer|Payable|排除：玩家交互
PayableTree|Payable|排除：一次性标记，不自动选择下一棵树
PayableGemChest|Payable|排除：资源存取
PayableGemGuard|Payable|排除：宝石交换及存取
Cabin|Payable|排除：角色解锁
Hermit|Payable|排除：乘员选择
Persephone|Hermit|排除：任务角色
Steed|Payable|排除：坐骑切换
SteedSpawn|Payable|排除：坐骑解锁
UnlockNewRulerStatue|Payable|排除：特定角色解锁
LokiPuzzleStaff|Payable|排除：谜题与神力
HeimdallPuzzleHorn|Payable|排除：谜题阶段
HeimdallPuzzlePillar|Payable|排除：谜题阶段
HelPuzzleItem|Payable|排除：谜题阶段
HelPuzzlePillar|Payable|排除：谜题阶段
ThorPuzzleHammer|Payable|排除：谜题阶段
ThorPuzzlePillar|Payable|排除：谜题阶段
ThorPuzzleStatue|Payable|排除：谜题阶段
Signpost|Payable|排除：地图交互
Snowman|Payable|排除：季节对象
PayableXbox|Payable|待核实且不开放：特殊设施
'@
$ownerRows=@'
FireTower|普通火罐补给；原生库存未满才可付，每次加一罐
CurrencyBagPayable|排除：钱袋解锁
SerpentLevelTrojanHorse|排除：战役任务与推进部队
TrojanHorseSummoningBell|排除：任务人员召集
GreedBuybackTrader|排除：被夺物品赎回选择
GreedGemTrader|排除：资源买卖和交换方向
GreedGolem|排除：特殊敌人状态
HermesShade|排除：任务角色
Oracle|排除：任务提示
PayableBorderBanner|排除：军事命令
PayableCrownStatue|排除：终章任务
PayableFishingRod|排除：钓鱼小游戏的反应时机
PlayerCargo|排除：玩家货物交互
CerberusPuzzlePillar|排除：谜题状态选择
ChariotPuzzlePillar|排除：昼夜与宝石谜题
ArtemisCage|排除：任务
GiantGoldenAppleTree|排除：任务及敌人波次
HermesDock|排除：任务交付及敌人波次
ItemOfPowerReward|排除：任务奖励和能力选择
BunnyStatue|排除：彩蛋任务
EggMound|排除：活动对象
Shipyard|普通舰队船生产：仅 Idle 且数量低于原生上限；进攻由独立事件触发
StagSpawnerHelper|排除：任务生成器
Telescope|待核实且不开放：特殊交互
WorldEatingSerpentWeakPoint|排除：战役首领任务
AssembledBuildProject.AssembledBuildProject|待核实且不开放：须按具体工程语义区分
AssembledBuildProject.PayableAssembledBuildSpawner|待核实且不开放：须按具体工程及最终副作用区分
'@
# 最后两个 owner 使用程序集完整命名空间，避免将 dump 中的短类名当作完整身份。
$expectedPayables=@{}
foreach($row in ($payableRows -split '\r?\n')){
    $parts=$row.Split('|',3)
    if($parts.Count -ne 3 -or $expectedPayables.ContainsKey($parts[0])){throw "已审核 Payable 清单格式错误：$row"}
    $expectedPayables.Add($parts[0],$parts[1])
}
$expectedOwners=@{}
foreach($row in ($ownerRows -split '\r?\n')){
    $parts=$row.Split('|',2)
    if($parts.Count -ne 2 -or $expectedOwners.ContainsKey($parts[0])){throw "已审核 owner 清单格式错误：$row"}
    $expectedOwners.Add($parts[0],$parts[1])
}
if($expectedPayables.Count -ne 49 -or $expectedOwners.Count -ne 27){throw '2.4.2 已审核清单数量变化，须连同审查依据一起更新。'}

Add-Type -Path $cecilPath
# 只解析元数据，不反射加载或执行 Assembly-CSharp，也不调用任何游戏方法。
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
try {
    $types=[System.Collections.Generic.List[Mono.Cecil.TypeDefinition]]::new()
    function Add-MetadataTypes($items){
        foreach($item in $items){$types.Add($item);Add-MetadataTypes $item.NestedTypes}
    }
    Add-MetadataTypes $assembly.MainModule.Types
    $byName=@{}
    foreach($type in $types){$byName.Add($type.FullName,$type)}
    foreach($name in @('Payable','IPayableComponentOwner')){
        if(!$byName.ContainsKey($name)){throw "变体差异：程序集缺少基础契约 $name；不能继续声称本机 2.4.2 覆盖通过。"}
    }
    function Get-MetadataAncestors($type){
        $seen=[System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        while($null -ne $type){
            if(!$seen.Add($type.FullName)){throw "原生元数据继承循环：$($type.FullName)"}
            $type
            if($null -eq $type.BaseType -or !$byName.ContainsKey($type.BaseType.FullName)){break}
            $type=$byName[$type.BaseType.FullName]
        }
    }
    function Get-MethodContract($method){
        $parameters=@(foreach($parameter in $method.Parameters){
            $direction=if($parameter.IsOut){':out'}elseif($parameter.IsIn){':in'}else{''}
            $parameter.ParameterType.FullName+$direction
        })
        $method.ReturnType.FullName+' '+$method.Name+'('+($parameters -join ',')+')'
    }
    $ownerContract=@(
        'Il2CppSystem.Action`1<Player> get_OnPay()',
        'System.Boolean CanPay(Player)',
        'System.Boolean IsLocked(Player,LockIndicator/LockReason&:out)'
    )
    $proxyContract=@($byName['IPayableComponentOwner'].Methods |
        Where-Object {$_.IsPublic -and !$_.IsStatic -and !$_.IsConstructor} |
        ForEach-Object {Get-MethodContract $_})
    $contractDelta=@(Compare-Object ($ownerContract|Sort-Object) ($proxyContract|Sort-Object))
    if($contractDelta.Count){
        $contractDelta|ForEach-Object {Write-Output ("owner 契约变化：{0} {1}" -f $_.SideIndicator,$_.InputObject)}
        throw 'IPayableComponentOwner 的公开实例方法契约变化，须重新复核。'
    }

    $actualPayables=@{}
    $actualOwners=[System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $explicitOwners=[System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($type in $types){
        if(!$type.IsClass -or $type.FullName -eq 'IPayableComponentOwner'){continue}
        $ancestors=@(Get-MetadataAncestors $type)
        if($type.FullName -ne 'Payable' -and $ancestors.FullName -contains 'Payable'){
            $actualPayables.Add($type.FullName,$type.BaseType.FullName)
        }
        $contracts=[System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($ancestor in $ancestors){
            foreach($implemented in $ancestor.Interfaces){
                if($implemented.InterfaceType.FullName -eq 'IPayableComponentOwner'){[void]$explicitOwners.Add($type.FullName)}
            }
            foreach($method in $ancestor.Methods){
                if($method.IsPublic -and !$method.IsStatic -and !$method.HasGenericParameters -and $method.Name -in @('get_OnPay','CanPay','IsLocked')){
                    [void]$contracts.Add((Get-MethodContract $method))
                }
            }
        }
        if(@($ownerContract|Where-Object {!$contracts.Contains($_)}).Count -eq 0){[void]$actualOwners.Add($type.FullName)}
    }
    # Il2CppInterop 生成的 2.4.2 代理会擦除 CLR 接口声明：实际 Interfaces 数量为 0。
    # 因而 owner 清单按代理的三个精确方法签名（含返回值、参数与 out）枚举，包含继承。
    # 这是已与原生 dump 的 27 个实现逐一核对过的结构契约，不把签名相似的新类自动开放。
    $issues=[System.Collections.Generic.List[string]]::new()
    foreach($name in ($actualPayables.Keys|Sort-Object)){
        if(!$expectedPayables.ContainsKey($name)){$issues.Add("待复核新增 Payable 类型：$name（父类 $($actualPayables[$name])）")}
        elseif($actualPayables[$name] -ne $expectedPayables[$name]){$issues.Add("继承契约变化：$name，原 $($expectedPayables[$name])，现 $($actualPayables[$name])")}
    }
    foreach($name in ($expectedPayables.Keys|Sort-Object)){
        if(!$actualPayables.ContainsKey($name)){$issues.Add("变体差异：缺少已审核 Payable 类型或已不再派生自 Payable：$name")}
    }
    foreach($name in ($actualOwners|Sort-Object)){
        if(!$expectedOwners.ContainsKey($name)){$issues.Add("待复核新增 owner 结构契约：$name；不得据此自动开放连续购买")}
    }
    foreach($name in ($expectedOwners.Keys|Sort-Object)){
        if(!$actualOwners.Contains($name)){$issues.Add("变体差异：缺少已审核 owner 类型或其方法契约发生变化：$name")}
    }
    foreach($name in $explicitOwners){
        if(!$actualOwners.Contains($name)){$issues.Add("owner 接口与方法契约不一致：$name")}
    }
    if($issues.Count){
        $issues|ForEach-Object {Write-Output $_}
        throw "原生连续购买覆盖门禁未通过：$($issues.Count) 项差异；必须复核当前游戏变体，不能沿用 2.4.2 结论。"
    }
    $hash=(Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash
    Write-Output "通过：本机 2.4.2 已审核的 $($actualPayables.Count) 个 Payable 派生类型及 $($actualOwners.Count) 个 owner 结构契约，无新增、缺失或已审核继承差异。"
    Write-Output "owner 元数据：CLR 接口声明 $($explicitOwners.Count) 个；实际使用精确方法签名及继承枚举，未执行游戏代码。"
    Write-Output "程序集：$([IO.Path]::GetFullPath($AssemblyPath))；SHA256=$hash"
    Write-Output '验证范围仅为安装副本的类型覆盖契约；不代表所有类型已启用，也不代表全部主题、场景或真实连续付款已经验收。'
} finally {$assembly.Dispose()}
