using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEngine;

namespace KingdomAdvisor
{
    // 原生游戏边界；只在单机本地角色上挂接，可恢复的视觉与会话技能。
    internal sealed class CompanionMounts
    {
        private sealed class Ride
        {
            public Player Player;
            public Steed Steed;
            public readonly CompanionMountState State = new CompanionMountState();
            public SpriteRenderer Visual;
            public SpriteRenderer Source;
            public readonly Dictionary<int, System.Tuple<SpriteRenderer,bool>> Hidden = new Dictionary<int,System.Tuple<SpriteRenderer,bool>>();
            public Transform Anchor;
            public Vector3 AnchorPosition;
            public float Walk, Run, Height;
            public int Direction = 1;
            public float LastX;
            public Vector3 Origin, Target;
            public bool Exploded;
            public bool Driving;
            public readonly CompanionInput Input=new CompanionInput();
            public readonly List<System.Tuple<SteedAbility,bool>> CarrierAbilities=new List<System.Tuple<SteedAbility,bool>>();
            public int Serial;
            public readonly List<GameObject> Effects = new List<GameObject>();
        }
        private readonly Dictionary<int,Ride> rides = new Dictionary<int,Ride>();
        private readonly Dictionary<int,float> cooldowns = new Dictionary<int,float>();
        private readonly Dictionary<int,int> serials = new Dictionary<int,int>();
        private readonly Dictionary<CompanionMountKind,Sprite[]> frames = new Dictionary<CompanionMountKind,Sprite[]>();
        private Sprite[] catEffects;
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private readonly GameActions actions;
        private readonly CompanionMountNetwork network;
        private int island = -1;
        private bool wasOnline;
        private float nextSweep;
        private Enemy[] enemies = Array.Empty<Enemy>();
        public int Casts, Hits;
        public float CaptureAt=-1;
        public CompanionMounts(GameActions actions) { this.actions = actions; network=new CompanionMountNetwork(this,actions); }
        public void VerifyResources()
        {
            Load(CompanionMountKind.Cat);Load(CompanionMountKind.Dog);catEffects=LoadAtlas("cat-vfx");
            var probe=new GameObject("KingdomAdvisor.SpriteBindingProbe").AddComponent<SpriteRenderer>();
            try{foreach(var atlas in frames.Values)foreach(var sprite in atlas)SetSprite(probe,sprite);}
            finally{UnityEngine.Object.Destroy(probe.gameObject);}
            Plugin.Instance.Log.LogInfo("Companion native sprite binding verified: all 16 frames retained by renderer");
            Plugin.Instance.Log.LogInfo("Companion mount atlases loaded: cat=8 dog=8, transparent point-filtered sprites");
        }
        public string Evidence=>"mounts="+rides.Count+" casts="+Casts+" hits="+Hits+" peerMod="+network.PeerMod+" hostAvailable="+network.HostAvailable;
        public int Selection(int owner) => rides.TryGetValue(owner,out var r) ? (int)r.State.Kind : 0;
        private Player Find(int id)
        {
            var kingdom = Managers._Inst?.kingdom;
            if (!kingdom) return null;
            if (kingdom.playerOne && kingdom.playerOne.playerId == id) return kingdom.playerOne;
            return kingdom.playerTwo && kingdom.playerTwo.playerId == id ? kingdom.playerTwo : null;
        }
        public bool Select(Snapshot snapshot,int owner,int selection,bool replica=false)
        {
            Plugin.Instance.Log.LogInfo("Companion mount selection requested player="+owner+" selection="+selection+" playing="+snapshot.Playing+" online="+snapshot.Online+" replica="+replica);
            if (selection < 0 || selection > 2) throw new ArgumentOutOfRangeException(nameof(selection));
            if(!replica && network.RequestSelection(snapshot,owner,selection))return false;
            if (selection == 0) { Remove(owner);  return true; }
            if (!snapshot.Playing) {  return false; }
            var player = Find(owner);
            if (!player || !replica && !player.hasLocalAuthority || !player.steed || player.isOnBoat || player.IsPetrified) { Plugin.Instance.Log.LogWarning("Companion mount rejected: "+PlayerEvidence(owner));  return false; }
            var source=SourceRenderer(player);
            if (!source || !player.steed.riderAnchor) { Plugin.Instance.Log.LogWarning("Companion mount visual interface unavailable: "+PlayerEvidence(owner));  return false; }
            var kind = (CompanionMountKind)selection;
            try { Load(kind); }
            catch (Exception ex) { Plugin.Instance.Log.LogError("Mount resource failed: "+ex);  return false; }
            float ready = cooldowns.TryGetValue(owner,out var deadline) ? deadline : 0;
            Remove(owner);
            var steed = player.steed;
            var visual = new GameObject("KingdomAdvisor.CompanionMount").AddComponent<SpriteRenderer>();
            visual.gameObject.layer=source.gameObject.layer;
            SetSprite(visual,frames[kind][4]);
            visual.color=Color.white;
            visual.sortingLayerID = source.sortingLayerID;
            visual.sortingOrder = source.sortingOrder;
            var ride = new Ride { Player=player, Steed=steed, Visual=visual, Source=source, Anchor=steed.riderAnchor,
                AnchorPosition=steed.riderAnchor.localPosition, Walk=steed.walkSpeed, Run=steed.runSpeed,
                Height=Math.Max(.5f,source.bounds.size.y), LastX=player.transform.position.x };
            ride.State.Select(kind);
            // 冷却保存在规则状态中，切换只取消当前技能，不缩短已开始的冷却。
            ride.State.Defer(ready);
            rides.Add(owner,ride);
            SuspendCarrierAbilities(ride);
            Plugin.Instance.Log.LogInfo("Companion mount selected player="+owner+" kind="+kind+" carrier="+steed.name+" renderer="+source.name+" anchor="+ride.Anchor.name+" height="+ride.Height);
            
            return true;
        }
        private static void SuspendCarrierAbilities(Ride ride)
        {
            if(ride.Steed.steedAbilities==null)return;
            foreach(var ability in ride.Steed.steedAbilities)
            {
                if(!ability)continue;
                ride.CarrierAbilities.Add(Tuple.Create(ability,ability.enabled));
                if(ability.IsAbilityInProgress)ability.Deactivate();
                ability.enabled=false;
                Plugin.Instance.Log.LogInfo("Carrier ability suspended player="+ride.Player.playerId+" type="+ability.GetType().Name);
            }
        }
        private static SpriteRenderer SourceRenderer(Player p)
        {
            // 当前版本的骑乘视觉由 Steed 持有；Player 的旧缓存可能为空。
            if(p.steed && p.steed._spriteFX && p.steed._spriteFX.Renderer)return p.steed._spriteFX.Renderer;
            return p._steedRenderer;
        }
        public string PlayerEvidence(int owner)
        {
            var p=Find(owner);
            if(!p)return "player="+owner+" missing";
            string visual=rides.TryGetValue(owner,out var r)?" visualVisible="+r.Visual.isVisible+" visualPosition="+r.Visual.transform.position+" sourcePosition="+r.Source.transform.position+" layer="+r.Visual.gameObject.layer+" alpha="+r.Visual.color.a+" sprite="+(r.Visual.sprite?r.Visual.sprite.name:"none"):"";
            return "player="+owner+" local="+p.hasLocalAuthority+" boat="+p.isOnBoat+" petrified="+p.IsPetrified+" steed="+(p.steed?p.steed.name:"none")+" renderer="+(p._steedRenderer?p._steedRenderer.name:"none")+" riderRenderer="+(p._riderRenderer?p._riderRenderer.name:"none")+" anchor="+(p.steed&&p.steed.riderAnchor?p.steed.riderAnchor.name:"none")+" selected="+Selection(owner)+visual;
        }
        private void Load(CompanionMountKind kind)
        {
            if (frames.ContainsKey(kind)) return;
            frames.Add(kind,LoadAtlas(kind == CompanionMountKind.Cat ? "cat" : "dog"));
        }
        private Sprite[] LoadAtlas(string name)
        {
            using var stream = typeof(CompanionMounts).Assembly.GetManifestResourceStream("KingdomAdvisor.Mounts."+name+".png");
            if (stream == null) throw new FileNotFoundException("Missing embedded mount atlas: "+name);
            using var memory = new MemoryStream(); stream.CopyTo(memory);
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.hideFlags=HideFlags.DontUnloadUnusedAsset;
            if (!ImageConversion.LoadImage(texture,memory.ToArray(),false)) { UnityEngine.Object.Destroy(texture); throw new InvalidDataException("Invalid atlas: "+name); }
            if (texture.width < 256 || texture.height < 128 || Math.Abs((float)texture.width/texture.height-2)> .05f) { UnityEngine.Object.Destroy(texture); throw new InvalidDataException("Atlas must use a wide 4 x 2 layout"); }
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            int w=texture.width/4,h=texture.height/2;
            var atlas = new Sprite[8];
            // 每格共享底部落脚点，按实际透明边界校正生成图的轻微对齐偏差。
            var pixels = texture.GetPixels32();
            for (int i=0;i<8;i++)
            {
                int cx=i%4*w,cy=(1-i/4)*h,minX=w,minY=h,maxX=-1,maxY=-1;
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[(cy+y)*texture.width+cx+x].a>32){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
                if(maxX<minX)throw new InvalidDataException("Empty atlas cell "+i);
                // 行走帧用一致像素比例；冲锋姿态降低身体而不放大。
                atlas[i]=CreateSprite(texture,new Rect(cx+minX,cy+minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0),h*.82f);
                atlas[i].name="KingdomAdvisor."+name+"."+i;
            }
            textures.Add(texture); return atlas;
        }
        private static Sprite CreateSprite(Texture2D texture,Rect rect,Vector2 pivot,float pixelsPerUnit)
        {
            // IL2CPP 中简化的托管 Create 重载可能被裁剪，直接调用原生绑定。
            var sprite=Sprite.CreateSprite(texture,rect,pivot,pixelsPerUnit,0,SpriteMeshType.FullRect,Vector4.zero,false,null);
            if(!sprite)throw new InvalidOperationException("Native sprite creation returned null: "+rect);
            if(!sprite.texture || sprite.vertices.Length<4)throw new InvalidOperationException("Native sprite has no texture or geometry");
            sprite.hideFlags=HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
        private static void SetSprite(SpriteRenderer renderer,Sprite sprite)
        {
            if(!sprite)throw new InvalidOperationException("Cannot bind an empty mount sprite");
            renderer.sprite=sprite;
            if(!renderer.sprite || renderer.sprite.GetInstanceID()!=sprite.GetInstanceID())
                throw new InvalidOperationException("Renderer did not retain mount sprite");
        }
        public void Cast(Snapshot snapshot,int owner,bool remoteRequest=false)
        {
            if(!remoteRequest && network.RequestCast(snapshot,owner))return;
            if (!rides.TryGetValue(owner,out var ride)) {  return; }
            if (!Valid(ride) || ride.Player.isOnBoat || ride.Player.IsPetrified || !ride.Player.hasCrown) {  return; }
            if (!ride.State.Begin(Time.time,snapshot.Playing,snapshot.Online||NetworkBigBoss.IsOnline,Time.timeScale<=0,NetworkBigBoss.HasWorldAuth))
            {  return; }
            ride.Exploded=false; ride.Origin=Ground(ride.Player.transform.position);
            ride.Target=ride.Origin+Vector3.right*ride.Direction*8;
            ride.Target.x=Math.Clamp(ride.Target.x,snapshot.Left+2,snapshot.Right-2);
            if (ride.State.Kind==CompanionMountKind.Dog)
            { ride.Steed.walkSpeed=ride.Walk*2.5f; ride.Steed.runSpeed=ride.Run*2.5f; }
            Casts++;
            if(ride.State.Kind==CompanionMountKind.Cat && Plugin.Instance.Settings.CompanionVfxCapture.Value){CaptureAt=Time.time+.75f;Plugin.Instance.Settings.CompanionVfxCapture.Value=false;}
            ride.Serial=serials.TryGetValue(owner,out int previous)?checked(previous+1):1;serials[owner]=ride.Serial;
            cooldowns[owner]=ride.State.ReadyAt;
            Plugin.Instance.Log.LogInfo("Companion skill cast player="+owner+" kind="+ride.State.Kind+" time="+Time.time);
        }
        private static bool Valid(Ride r) => r.Player && r.Steed && r.Source && r.Visual && r.Anchor && r.Player.steed && r.Player.steed.GetInstanceID()==r.Steed.GetInstanceID();
        private static Vector3 Ground(Vector3 p) { var ground=World.GroundCollider; if(ground)p.y=ground.bounds.max.y; return p; }
        public void Update(Snapshot snapshot,AdvisorController controller)
        {
            if (!snapshot.Playing || island!=-1 && snapshot.Island!=island) { Restore();network.Reset(); island=snapshot.Island; return; }
            island=snapshot.Island;
            if(wasOnline!=snapshot.Online){Restore();network.Reset();wasOnline=snapshot.Online;}
            network.Poll(snapshot);
            if(Rewired.ReInput.isReady)foreach(var ride in rides.Values.ToArray())if(ride.Player && ride.Player.hasLocalAuthority)
            {
                var input=controller.NativeInputFor(ride.Player.playerId);
                if(input!=null){float axis=input.GetAxis(RewiredAxis.Horizontal);NativeInput(ride.Player,Math.Abs(axis)<.2f?0:Math.Sign(axis),input.GetButton(RewiredAxis.Gallop),input.GetButtonDoublePressDown(RewiredAxis.Gallop));}
            }
            foreach (var item in rides.ToArray()) if (!Valid(item.Value) || !item.Value.Player.gameObject.activeInHierarchy || item.Value.Player.isOnBoat || item.Value.Player.IsPetrified) Remove(item.Key);
            if (Time.time>=nextSweep && rides.Values.Any(r=>r.State.Active)) { enemies=UnityEngine.Object.FindObjectsOfType(Il2CppInterop.Runtime.Il2CppType.Of<Enemy>()).Select(e=>e.TryCast<Enemy>()).Where(e=>e).ToArray(); nextSweep=Time.time+.1f; }
            foreach (var ride in rides.Values)
            {
                if(ride.State.Step(Time.time))StopDrive(ride);
                if(ride.State.Active)
                {
                    float elapsed=Time.time-ride.State.StartedAt;
                    bool authority=!snapshot.Online || NetworkBigBoss.HasWorldAuth;
                    if(authority && ride.State.Kind==CompanionMountKind.Cat && elapsed>=.65f && !ride.Exploded)
                    { Hit(ride,ride.Target,7,12); ride.Exploded=true; }
                    if(ride.State.Kind==CompanionMountKind.Dog){if(ride.Player.hasLocalAuthority){ride.Player.mover.SetSpeed(ride.Run*2.5f,ride.Direction);ride.Driving=true;}if(authority)Hit(ride,Ground(ride.Player.transform.position),ride.Height*1.2f,8);}
                }
                var info=snapshot.Players.FirstOrDefault(p=>p.Id==ride.Player.playerId);
                if(info!=null)
                { info.Mount=ride.State.Kind==CompanionMountKind.Cat ? "呆猫" : "呆狗"; info.Ability=ride.State.Kind==CompanionMountKind.Cat ? "喵式大爆桶" : "疾风破阵"; }
            }
        }
        public void NativeInput(Player player,int direction,bool pressed,bool doubleTap)
        {
            if(!player || !player.hasLocalAuthority || !rides.TryGetValue(player.playerId,out var ride))return;
            var active=AdvisorBehaviour.Active;
            bool allowed=active!=null && !active.Controller.Blocks(player.playerId) && !active.Controller.ChordHeld(player.playerId);
            if(!ride.Input.Step(direction,pressed,doubleTap,allowed))return;
            if(direction!=0)ride.Direction=direction>0?1:-1;
            Cast(active.Controller.CurrentSnapshot,player.playerId);
        }
        private static void StopDrive(Ride ride)
        {
            if(ride.Steed){ride.Steed.walkSpeed=ride.Walk;ride.Steed.runSpeed=ride.Run;}
            if(ride.Driving && ride.Player && ride.Player.hasLocalAuthority && ride.Player.mover)
            {
                // SetSpeed 写的是目标速度；恢复走跑参数不会清除这个目标。
                ride.Player.mover.SetSpeed(0,ride.Direction);
                Plugin.Instance.Log.LogInfo("Companion dash ended player="+ride.Player.playerId+" goalSpeed="+ride.Player.mover.goalSpeed);
            }
            ride.Driving=false;
        }
        private void Hit(Ride ride,Vector3 origin,float radius,int damage)
        {
            if(NetworkBigBoss.IsOnline && !NetworkBigBoss.HasWorldAuth)throw new InvalidOperationException("Client must never apply companion damage");
            foreach(var enemy in enemies)
            {
                if(!enemy || !enemy.gameObject.activeInHierarchy || !Enemy.IsActiveThreat(enemy) || !enemy.damageable || enemy.damageable.isDead)continue;
                var point=enemy.transform.position;
                if(!CompanionMountState.InRadius(point.x,point.y,origin.x,origin.y,radius))continue;
                if(!ride.State.ClaimHit(enemy.GetInstanceID()))continue;
                enemy.damageable.ReceiveDamage(damage,ride.Player.gameObject,DamageSource.PlayerSteed);
                if(NetworkBigBoss.IsOnline && enemy.damageable && !enemy.damageable.isDead)enemy.damageable.SendHP();
                Hits++;
            }
        }
        private void Hide(Ride ride,SpriteRenderer renderer)
        {
            if(!renderer || renderer==ride.Visual)return;
            int id=renderer.GetInstanceID();
            if(!ride.Hidden.ContainsKey(id))ride.Hidden[id]=Tuple.Create(renderer,renderer.enabled);
            renderer.enabled=false;
        }
        public void LateUpdate()
        {
            foreach(var ride in rides.Values)
            {
                if(!Valid(ride))continue;
                var p=ride.Player;float delta=p.transform.position.x-ride.LastX;
                if(Math.Abs(delta)>.001f)ride.Direction=delta>0?1:-1;
                else if(p.mover && p.mover.GetDirection()!=0)ride.Direction=p.mover.GetDirection()>0?1:-1;
                ride.LastX=p.transform.position.x;
                bool moving=p.isMoving;
                int frame=moving ? (int)(Time.time*(p.isRunning?12:7))%4 : 4+(int)(Time.time*2)%2;
                float elapsed=Time.time-ride.State.StartedAt;
                if(ride.State.Active)frame=ride.State.Kind==CompanionMountKind.Cat ? (elapsed<.3f?6:7) : 6+(int)(Time.time*12)%2;
                SetSprite(ride.Visual,frames[ride.State.Kind][frame]);
                float height=ride.Height*(ride.State.Kind==CompanionMountKind.Cat?1.25f:1.1f);
                ride.Visual.transform.localScale=Vector3.one*height;
                ride.Visual.flipX=ride.Direction<0;
                // 使用实际可见精灵的地面与深度；全岛碰撞体的包围盒并非脚底。
                var bounds=ride.Source.bounds;
                var pos=new Vector3(bounds.center.x,bounds.min.y,ride.Source.transform.position.z);
                ride.Visual.transform.position=pos;
                ride.Visual.color=Color.white;
                // 相机尚未看到替代精灵时保留原生，避免换乘变成空白角色。
                if(!ride.Visual.sprite || !ride.Visual.isVisible){foreach(var item in ride.Hidden.Values)if(item.Item1)item.Item1.enabled=item.Item2;continue;}
                Hide(ride,ride.Source);
                if(p._additionalSteedSpriteRenderers!=null)foreach(var extra in p._additionalSteedSpriteRenderers)Hide(ride,extra);
                if(ride.Steed.additionalSpriteRenderers!=null)foreach(var extra in ride.Steed.additionalSpriteRenderers)if(extra)Hide(ride,extra.Renderer);
                if(ride.Steed.reins)foreach(var extra in ride.Steed.reins.GetComponentsInChildren<SpriteRenderer>())Hide(ride,extra);
                // 骑手仍沿用原生锚点，待验证角色层级后再校正骑姿。
                DrawEffects(ride,pos,height,elapsed);
            }
        }
        private void DrawEffects(Ride ride,Vector3 position,float height,float elapsed)
        {
            bool visible=ride.State.Active || ride.Exploded && elapsed<1.3f;
            if(!visible) { foreach(var effect in ride.Effects)if(effect)effect.SetActive(false); return; }
            // 有界粒子对象复用，最多16个；不逐帧生成 Unity 对象。
            while(ride.Effects.Count<16)
            {
                var effect=new GameObject("KingdomAdvisor.MountEffect");var renderer=effect.AddComponent<SpriteRenderer>();
                SetSprite(renderer,EffectSprite());renderer.sortingLayerID=ride.Visual.sortingLayerID;renderer.sortingOrder=ride.Visual.sortingOrder+3;
                ride.Effects.Add(effect);
            }
            for(int i=0;i<ride.Effects.Count;i++)
            {
                var effect=ride.Effects[i];var renderer=effect.GetComponent<SpriteRenderer>();effect.SetActive(true);
                if(ride.State.Kind==CompanionMountKind.Cat)
                {
                    effect.SetActive(i==0);
                    if(i!=0)continue;
                    renderer.color=Color.white;
                    if(!ride.Exploded)
                    {
                        SetSprite(renderer,catEffects[(int)(elapsed*12)%2]);
                        float t=Math.Clamp(elapsed/.65f,0,1);
                        var point=Vector3.Lerp(ride.Origin,ride.Target,t);
                        point.y+=height*.7f+(float)Math.Sin(t*Math.PI)*height*1.5f;
                        effect.transform.position=point;
                        effect.transform.localScale=Vector3.one*height*.65f;
                    }
                    else
                    {
                        float t=elapsed-.65f;
                        int frame=t<.06f?2:t<.14f?3:t<.23f?4:t<.36f?5:t<.5f?6:7;
                        SetSprite(renderer,catEffects[frame]);
                        effect.transform.position=ride.Target;
                        float scale=frame<5?3.5f:4.5f+t;
                        effect.transform.localScale=Vector3.one*scale;
                        renderer.color=new Color(1,1,1,frame<5?1:Math.Clamp(1-(t-.23f)/.42f,0,1));
                    }
                }
                else
                {
                    float t=(Time.time*3+i*.061f)%1;
                    effect.transform.position=position+new Vector3(-ride.Direction*height*(.4f+t*1.8f),height*(.15f+(i%4)*.16f),-.01f);
                    effect.transform.localScale=new Vector3(height*(.3f+t*.6f),height*.035f,1);
                    renderer.color=new Color(.64f,.91f,1,1-t);
                }
            }
        }
        private Sprite effectSprite;
        private Sprite EffectSprite()
        {
            if(effectSprite)return effectSprite;
            var texture=new Texture2D(1,1,TextureFormat.RGBA32,false);texture.hideFlags=HideFlags.DontUnloadUnusedAsset;texture.SetPixel(0,0,Color.white);texture.Apply();texture.filterMode=FilterMode.Point;textures.Add(texture);
            return effectSprite=CreateSprite(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        }
        private void Remove(int owner)
        {
            if(!rides.TryGetValue(owner,out var ride))return;
            StopDrive(ride);
            foreach(var item in ride.CarrierAbilities)if(item.Item1)item.Item1.enabled=item.Item2;
            foreach(var item in ride.Hidden.Values)if(item.Item1)item.Item1.enabled=item.Item2;
            if(ride.Anchor)ride.Anchor.localPosition=ride.AnchorPosition;
            if(ride.Steed){ride.Steed.walkSpeed=ride.Walk;ride.Steed.runSpeed=ride.Run;}
            if(ride.Visual)UnityEngine.Object.Destroy(ride.Visual.gameObject);
            foreach(var effect in ride.Effects)if(effect)UnityEngine.Object.Destroy(effect);
            rides.Remove(owner);
        }
        public void Restore(){foreach(int id in rides.Keys.ToArray())Remove(id);enemies=Array.Empty<Enemy>();cooldowns.Clear();serials.Clear();}
        public void RemoveRemote(){foreach(var pair in rides.ToArray())if(!pair.Value.Player || !pair.Value.Player.hasLocalAuthority)Remove(pair.Key);}
        public string NetworkSlot(int owner)
        {
            if(!rides.TryGetValue(owner,out var r))return "0,0,0,600";
            return ((int)r.State.Kind)+","+r.Serial+","+r.State.Remaining(Time.time).ToString("F2",CultureInfo.InvariantCulture)+","+Math.Min(600,Time.time-r.State.StartedAt).ToString("F2",CultureInfo.InvariantCulture);
        }
        public void ApplyNetworkSlot(Snapshot s,int owner,string value)
        {
            if(NetworkBigBoss.HasWorldAuth)return;
            if(!CompanionMountWire.TrySlot(value,out var slot))return;
            int kind=slot.Kind,serial=slot.Serial;float cooldown=slot.Cooldown,age=slot.Age;
            if(kind==0){Remove(owner);return;}
            if(Selection(owner)!=kind && !Select(s,owner,kind,true))return;
            if(!rides.TryGetValue(owner,out var r))return;
            if(serial!=r.Serial){r.Origin=Ground(r.Player.transform.position);r.Target=r.Origin+Vector3.right*r.Direction*8;}
            r.Serial=serial;
            r.State.Synchronize((CompanionMountKind)kind,Time.time,cooldown,age,serial>0);
            if(!r.State.Active)StopDrive(r);
            r.Exploded=kind==1 && serial>0 && age>=.65f;
            if(r.Player.hasLocalAuthority){r.Steed.walkSpeed=r.Walk*(kind==2&&r.State.Active?2.5f:1);r.Steed.runSpeed=r.Run*(kind==2&&r.State.Active?2.5f:1);}
        }
        public void Dispose()
        {
            Restore();foreach(var atlas in frames.Values)foreach(var sprite in atlas)if(sprite)UnityEngine.Object.Destroy(sprite);
            if(catEffects!=null)foreach(var sprite in catEffects)if(sprite)UnityEngine.Object.Destroy(sprite);if(effectSprite)UnityEngine.Object.Destroy(effectSprite);foreach(var texture in textures)if(texture)UnityEngine.Object.Destroy(texture);frames.Clear();textures.Clear();
        }
    }
}


