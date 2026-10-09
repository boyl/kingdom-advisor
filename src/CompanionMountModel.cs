using System;
using System.Collections.Generic;

namespace KingdomAdvisor
{
    public enum CompanionMountKind { Native, Cat, Dog }

    // 与 Unity、输入设备和原生存档隔离的会话规则。
    public sealed class CompanionMountState
    {
        public CompanionMountKind Kind { get; private set; }
        public float ReadyAt { get; private set; }
        public float StartedAt { get; private set; }
        public bool Active { get; private set; }
        private readonly HashSet<int> hits = new HashSet<int>();
        public float Cooldown => .5f;
        public float Duration => Kind == CompanionMountKind.Cat ? .8f : 2;
        public float Remaining(float now) => Math.Max(0, ReadyAt - now);
        public void Defer(float readyAt) { ReadyAt = Math.Max(ReadyAt, readyAt); }
        public void Select(CompanionMountKind kind)
        {
            if (!Enum.IsDefined(typeof(CompanionMountKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            // 切换不能清除冷却并绕过限制。
            Active = false; hits.Clear();
        }
        public bool Begin(float now, bool playing, bool online, bool paused, bool worldAuthority = false)
        {
            if (!playing || online && !worldAuthority || paused || Kind == CompanionMountKind.Native || Active || now < ReadyAt) return false;
            StartedAt = now; ReadyAt = now + Cooldown; Active = true; hits.Clear(); return true;
        }
        public bool Step(float now)
        {
            if (!Active || now < StartedAt + Duration) return false;
            Active = false; return true;
        }
        public bool ClaimHit(int instance) => Active && hits.Add(instance);
        public void Synchronize(CompanionMountKind kind,float now,float cooldown,float age,bool hasCast)
        {
            Kind=kind; ReadyAt=now+cooldown; StartedAt=now-age; Active=hasCast && age<Duration;
        }
        public void Reset() { Kind = CompanionMountKind.Native; ReadyAt = StartedAt = 0; Active = false; hits.Clear(); }
        public static bool InRadius(float enemyX, float enemyY, float x, float y, float radius)
            => (enemyX - x) * (enemyX - x) + (enemyY - y) * (enemyY - y) <= radius * radius;
    }
}
