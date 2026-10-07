using UnityEngine;

namespace CoalShoot
{
    public enum DamageKind
    {
        Projectile,
        Contact,
        Stomp,
        Dash,
        Hazard,
        Fall
    }

    public struct DamageInfo
    {
        public float Amount;
        public Vector2 Point;
        public Vector2 Direction;
        public Team SourceTeam;
        public DamageKind Kind;
        /// <summary>Супер-шкала стрелявшего игрока: урон по врагам её заполняет.</summary>
        public SuperMeter Source;
    }

    /// <summary>Всё, что получает урон: игроки, враги, босс.</summary>
    public interface IDamageable
    {
        Team Team { get; }
        bool CanBeDamaged { get; }
        /// <returns>true, если урон прошёл</returns>
        bool TakeDamage(in DamageInfo info);
    }
}
