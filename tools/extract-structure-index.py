# 离线审计工具，依赖 UnityPy==1.20.26；不随 Mod 运行。
# 参数：游戏 Data 目录、输出目录（可选）。只导出对象名称和脚本类型，不导出游戏素材。
from pathlib import Path
import json,sys
sys.path.insert(0,str(Path(__file__).parent/'asset-deps'))
import UnityPy
root=Path(sys.argv[1]);out=Path(sys.argv[2]) if len(sys.argv)>2 else Path(__file__).parent
out.mkdir(parents=True,exist_ok=True)
records=[];errors=[];counts={}
for path in [*sorted(root.glob('*.assets')),*sorted(root.glob('level[0-9]*'))]:
 env=UnityPy.load(str(path))
 for obj in env.objects:
  counts[obj.type.name]=counts.get(obj.type.name,0)+1
  if obj.type.name!='GameObject':continue
  go=obj.read();components=[]
  for pair in go.m_Component:
   ref=pair.component if hasattr(pair,'component') else pair[1]
   if not ref:continue
   try:
    comp=ref.deref()
    if comp.type.name!='MonoBehaviour':continue
    # 只读取MonoBehaviour公共头的游戏对象/脚本引用，不解释自定义序列化负载。
    data=comp.read(check_read=False)
    if data.m_Script.path_id==0:continue
    script=data.m_Script.read()
    components.append(script.m_ClassName)
   except Exception as ex:
    if len(errors)<5:errors.append(type(ex).__name__+': '+str(ex)[:120])
  if components:records.append({'file':path.name,'id':obj.path_id,'name':go.m_Name,'components':components})
 print(path.name,'objects',len(env.objects),'gameobjects-with-scripts',len(records),flush=True)
(out/'asset-gameobjects.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
(out/'asset-audit-summary.json').write_text(json.dumps({'objects':counts,'errors':errors,'records':len(records)},indent=2),encoding='utf8')
print('Errors',errors)
