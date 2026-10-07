using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Сочность движения: сжатие и растяжение при прыжке и приземлении, пыль, след рывка,
    /// мигание во время неуязвимости, разворот спрайта, параметры аниматора.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisuals : MonoBehaviour
    {
        [Tooltip("Дочерний объект со спрайтом. Пивот спрайта — внизу по центру, тогда сжатие идёт от ног")]
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer sprite;
        [Tooltip("Необязательно. Параметры: Speed (float), Grounded (bool), VelY (float), Dashing (bool), Gliding (bool)")]
        [SerializeField] Animator animator;
        [SerializeField] GameObject jumpDust;
        [SerializeField] GameObject landDust;
        [SerializeField] GameObject airJumpFx;
        [SerializeField] GameObject dashFx;
        [SerializeField] float squashReturnSpeed = 11f;
        [SerializeField] Color dashGhostColor = new Color(0.14f, 0.27f, 0.88f, 0.6f);
        [SerializeField] float dashGhostInterval = 0.033f;
        [SerializeField] float dashGhostLife = 0.23f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int VelYId = Animator.StringToHash("VelY");
        static readonly int DashId = Animator.StringToHash("Dashing");
        static readonly int GlideId = Animator.StringToHash("Gliding");

        PlayerController pc;
        PlayerMotor2D motor;
        PlayerHealth health;
        Vector2 squash = Vector2.one;
        float ghostTimer;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            motor = GetComponent<PlayerMotor2D>();
            health = GetComponent<PlayerHealth>();
        }

        void OnEnable()
        {
            motor.Jumped += OnJump;
            motor.AirJumped += OnAirJump;
            motor.Landed += OnLand;
            motor.Dashed += OnDash;
        }

        void OnDisable()
        {
            motor.Jumped -= OnJump;
            motor.AirJumped -= OnAirJump;
            motor.Landed -= OnLand;
            motor.Dashed -= OnDash;
        }

        Vector3 Feet => body != null ? body.position : transform.position;

        void OnJump()
        {
            squash = new Vector2(0.65f, 1.4f);
            FX.Spawn(jumpDust, Feet, 1f);
        }

        void OnAirJump()
        {
            squash = new Vector2(0.65f, 1.4f);
            FX.Spawn(airJumpFx, Feet, 1f);
        }

        void OnLand(float fallSpeed)
        {
            float k = Mathf.Clamp01(fallSpeed / 40f);
            float sx = 1f + 0.5f * k;
            squash = new Vector2(sx, 1f / sx);
            if (fallSpeed > 8f) FX.Spawn(landDust, Feet, 1f);
            if (fallSpeed > 33f) CameraShake.Add(0.22f);
        }

        void OnDash()
        {
            squash = new Vector2(1.6f, 0.65f);
            FX.Spawn(dashFx, pc.Center, 1f);
            CameraShake.Add(0.25f);
            HitStop.Freeze(0.033f);
        }

        void LateUpdate()
        {
            squash = Vector2.Lerp(squash, Vector2.one, 1f - Mathf.Exp(-squashReturnSpeed * Time.deltaTime));
            if (body != null) body.localScale = new Vector3(squash.x * motor.Facing, squash.y, 1f);

            if (sprite != null)
            {
                bool blink = health.IsInvulnerable && !motor.IsDashing && Mathf.FloorToInt(Time.time * 20f) % 2 == 0;
                Color c = sprite.color;
                c.a = blink ? 0.35f : 1f;
                sprite.color = c;
            }

            if (animator != null)
            {
                animator.SetFloat(SpeedId, Mathf.Abs(motor.Velocity.x));
                animator.SetBool(GroundedId, motor.IsGrounded);
                animator.SetFloat(VelYId, motor.Velocity.y);
                animator.SetBool(DashId, motor.IsDashing);
                animator.SetBool(GlideId, motor.IsGliding);
            }

            if (motor.IsDashing && sprite != null)
            {
                ghostTimer -= Time.deltaTime;
                if (ghostTimer <= 0f)
                {
                    ghostTimer = dashGhostInterval;
                    SpawnDashGhost();
                }
            }
        }

        void SpawnDashGhost()
        {
            var go = new GameObject("DashGhost");
            go.transform.SetPositionAndRotation(sprite.transform.position, sprite.transform.rotation);
            go.transform.localScale = sprite.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite.sprite;
            sr.flipX = sprite.flipX;
            sr.color = dashGhostColor;
            sr.sortingLayerID = sprite.sortingLayerID;
            sr.sortingOrder = sprite.sortingOrder - 1;
            go.AddComponent<FadeAndDestroy>().Init(dashGhostLife);
        }
    }
}
