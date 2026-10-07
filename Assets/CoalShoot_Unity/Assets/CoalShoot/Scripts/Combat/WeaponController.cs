using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Два слота оружия, смена, стрельба в 8 направлениях, зарядный выстрел, супер-выстрел за карту.
    /// Оружие берётся из ProgressionService (что надето в лавке) или из defaultLoadout.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class WeaponController : MonoBehaviour
    {
        [Tooltip("Если на сцене нет ProgressionService")]
        [SerializeField] WeaponData[] defaultLoadout = new WeaponData[2];
        [Tooltip("Точка, вокруг которой вращается дуло. Не делайте её дочерней к отражаемому спрайту")]
        [SerializeField] Transform aimPivot;
        [SerializeField] float muzzleDistance = 0.8f;
        [Tooltip("Необязательно: спрайт оружия, поворачивается по прицелу")]
        [SerializeField] Transform gunVisual;
        [SerializeField] GameObject muzzleFlashFx;
        [SerializeField] GameObject exFx;
        [SerializeField] GameObject chargeReadyFx;
        [Tooltip("Персонаж замирает на время супер-выстрела")]
        [SerializeField] float exFreeze = 0.23f;
        [SerializeField] float exRecoil = 9f;

        public int ActiveSlot { get; private set; }
        public WeaponData Active => slots[ActiveSlot] != null ? slots[ActiveSlot] : slots[1 - ActiveSlot];
        public float Charge01 => Active != null && Active.mode == FireMode.Charge ? Mathf.Clamp01(charge / Active.chargeTime) : 0f;
        public Vector2 MuzzlePosition => PivotPosition + pc.Aim.Direction * muzzleDistance;

        public event Action<WeaponData> WeaponChanged;
        public event Action Fired;
        public event Action ExFired;
        public event Action ChargeReady;

        readonly WeaponData[] slots = new WeaponData[2];
        PlayerController pc;
        float cooldown;
        float charge;
        bool chargeReadyRaised;

        Vector2 PivotPosition => aimPivot != null ? (Vector2)aimPivot.position : pc.Center;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            slots[0] = defaultLoadout.Length > 0 ? defaultLoadout[0] : null;
            slots[1] = defaultLoadout.Length > 1 ? defaultLoadout[1] : null;
        }

        public void SetLoadout(WeaponData first, WeaponData second)
        {
            slots[0] = first != null ? first : (defaultLoadout.Length > 0 ? defaultLoadout[0] : null);
            slots[1] = second != null ? second : (defaultLoadout.Length > 1 ? defaultLoadout[1] : null);
            ActiveSlot = 0;
            charge = 0f;
            WeaponChanged?.Invoke(Active);
        }

        public void Tick(in PlayerInputFrame input, float dt)
        {
            cooldown -= dt;

            if (input.SwapPressed && slots[0] != null && slots[1] != null)
            {
                ActiveSlot = 1 - ActiveSlot;
                cooldown = Mathf.Max(cooldown, 0.1f);
                charge = 0f;
                chargeReadyRaised = false;
                WeaponChanged?.Invoke(Active);
            }

            WeaponData w = Active;
            if (w == null || pc.Motor.IsDashing || pc.Motor.IsFrozen) return;

            if (input.ExPressed && pc.Super.TrySpendCard())
            {
                FireEx(w);
                return;
            }

            if (w.mode == FireMode.Charge)
            {
                if (input.ShootHeld && (cooldown <= 0f || charge > 0f))
                {
                    charge += dt;
                    if (charge >= w.chargeTime && !chargeReadyRaised)
                    {
                        chargeReadyRaised = true;
                        FX.Spawn(chargeReadyFx, MuzzlePosition, 0.5f);
                        CameraShake.Add(0.05f);
                        ChargeReady?.Invoke();
                    }
                }
                else if (!input.ShootHeld && charge > 0f)
                {
                    FireCharge(w, charge >= w.chargeTime);
                    charge = 0f;
                    chargeReadyRaised = false;
                    cooldown = w.fireInterval;
                }
                return;
            }

            if (input.ShootHeld && cooldown <= 0f)
            {
                Fire(w);
                cooldown = w.fireInterval;
            }
        }

        void LateUpdate()
        {
            if (gunVisual == null) return;
            gunVisual.position = PivotPosition + pc.Aim.Direction * (muzzleDistance * 0.5f);
            gunVisual.rotation = Quaternion.Euler(0f, 0f, pc.Aim.AngleDegrees);
            Vector3 s = gunVisual.localScale;
            s.y = Mathf.Abs(s.y) * (pc.Aim.Direction.x < 0f ? -1f : 1f);
            gunVisual.localScale = s;
        }

        Projectile.Spec BaseSpec(WeaponData w, float damage, float lifetime)
        {
            return new Projectile.Spec
            {
                team = Team.Player,
                damage = damage,
                lifetime = lifetime,
                owner = pc.Super,
                kind = DamageKind.Projectile
            };
        }

        static void ShootAt(Projectile prefab, Vector2 pos, float angleDeg, float speed, Projectile.Spec spec)
        {
            float r = angleDeg * Mathf.Deg2Rad;
            spec.velocity = new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * speed;
            Projectile.Spawn(prefab, pos, spec);
        }

        void Fire(WeaponData w)
        {
            Vector2 dir = pc.Aim.Direction;
            Vector2 pos = MuzzlePosition;
            float angle = pc.Aim.AngleDegrees;

            switch (w.mode)
            {
                case FireMode.Straight:
                    ShootAt(w.projectile, pos, angle + UnityEngine.Random.Range(-1f, 1f), w.speed, BaseSpec(w, w.damage, w.lifetime));
                    break;

                case FireMode.Spread:
                    for (int i = 0; i < w.pellets; i++)
                    {
                        float t = w.pellets == 1 ? 0f : i / (float)(w.pellets - 1) - 0.5f;
                        float a = angle + t * 2f * w.spreadDegrees + UnityEngine.Random.Range(-1.7f, 1.7f);
                        var spec = BaseSpec(w, w.damage, w.lifetime * UnityEngine.Random.Range(0.88f, 1.08f));
                        ShootAt(w.projectile, pos, a, w.speed * UnityEngine.Random.Range(0.92f, 1.06f), spec);
                    }
                    break;

                case FireMode.Homing:
                {
                    var spec = BaseSpec(w, w.damage, w.lifetime);
                    spec.homingTurnDeg = w.homingTurnDegPerSec;
                    ShootAt(w.projectile, pos, angle + UnityEngine.Random.Range(-17f, 17f), w.speed, spec);
                    break;
                }

                case FireMode.Lob:
                {
                    var spec = BaseSpec(w, w.damage, w.lifetime);
                    spec.gravity = w.gravity;
                    spec.bounces = w.bounces;
                    spec.velocity = dir * w.speed + Vector2.up * w.lobUpSpeed;
                    Projectile.Spawn(w.projectile, pos, spec);
                    break;
                }
            }

            if (w.recoil > 0f) pc.Motor.AddRecoil(-dir.x * w.recoil);
            CameraShake.Add(w.shake);
            FX.Spawn(muzzleFlashFx, pos, 0.3f);
            Fired?.Invoke();
        }

        void FireCharge(WeaponData w, bool full)
        {
            Vector2 pos = MuzzlePosition;
            float angle = pc.Aim.AngleDegrees;
            if (full)
            {
                var spec = BaseSpec(w, w.chargedDamage, w.lifetime);
                spec.pierce = true;
                ShootAt(w.chargedProjectile != null ? w.chargedProjectile : w.projectile, pos, angle, w.chargedSpeed, spec);
                pc.Motor.AddRecoil(-pc.Aim.Direction.x * 9.6f);
                CameraShake.Add(0.3f);
                HitStop.Freeze(0.05f);
            }
            else
            {
                ShootAt(w.projectile, pos, angle, w.speed, BaseSpec(w, w.damage, w.lifetime));
                CameraShake.Add(w.shake);
            }
            FX.Spawn(muzzleFlashFx, pos, 0.3f);
            Fired?.Invoke();
        }

        void FireEx(WeaponData w)
        {
            Vector2 dir = pc.Aim.Direction;
            Vector2 pos = MuzzlePosition;
            float angle = pc.Aim.AngleDegrees;
            Projectile prefab = w.exProjectile != null ? w.exProjectile : w.projectile;
            var spec = BaseSpec(w, w.exDamage, w.exLifetime);

            pc.Motor.Freeze(exFreeze);
            HitStop.Freeze(0.067f);
            CameraShake.Add(0.35f);
            FX.Spawn(exFx, pc.Center, 1f);
            charge = 0f;
            chargeReadyRaised = false;
            cooldown = Mathf.Max(cooldown, exFreeze);

            switch (w.exMode)
            {
                case ExMode.PiercingStar:
                case ExMode.Lance:
                    spec.pierce = true;
                    ShootAt(prefab, pos, angle, w.exSpeed, spec);
                    break;

                case ExMode.Radial:
                {
                    int count = Mathf.Max(1, w.exCount);
                    for (int i = 0; i < count; i++) ShootAt(prefab, pc.Center, i * 360f / count, w.exSpeed, spec);
                    break;
                }

                case ExMode.HomingSwarm:
                {
                    int count = Mathf.Max(1, w.exCount);
                    spec.homingTurnDeg = w.homingTurnDegPerSec * 1.15f;
                    for (int i = 0; i < count; i++) ShootAt(prefab, pos, angle + (i - (count - 1) * 0.5f) * 26f, w.exSpeed, spec);
                    break;
                }

                case ExMode.BigLob:
                    spec.gravity = w.gravity;
                    spec.bounces = 3;
                    spec.velocity = dir * w.exSpeed + Vector2.up * 18f;
                    spec.fragmentPrefab = w.projectile;
                    spec.fragments = 8;
                    spec.fragmentSpeed = 21f;
                    spec.fragmentDamage = 1.2f;
                    Projectile.Spawn(prefab, pos, spec);
                    break;
            }

            pc.Motor.AddRecoil(-dir.x * exRecoil);
            ExFired?.Invoke();
        }
    }
}
