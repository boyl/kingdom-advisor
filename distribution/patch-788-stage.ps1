#Requires -Version 7.0
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$source=Join-Path $root 'bepinex-788-official/BepInEx/core'
$stage=Join-Path $root 'bepinex-788-patched/core'
if(Test-Path -LiteralPath $stage){throw '暂存目录已存在；不得覆盖既有试验'}
foreach($item in @(@('LibCpp2IL.dll','DD3BD5417CAC3FD203B906A90DBAE6719136E61F43C2B4EED55F56242BB10A14'),@('Cpp2IL.Core.dll','A7AE866298472BFA5176C1B5B71ADFA9FE6E2F1DEEF7B8D50F063C0E2C0BA258'))){if((Get-FileHash (Join-Path $source $item[0])).Hash -ne $item[1]){throw '源文件不是已核验官方 #788'}}
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $stage
Add-Type -Path (Join-Path $source 'Mono.Cecil.dll')
$refs=@((Join-Path $source 'Mono.Cecil.dll'))+@(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;using System.Linq;using System.IO;using Mono.Cecil;using Mono.Cecil.Cil;
public static class Patch788 {
 static void Insert(MethodDefinition m,Instruction anchor,params Instruction[] code){var p=m.Body.GetILProcessor();foreach(var i in code)p.InsertBefore(anchor,i);}
 static void Widen(MethodDefinition m){foreach(var i in m.Body.Instructions){var name=i.OpCode.Code.ToString();if(i.Operand is Instruction && name.EndsWith("_S"))i.OpCode=(OpCode)typeof(OpCodes).GetField(name.Substring(0,name.Length-2)).GetValue(null);}}
 static Instruction I(OpCode c)=>Instruction.Create(c);
 static void GuardValue(MethodDefinition m,Instruction call){var next=call.Next;Insert(m,next,I(OpCodes.Dup),Instruction.Create(OpCodes.Brtrue,next),I(OpCodes.Pop),I(OpCodes.Ldnull),I(OpCodes.Ret));}
 public static void Run(string dir){
  var lp=Path.Combine(dir,"LibCpp2IL.dll");var lib=AssemblyDefinition.ReadAssembly(lp);
  var m=lib.MainModule.Types.Single(t=>t.FullName=="LibCpp2IL.Metadata.Il2CppPropertyDefinition").Methods.Single(x=>x.Name=="get_RawPropertyType");
  var setter=m.Body.Instructions.Single(i=>i.Operand is MethodReference r && r.Name=="get_Setter");
  var parameters=m.Body.Instructions.Single(i=>i.Operand is MethodReference r && r.Name=="get_Parameters");
  var element=m.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldelem_Ref);
  GuardValue(m,setter);GuardValue(m,parameters);GuardValue(m,element);
  var next=parameters.Next;
  // 参数数组有值但长度为零时，同样不存在可用属性类型。
  // 原 null 保护的成功跳转抵达原 ldc.i4.0；把长度保护放在该目标处。
  var index=m.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldc_I4_0);
  var lengthStart=I(OpCodes.Dup);
  foreach(var branch in m.Body.Instructions.Where(i=>ReferenceEquals(i.Operand,index)).ToArray())branch.Operand=lengthStart;
  Insert(m,index,lengthStart,I(OpCodes.Ldlen),Instruction.Create(OpCodes.Brtrue,index),I(OpCodes.Pop),I(OpCodes.Ldnull),I(OpCodes.Ret));
  Widen(m);lib.Write(lp+".tmp");lib.Dispose();File.Move(lp+".tmp",lp,true);
  var cp=Path.Combine(dir,"Cpp2IL.Core.dll");var cpp=AssemblyDefinition.ReadAssembly(cp);
  var type=cpp.MainModule.Types.Single(t=>t.Name=="AsmResolverAssemblyPopulator");
  m=type.Methods.Single(x=>x.Name=="CopyPropertiesInType");
  var ctx=cpp.MainModule.Types.Single(t=>t.Name=="PropertyAnalysisContext");
  var def=ctx.Methods.Single(x=>x.Name=="get_Definition");
  var raw=ctx.Methods.Single(x=>x.Name=="get_DefaultPropertyType").Body.Instructions.Single(i=>i.Operand is MethodReference r && r.Name=="get_RawPropertyType").Operand as MethodReference;
  var loop=m.Body.Instructions.First(i=>i.OpCode==OpCodes.Br).Operand as Instruction;
  var anchor=m.Body.Instructions.Single(i=>i.OpCode==OpCodes.Stloc_1).Next;
  Insert(m,anchor,I(OpCodes.Ldloc_1),Instruction.Create(OpCodes.Callvirt,def),Instruction.Create(OpCodes.Brfalse,loop),I(OpCodes.Ldloc_1),Instruction.Create(OpCodes.Callvirt,def),Instruction.Create(OpCodes.Callvirt,raw),Instruction.Create(OpCodes.Brfalse,loop));
  Widen(m);
  m=type.Methods.Single(x=>x.Name=="PopulateCustomAttributes");
  var key=m.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldstr && (string)i.Operand=="AsmResolverProperty");
  var get=key.Next;var attrs=get.Next;var copy=attrs.Next;var end=copy.Next;
  if(!(copy.Operand is MethodReference rr)||rr.Name!="CopyCustomAttributes")throw new Exception("属性复制形状不匹配");
  Insert(m,attrs,I(OpCodes.Dup),Instruction.Create(OpCodes.Brtrue,attrs),I(OpCodes.Pop),I(OpCodes.Pop),Instruction.Create(OpCodes.Br,end));
  Widen(m);cpp.Write(cp+".tmp");cpp.Dispose();File.Move(cp+".tmp",cp,true);
 }
}
'@
[Patch788]::Run($stage)
Get-FileHash (Join-Path $stage 'LibCpp2IL.dll'),(Join-Path $stage 'Cpp2IL.Core.dll')
