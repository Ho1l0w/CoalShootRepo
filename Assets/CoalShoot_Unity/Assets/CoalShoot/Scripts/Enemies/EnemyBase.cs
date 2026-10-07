using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Общая основа врагов: здоровье, направление взгляда, отдача от попаданий, поиск ближайшего игрока.
    ///
    /// Сборка врага:
    ///   Корень — слой Enemy, Rigidbody2D (Kinematic), BoxCollider2D (не триггер), Health, скрипт врага.
    ///   Visual — спрайты, отражается по scale.x (рисуйте врага смотрящим вправо).
    ///   Contact — BoxCollider2D (isTrigger) на слое Hazard + ContactDamage.
    ///   Shield (только щитоносец) — BoxCollider2D (isTrigger) на слое Enemy + Shield, ребёнок Visual.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected Transform visual;
        [SerializeField] protected int startFacing = -1;
        [Tooltip("Скорость отлёта при попадании")]
        [SerializeField] protected float knockbackOnHit = 8f;
        [SerializeField] protected float knockbackDecay = 60f;

        protected Health health;
        protected Rigidbody2D rb;
        protected Collider2D body;
        protected int facing = -1;
        protected float knockback;

        public int Facing => facing;

        protected virtual void Awake()
        {
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            body = GetComponent<Collider2D>();
            health.Damaged += OnDamaged;
            SetFacing(startFacing);
        }

        protected virtual void OnDestroy()
        {
            if (health != null) health.Damaged -= OnDamaged;
        }

        protected virtual void OnDamaged(DamageInfo info)
        {
            float dir = Mathf.Abs(info.Direction.x) > 0.01f ? Mathf.Sign(info.Direction.x) : -facing;
            knockback = dir * knockbackOnHit;
        }

        protected void SetFacing(int dir)
        {
            if (dir == 0) return;
            facing = dir > 0 ? 1 : -1;
            Transform t = visual != null ? visual : transform;
            Vector3 s = t.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            t.localScale = s;
        }

        protected PlayerController Target(float range) => PlayerRegistry.Nearest(rb.position, range);

        /// <summary>Текущая отдача; затухает со временем.</summary>
        protected float TakeKnockback(float dt)
        {
            float k = knockback;
            knockback = Mathf.MoveTowards(knockback, 0f, knockbackDecay * dt);
            return k;
        }

        /// <summary>Есть ли земля впереди под ногами и нет ли стены.</summary>
        protected bool CanWalkForward(LayerMask groundMask, float edgeDistance = 0.6f, float wallDistance = 0.2f)
        {
            Bounds b = body != null ? body.bounds : new Bounds(rb.position, Vector3.one * 0.8f);
            float frontX = facing > 0 ? b.max.x + 0.05f : b.min.x - 0.05f;
            bool ground = Physics2D.Raycast(new Vector2(frontX, b.min.y + 0.05f), Vector2.down, edgeDistance, groundMask).collider != null;
            bool wall = Physics2D.Raycast(new Vector2(facing > 0 ? b.max.x : b.min.x, b.center.y), Vector2.right * facing, wallDistance, groundMask).collider != null;
            return ground && !wall;
        }
    }
}
