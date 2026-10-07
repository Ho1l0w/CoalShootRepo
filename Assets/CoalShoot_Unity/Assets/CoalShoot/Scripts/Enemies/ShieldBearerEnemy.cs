using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Щитоносец: медленно идёт к игроку и с задержкой разворачивается к нему щитом.
    /// В лоб его не пробить: бейте по диагонали сверху, со спины, Угольком, пробивающим выстрелом или прыжком на голову.
    /// Задержка разворота — окно, чтобы забежать за спину рывком. Здоровье 5.
    /// </summary>
    public class ShieldBearerEnemy : EnemyBase
    {
        [SerializeField] float patrolSpeed = 1.8f;
        [SerializeField] float approachSpeed = 1.7f;
        [SerializeField] float noticeRange = 13f;
        [Tooltip("34 кадра в прототипе")]
        [SerializeField] float turnDelay = 0.57f;
        [SerializeField] LayerMask groundMask;
        [SerializeField] Transform minX;
        [SerializeField] Transform maxX;

        float turnTimer;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 pos = rb.position;
            PlayerController target = Target(noticeRange);

            if (target != null)
            {
                int want = target.Center.x > pos.x ? 1 : -1;
                if (want != facing)
                {
                    turnTimer += dt;
                    if (turnTimer >= turnDelay) { SetFacing(want); turnTimer = 0f; }
                }
                else turnTimer = 0f;
            }

            float speed = target != null ? approachSpeed : patrolSpeed;
            bool outOfRange = (minX != null && pos.x <= minX.position.x && facing < 0) ||
                              (maxX != null && pos.x >= maxX.position.x && facing > 0);
            if (outOfRange || !CanWalkForward(groundMask))
            {
                if (target == null) SetFacing(-facing);
                else speed = 0f;
            }
            rb.MovePosition(pos + new Vector2(facing * speed + TakeKnockback(dt), 0f) * dt);
        }
    }
}
