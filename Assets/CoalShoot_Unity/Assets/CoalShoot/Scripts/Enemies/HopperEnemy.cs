using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Прыгун: сидит, потом прыгает в сторону игрока. Гравитация своя, приземление — BoxCast вниз.
    /// Его же можно использовать как миньона босса: Launch() бросает его из точки появления. Здоровье 3.
    /// </summary>
    public class HopperEnemy : EnemyBase
    {
        [SerializeField] float hopSpeedX = 7.8f;
        [SerializeField] float hopSpeedY = 24f;
        [SerializeField] float gravity = 90f;
        [SerializeField] float waitMin = 0.5f;
        [SerializeField] float waitMax = 0.9f;
        [SerializeField] float noticeRange = 9f;
        [SerializeField] LayerMask groundMask;
        [SerializeField] Transform minX;
        [SerializeField] Transform maxX;
        [SerializeField] GameObject landDust;
        [Tooltip("Миньоны босса исчезают сами")]
        [SerializeField] float lifetime = 0f;

        Vector2 vel;
        bool grounded = true;
        float wait;
        float age;

        protected override void Awake()
        {
            base.Awake();
            wait = Random.Range(waitMin, waitMax);
        }

        /// <summary>Бросить в воздух (миньон из пасти босса).</summary>
        public void Launch(Vector2 velocity)
        {
            vel = velocity;
            grounded = false;
            SetFacing(velocity.x >= 0f ? 1 : -1);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (lifetime > 0f && age > lifetime) { health.Kill(); return; }

            Vector2 pos = rb.position;
            if (grounded)
            {
                float k = TakeKnockback(dt);
                if (k != 0f) rb.MovePosition(pos + new Vector2(k * dt, 0f));
                wait -= dt;
                if (wait <= 0f) Hop();
                return;
            }

            vel.y -= gravity * dt;
            Vector2 next = pos + (vel + new Vector2(TakeKnockback(dt), 0f)) * dt;
            if (minX != null && next.x < minX.position.x) { next.x = minX.position.x; vel.x = Mathf.Abs(vel.x); }
            if (maxX != null && next.x > maxX.position.x) { next.x = maxX.position.x; vel.x = -Mathf.Abs(vel.x); }

            if (vel.y < 0f && body != null)
            {
                Bounds b = body.bounds;
                RaycastHit2D hit = Physics2D.BoxCast(b.center, b.size * 0.9f, 0f, Vector2.down, -vel.y * dt + 0.02f, groundMask);
                if (hit.collider != null)
                {
                    next.y = hit.centroid.y - (b.center.y - pos.y);
                    grounded = true;
                    vel = Vector2.zero;
                    wait = Random.Range(waitMin, waitMax);
                    FX.Spawn(landDust, new Vector3(next.x, b.min.y, 0f), 1f);
                }
            }
            rb.MovePosition(next);
        }

        void Hop()
        {
            PlayerController target = Target(noticeRange);
            int dir = target != null ? (target.Center.x > rb.position.x ? 1 : -1) : -facing;
            SetFacing(dir);
            vel = new Vector2(dir * hopSpeedX, hopSpeedY);
            grounded = false;
        }
    }
}
