using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Платформа, которая ездит между двумя точками. Игрок на ней получает её скорость (PlayerMotor2D).
    /// Rigidbody2D Kinematic, коллайдер на слое Ground (или OneWay с PlatformEffector2D).
    /// Точки можно сделать детьми платформы: позиции запоминаются при старте.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] Transform pointA;
        [SerializeField] Transform pointB;
        [Tooltip("0.6 px/кадр в прототипе")]
        [SerializeField] float speed = 3.6f;
        [SerializeField] float waitAtEnds = 0f;

        public Vector2 Velocity { get; private set; }

        Rigidbody2D rb;
        Vector2 a, b;
        bool towardsB = true;
        float wait;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            a = pointA != null ? (Vector2)pointA.position : rb.position;
            b = pointB != null ? (Vector2)pointB.position : rb.position;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (wait > 0f)
            {
                wait -= dt;
                Velocity = Vector2.zero;
                return;
            }
            Vector2 target = towardsB ? b : a;
            Vector2 pos = rb.position;
            Vector2 next = Vector2.MoveTowards(pos, target, speed * dt);
            Velocity = (next - pos) / dt;
            rb.MovePosition(next);
            if ((next - target).sqrMagnitude < 1e-6f)
            {
                towardsB = !towardsB;
                wait = waitAtEnds;
            }
        }

        void OnDrawGizmos()
        {
            if (pointA == null || pointB == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}
