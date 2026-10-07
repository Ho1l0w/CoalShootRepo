using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Турель: видит игрока — заряжается (видимый сигнал) и стреляет медленным снарядом в него.
    /// Каждый N-й снаряд синий и парируется. Потолочная турель — тот же префаб, повёрнутый вверх ногами
    /// (Visual scale.y = -1). Здоровье 6.
    /// </summary>
    public class TurretEnemy : EnemyBase
    {
        [SerializeField] Projectile bulletPrefab;
        [SerializeField] Transform muzzle;
        [SerializeField] float range = 17f;
        [Tooltip("95 кадров в прототипе")]
        [SerializeField] float interval = 1.58f;
        [Tooltip("Сколько секунд до выстрела виден заряд")]
        [SerializeField] float telegraph = 0.4f;
        [SerializeField] float bulletSpeed = 10.2f;
        [SerializeField] float bulletLifetime = 3.6f;
        [SerializeField] int parryEvery = 3;
        [SerializeField] GameObject chargeFx;
        [SerializeField] Animator animator;

        public event Action Charging;
        public event Action Fired;

        float timer;
        int shots;
        bool charging;

        void Start() => timer = interval;

        void FixedUpdate()
        {
            PlayerController target = Target(range);
            if (target == null)
            {
                timer = Mathf.Max(timer, 0.67f);
                charging = false;
                return;
            }

            SetFacing(target.Center.x > rb.position.x ? 1 : -1);
            timer -= Time.fixedDeltaTime;
            if (!charging && timer <= telegraph)
            {
                charging = true;
                Vector3 at = muzzle != null ? muzzle.position : transform.position;
                FX.Spawn(chargeFx, at, telegraph + 0.1f);
                if (animator != null) animator.SetTrigger("Charge");
                Charging?.Invoke();
            }
            if (timer <= 0f)
            {
                Fire(target);
                timer = interval;
                charging = false;
            }
        }

        void Fire(PlayerController target)
        {
            Vector2 from = muzzle != null ? (Vector2)muzzle.position : rb.position;
            Vector2 dir = (target.Center - from).normalized;
            bool parry = parryEvery > 0 && shots % parryEvery == parryEvery - 1;
            Projectile.Spawn(bulletPrefab, from, new Projectile.Spec
            {
                team = Team.Enemy,
                damage = 1f,
                velocity = dir * bulletSpeed,
                lifetime = bulletLifetime,
                parryable = parry,
                kind = DamageKind.Projectile
            });
            shots++;
            CameraShake.Add(0.04f);
            if (animator != null) animator.SetTrigger("Fire");
            Fired?.Invoke();
        }
    }
}
