using UnityEngine;

namespace CoalShoot
{
    /// <summary>Летун: парит у точки появления, а заметив игрока, преследует его чуть выше головы. Здоровье 2.</summary>
    public class FlyerEnemy : EnemyBase
    {
        [SerializeField] float chaseRange = 11f;
        [SerializeField] Vector2 maxSpeed = new Vector2(2.7f, 2.1f);
        [SerializeField] float follow = 1.8f;
        [SerializeField] float bobSpeed = 5.4f;
        [SerializeField] float bobAmount = 1.5f;
        [SerializeField] float hoverAbovePlayer = 0.8f;
        [SerializeField] LayerMask groundMask;

        Vector2 home;
        float t;

        protected override void Awake()
        {
            base.Awake();
            home = rb.position;
            t = Random.value * 10f;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            t += dt;
            PlayerController target = Target(chaseRange);
            Vector2 goal = target != null ? target.Center + Vector2.up * hoverAbovePlayer : home;
            if (target != null) SetFacing(target.Center.x > rb.position.x ? 1 : -1);

            Vector2 d = goal - rb.position;
            Vector2 v = new Vector2(
                Mathf.Clamp(d.x * follow, -maxSpeed.x, maxSpeed.x) + TakeKnockback(dt),
                Mathf.Clamp(d.y * follow, -maxSpeed.y, maxSpeed.y) + Mathf.Sin(t * bobSpeed) * bobAmount);
            Vector2 next = rb.position + v * dt;
            if (Physics2D.OverlapCircle(next, 0.3f, groundMask) == null) rb.MovePosition(next);
        }
    }
}
