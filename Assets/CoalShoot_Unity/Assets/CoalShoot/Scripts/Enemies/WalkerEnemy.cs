using UnityEngine;

namespace CoalShoot
{
    /// <summary>Ползун: ходит туда-сюда, разворачивается у края и у стены. Здоровье 3.</summary>
    public class WalkerEnemy : EnemyBase
    {
        [Tooltip("0.45 px/кадр в прототипе")]
        [SerializeField] float speed = 2.7f;
        [SerializeField] LayerMask groundMask;
        [Tooltip("Необязательные границы патруля")]
        [SerializeField] Transform minX;
        [SerializeField] Transform maxX;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 pos = rb.position;
            bool outOfRange = (minX != null && pos.x <= minX.position.x && facing < 0) ||
                              (maxX != null && pos.x >= maxX.position.x && facing > 0);
            if (outOfRange || !CanWalkForward(groundMask)) SetFacing(-facing);
            rb.MovePosition(pos + new Vector2(facing * speed + TakeKnockback(dt), 0f) * dt);
        }
    }
}
