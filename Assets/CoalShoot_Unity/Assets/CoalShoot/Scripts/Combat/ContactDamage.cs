using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Урон при касании для врагов и шипов. Вешается на дочерний коллайдер-триггер на слое Hazard.
    /// Прыжок сверху ранит врага и подбрасывает игрока. Рывок таранит врага насквозь,
    /// но щитоносца в лоб — отбрасывает. Шипы могут подбрасывать игрока (bouncePlayerUp).
    /// </summary>
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] Team team = Team.Enemy;
        [Tooltip("Здоровье владельца. Пусто — ищется в родителях. У шипов не нужно")]
        [SerializeField] Health health;
        [SerializeField] bool stompable = true;
        [SerializeField] float stompDamage = 2f;
        [SerializeField] float dashDamage = 3f;
        [Tooltip("4.4 px/кадр в прототипе")]
        [SerializeField] float stompBounce = 26.4f;
        [Tooltip("Шипы: подбросить игрока после удара")]
        [SerializeField] bool bouncePlayerUp = false;
        [SerializeField] float hazardBounce = 27.6f;
        [Tooltip("Если есть щит — рывок в лицо отскакивает")]
        [SerializeField] Shield shield;
        [SerializeField] float dashKnockback = 15.6f;

        Collider2D ownCollider;
        float nextDashHitTime;

        void Awake()
        {
            if (health == null) health = GetComponentInParent<Health>();
            ownCollider = GetComponent<Collider2D>();
        }

        void OnTriggerStay2D(Collider2D other) => Handle(other);
        void OnCollisionStay2D(Collision2D collision) => Handle(collision.collider);

        void Handle(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null || pc.Health.IsDown) return;
            var motor = pc.Motor;

            if (health != null && health.CanBeDamaged)
            {
                if (motor.IsDashing)
                {
                    if (shield != null && motor.DashDirection == -shield.Facing)
                    {
                        motor.Knockback(-motor.DashDirection);
                        pc.Health.GrantInvulnerability(0.25f);
                        shield.OnBlocked(other.bounds.center);
                        CameraShake.Add(0.2f);
                        return;
                    }
                    if (Time.time >= nextDashHitTime)
                    {
                        nextDashHitTime = Time.time + 0.2f;
                        health.TakeDamage(new DamageInfo
                        {
                            Amount = dashDamage, Point = other.bounds.center, Direction = Vector2.right * motor.DashDirection,
                            SourceTeam = Team.Player, Kind = DamageKind.Dash, Source = pc.Super
                        });
                    }
                    pc.Health.GrantInvulnerability(0.17f);
                    return;
                }

                float myTop = ownCollider != null ? ownCollider.bounds.max.y : transform.position.y;
                if (stompable && motor.Velocity.y <= 0f && other.bounds.min.y >= myTop - 0.3f)
                {
                    health.TakeDamage(new DamageInfo
                    {
                        Amount = stompDamage, Point = other.bounds.center, Direction = Vector2.down,
                        SourceTeam = Team.Player, Kind = DamageKind.Stomp, Source = pc.Super
                    });
                    motor.LaunchUp(stompBounce, true);
                    pc.Health.GrantInvulnerability(0.17f);
                    return;
                }
            }

            bool hurt = pc.Health.TakeDamage(new DamageInfo
            {
                Amount = 1f, Point = transform.position, SourceTeam = team,
                Kind = health != null ? DamageKind.Contact : DamageKind.Hazard
            });
            if (hurt && bouncePlayerUp) motor.LaunchUp(hazardBounce, false);
        }
    }
}
