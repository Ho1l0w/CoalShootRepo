using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Сердца игрока. Любой удар снимает одно сердце, отбрасывает и даёт неуязвимость (персонаж мигает).
    /// На нуле игрок «падает» — PlayerController превращает его в призрака, которого может поднять напарник.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] int baseMaxHp = 3;
        [SerializeField] float invulnerabilityAfterHit = 1.67f;
        [SerializeField] float hitStop = 0.1f;
        [SerializeField] float hitShake = 0.5f;
        [SerializeField] float downShake = 0.6f;
        [SerializeField] GameObject hitFx;
        [SerializeField] GameObject downFx;
        [SerializeField] GameObject reviveFx;

        public int Hp;
        public int MaxHp { get; private set; }
        public bool IsDown { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil || (smokeDash && pc.Motor.IsDashing);
        public Team Team => Team.Player;
        public bool CanBeDamaged => !IsDown && !IsInvulnerable;

        /// <summary>(сердца, максимум)</summary>
        public event Action<int, int> Changed;
        public event Action Hurt;
        public event Action Downed;
        public event Action Revived;

        PlayerController pc;
        float invulnerableUntil;
        bool smokeDash;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            MaxHp = baseMaxHp;
            Hp = MaxHp;
        }

        /// <summary>Применить амулеты: лишнее сердце, неуязвимость во время рывка.</summary>
        public void Configure(int extraHearts, bool dashMakesInvulnerable)
        {
            MaxHp = baseMaxHp + Mathf.Max(0, extraHearts);
            Hp = MaxHp;
            smokeDash = dashMakesInvulnerable;
            Changed?.Invoke(Hp, MaxHp);
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (!CanBeDamaged || info.SourceTeam == Team.Player) return false;

            Hp = Mathf.Max(0, Hp - 1);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;
            if (info.Kind != DamageKind.Fall) pc.Motor.Knockback(info.Point.x > pc.Center.x ? -1 : 1);

            HitStop.Freeze(hitStop);
            CameraShake.Add(hitShake);
            FX.Spawn(hitFx, pc.Center, 1.5f);
            Hurt?.Invoke();
            Changed?.Invoke(Hp, MaxHp);

            if (Hp == 0) GoDown();
            return true;
        }

        void GoDown()
        {
            IsDown = true;
            CameraShake.Add(downShake);
            HitStop.Freeze(0.17f);
            FX.Spawn(downFx, pc.Center, 2f);
            Downed?.Invoke();
        }

        public void Revive(int hp, Vector2 position)
        {
            IsDown = false;
            Hp = Mathf.Clamp(hp, 1, MaxHp);
            invulnerableUntil = Time.time + 2f;
            pc.Motor.Teleport(position);
            FX.Spawn(reviveFx, position, 1.5f);
            Revived?.Invoke();
            Changed?.Invoke(Hp, MaxHp);
        }

        public void HealFull()
        {
            if (IsDown) return;
            Hp = MaxHp;
            Changed?.Invoke(Hp, MaxHp);
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }
    }
}
