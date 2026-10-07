using System.Collections.Generic;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Снаряд игрока или врага. Двигается сам (Kinematic), стены проверяет CircleCast-ом по пути,
    /// поэтому быстрые пули не пролетают сквозь тонкие стены. Цели ищет OverlapCircle по маске своей команды.
    /// Поведение задаётся Spec: дуга, отскоки, пробивание, самонаведение, осколки, парирование.
    ///
    /// Префаб: Rigidbody2D (Kinematic) + CircleCollider2D (isTrigger) + SpriteRenderer + этот скрипт.
    /// Вражеским снарядам добавьте Parryable (выключенным) и слой EnemyProjectile.
    /// В solidMask — только сплошная земля: сквозь односторонние платформы стрелять можно.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        public struct Spec
        {
            public Team team;
            public float damage;
            public Vector2 velocity;
            public float lifetime;
            public float gravity;
            public int bounces;
            public bool pierce;
            public float homingTurnDeg;
            public bool parryable;
            public SuperMeter owner;
            public DamageKind kind;
            public Projectile fragmentPrefab;
            public int fragments;
            public float fragmentSpeed;
            public float fragmentDamage;
        }

        [SerializeField] float radius = 0.15f;
        [SerializeField] LayerMask solidMask;
        [Tooltip("Кого бьют вражеские снаряды: слой Player")]
        [SerializeField] LayerMask hitsPlayersMask;
        [Tooltip("Кого бьют снаряды игрока: слой Enemy")]
        [SerializeField] LayerMask hitsEnemiesMask;
        [SerializeField] float homingRange = 10f;
        [SerializeField] bool alignToVelocity = true;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Color parryColor = new Color(0.14f, 0.27f, 0.88f);
        [SerializeField] GameObject impactFx;
        [SerializeField] GameObject bounceFx;

        static readonly List<Projectile> active = new List<Projectile>();
        public static IReadOnlyList<Projectile> Active => active;

        Spec spec;
        Rigidbody2D rb;
        Parryable parryable;
        Color baseColor = Color.white;
        float age;
        float retargetTimer;
        Vector2 vel;
        Collider2D homingTarget;
        ContactFilter2D targetFilter;
        bool alive;
        readonly List<Collider2D> results = new List<Collider2D>(8);
        readonly Dictionary<IDamageable, float> rehitAt = new Dictionary<IDamageable, float>();

        public Team Team => spec.team;
        public Vector2 Velocity => vel;

        public static Projectile Spawn(Projectile prefab, Vector2 position, Spec spec)
        {
            if (prefab == null) return null;
            Projectile p = ProjectilePool.Get(prefab);
            p.Launch(position, spec);
            return p;
        }

        /// <summary>Убрать все снаряды команды: смена фазы босса, нокаут.</summary>
        public static void DespawnTeam(Team team)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (i >= active.Count) continue;
                var p = active[i];
                if (p == null) { active.RemoveAt(i); continue; }
                if (p.spec.team == team) p.Despawn();
            }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            parryable = GetComponent<Parryable>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;
        }

        void Launch(Vector2 position, Spec s)
        {
            spec = s;
            if (spec.lifetime <= 0f) spec.lifetime = 2f;
            vel = s.velocity;
            age = 0f;
            retargetTimer = 0f;
            homingTarget = null;
            rehitAt.Clear();
            alive = true;

            transform.position = position;
            rb.position = position;
            targetFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = spec.team == Team.Player ? hitsEnemiesMask : hitsPlayersMask,
                useTriggers = true
            };
            if (parryable != null) parryable.enabled = spec.parryable;
            if (sprite != null) sprite.color = spec.parryable ? parryColor : baseColor;
            Align();
            active.Add(this);
        }

        void FixedUpdate()
        {
            if (!alive) return;
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (age >= spec.lifetime) { End(rb.position); return; }
            if (spec.homingTurnDeg > 0f) Steer(dt);
            if (spec.gravity != 0f) vel.y -= spec.gravity * dt;

            Vector2 pos = rb.position;
            Vector2 step = vel * dt;
            float dist = step.magnitude;
            if (dist > 1e-5f)
            {
                RaycastHit2D hit = Physics2D.CircleCast(pos, radius, step / dist, dist, solidMask);
                if (hit.collider != null)
                {
                    if (spec.bounces > 0 && hit.normal.y > 0.5f)
                    {
                        spec.bounces--;
                        vel = Vector2.Reflect(vel, hit.normal) * 0.72f;
                        rb.MovePosition(hit.centroid + hit.normal * 0.02f);
                        FX.Spawn(bounceFx, hit.point, 1f);
                        CameraShake.Add(0.03f);
                        return;
                    }
                    End(hit.centroid);
                    return;
                }
            }

            Vector2 next = pos + step;
            rb.MovePosition(next);
            Align();
            HitTargets(next);
        }

        void HitTargets(Vector2 at)
        {
            int n = Physics2D.OverlapCircle(at, radius, targetFilter, results);
            if (n == 0) return;

            // Сначала щиты: снаряд игрока, летящий в лицевую сторону щита, гаснет.
            if (!spec.pierce && spec.team == Team.Player)
            {
                for (int i = 0; i < n; i++)
                {
                    var shield = results[i].GetComponent<Shield>();
                    if (shield != null && shield.Blocks(vel))
                    {
                        shield.OnBlocked(at);
                        End(at);
                        return;
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                var col = results[i];
                if (col.GetComponent<Shield>() != null) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.Team == spec.team) continue;

                if (spec.pierce)
                {
                    if (rehitAt.TryGetValue(target, out float until) && Time.time < until) continue;
                    rehitAt[target] = Time.time + 0.13f;
                }

                var info = new DamageInfo
                {
                    Amount = spec.damage,
                    Point = at,
                    Direction = vel.sqrMagnitude > 0f ? vel.normalized : Vector2.zero,
                    SourceTeam = spec.team,
                    Kind = spec.kind,
                    Source = spec.owner
                };
                bool landed = target.TakeDamage(info);
                // Пули игрока гаснут о любую цель; вражеские пролетают сквозь неуязвимого игрока.
                if (!spec.pierce && (landed || spec.team == Team.Player))
                {
                    End(at);
                    return;
                }
            }
        }

        void Steer(float dt)
        {
            retargetTimer -= dt;
            if (retargetTimer <= 0f || homingTarget == null || !homingTarget.isActiveAndEnabled)
            {
                homingTarget = FindTarget();
                retargetTimer = 0.15f;
            }
            if (homingTarget == null) return;

            Vector2 to = (Vector2)homingTarget.bounds.center - rb.position;
            float speed = vel.magnitude;
            float current = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            float wanted = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(current, wanted, spec.homingTurnDeg * dt) * Mathf.Deg2Rad;
            vel = new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * speed;
        }

        Collider2D FindTarget()
        {
            int n = Physics2D.OverlapCircle(rb.position, homingRange, targetFilter, results);
            Collider2D best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (results[i].GetComponent<Shield>() != null) continue;
                var d = results[i].GetComponentInParent<IDamageable>();
                if (d == null || d.Team == spec.team || !d.CanBeDamaged) continue;
                float sqr = ((Vector2)results[i].bounds.center - rb.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = results[i]; }
            }
            return best;
        }

        void End(Vector2 at)
        {
            if (!alive) return;
            FX.Spawn(impactFx, at, 1f);
            if (spec.fragments > 0 && spec.fragmentPrefab != null)
            {
                for (int i = 0; i < spec.fragments; i++)
                {
                    float a = i * Mathf.PI * 2f / spec.fragments;
                    Spawn(spec.fragmentPrefab, at, new Spec
                    {
                        team = spec.team,
                        damage = spec.fragmentDamage,
                        velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spec.fragmentSpeed,
                        lifetime = 0.37f,
                        owner = spec.owner,
                        kind = spec.kind
                    });
                }
                CameraShake.Add(0.3f);
                HitStop.Freeze(0.05f);
            }
            Despawn();
        }

        public void Despawn()
        {
            if (!alive) return;
            alive = false;
            active.Remove(this);
            ProjectilePool.Release(this);
        }

        void OnDisable()
        {
            if (!alive) return;
            alive = false;
            active.Remove(this);
        }

        void Align()
        {
            if (!alignToVelocity || vel.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
