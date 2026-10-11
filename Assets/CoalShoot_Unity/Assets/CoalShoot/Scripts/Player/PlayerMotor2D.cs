using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Движение игрока. Здесь живёт всё, что делает управление отзывчивым:
    /// coyote time, буфер прыжка, зависание в верхней точке, обрезание прыжка при отпускании,
    /// разгон с усилением на развороте, двойной прыжок, рывок с буфером, коррекция углов о потолок,
    /// отбрасывание, перенос на движущейся платформе.
    ///
    /// Rigidbody2D: Dynamic, Gravity Scale = 0 (гравитацию считаем сами), Freeze Rotation Z,
    /// материал без трения. Коллайдер: BoxCollider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] MovementConfig config;
        [Tooltip("Земля и односторонние платформы: на них можно стоять")]
        [SerializeField] LayerMask groundMask;
        [Tooltip("Только сплошные стены и потолки: через односторонние платформы можно пролететь снизу")]
        [SerializeField] LayerMask solidMask;

        public MovementConfig Config => config;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => dashTimer > 0f;
        public bool IsGliding { get; private set; }
        public bool IsFrozen => freezeTimer > 0f;
        public int Facing { get; private set; } = 1;
        public int DashDirection { get; private set; } = 1;
        /// <summary>Собственная скорость персонажа без скорости платформы.</summary>
        public Vector2 Velocity => vel;
        /// <summary>Последняя точка, где персонаж надёжно стоял. Сюда возвращает падение в яму.</summary>
        public Vector2 LastSafePosition;
        public bool ControlLocked { get; set; }
        /// <summary>0 или 1. Выставляет PlayerController по прогрессу.</summary>
        public int AirJumpsMax { get; set; }
        /// <summary>Вызывается при прыжке в воздухе раньше двойного прыжка. Вернула true — нажатие потрачено (парирование).</summary>
        public Func<bool> TryAirAction { get; set; }

        public event Action Jumped;
        public event Action AirJumped;
        public event Action Dashed;
        /// <summary>Аргумент — скорость падения в момент приземления.</summary>
        public event Action<float> Landed;

        Rigidbody2D rb;
        BoxCollider2D box;
        Vector2 vel;
        Vector2 lastPlatformVel;
        float moveX;
        bool jumpHeld;
        bool holdPosition;
        float coyoteTimer, jumpBufferTimer, dashBufferTimer, dashTimer, knockbackTimer, freezeTimer, groundedTime;
        int airJumpsLeft;
        bool jumpCut;
        bool canDash = true;
        float airVy;
        bool inStep;
        bool hasOverrideX, hasOverrideY;
        float overrideX, overrideY;
        float recoil;
        ContactFilter2D groundFilter;
        readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>(8);
        static readonly int[] Signs = { 1, -1 };

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundMask,
                useNormalAngle = true,
                minNormalAngle = 45f,
                maxNormalAngle = 135f
            };
            LastSafePosition = rb.position;
        }

        /// <summary>Ввод на этот физический кадр. hold — стоять на месте и только целиться.</summary>
        public void SetInput(float x, bool jump, bool hold)
        {
            moveX = Mathf.Clamp(x, -1f, 1f);
            jumpHeld = jump;
            holdPosition = hold;
            if (!ControlLocked && knockbackTimer <= 0f && dashTimer <= 0f && Mathf.Abs(moveX) > 0.01f)
                Facing = moveX > 0f ? 1 : -1;
        }

        public void RequestJump()
        {
            if (!ControlLocked) jumpBufferTimer = config.jumpBuffer;
        }

        public void RequestDash()
        {
            if (!ControlLocked) dashBufferTimer = config.dashBuffer;
        }

        /// <summary>Подбросить вверх: парирование, прыжок по голове врага, шипы.</summary>
        public void LaunchUp(float speed, bool refreshAbilities)
        {
            SetVy(speed);
            jumpCut = true;
            dashTimer = 0f;
            coyoteTimer = 0f;
            IsGrounded = false;
            if (refreshAbilities) RefreshAbilities();
        }

        public void Knockback(int directionX)
        {
            SetVx(directionX * config.knockbackSpeed);
            SetVy(config.knockbackUp);
            knockbackTimer = config.knockbackTime;
            dashTimer = 0f;
            jumpCut = true;
            IsGrounded = false;
        }

        /// <summary>Отдача оружия по горизонтали.</summary>
        public void AddRecoil(float vx)
        {
            if (dashTimer <= 0f && knockbackTimer <= 0f) recoil += vx;
        }

        /// <summary>Застыть на месте (выстрел супером).</summary>
        public void Freeze(float seconds)
        {
            freezeTimer = Mathf.Max(freezeTimer, seconds);
            dashTimer = 0f;
        }

        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
            vel = Vector2.zero;
            lastPlatformVel = Vector2.zero;
            rb.SetVelocity(Vector2.zero);
            dashTimer = knockbackTimer = freezeTimer = 0f;
            hasOverrideX = hasOverrideY = false;
            LastSafePosition = position;
        }

        public void RefreshAbilities()
        {
            airJumpsLeft = AirJumpsMax;
            canDash = true;
        }

        void SetVx(float v)
        {
            if (inStep) vel.x = v;
            else { hasOverrideX = true; overrideX = v; }
        }

        void SetVy(float v)
        {
            if (inStep) vel.y = v;
            else { hasOverrideY = true; overrideY = v; }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            inStep = true;

            // Скорость после столкновений прошлого шага, без вклада платформы.
            vel = rb.GetVelocity() - lastPlatformVel;
            if (hasOverrideX) { vel.x = overrideX; hasOverrideX = false; }
            if (hasOverrideY) { vel.y = overrideY; hasOverrideY = false; }
            if (recoil != 0f) { vel.x += recoil; recoil = 0f; }

            bool wasGrounded = IsGrounded;
            Vector2 platformVel = Vector2.zero;
            IsGrounded = vel.y <= 0.01f && ProbeGround(out platformVel);
            if (IsGrounded)
            {
                if (!wasGrounded) Landed?.Invoke(-airVy);
                coyoteTimer = config.coyoteTime;
                airJumpsLeft = AirJumpsMax;
                canDash = true;
                jumpCut = false;
                groundedTime += dt;
            }
            else
            {
                coyoteTimer -= dt;
                groundedTime = 0f;
            }

            jumpBufferTimer -= dt;
            dashBufferTimer -= dt;
            knockbackTimer -= dt;
            IsGliding = false;

            if (freezeTimer > 0f)
            {
                freezeTimer -= dt;
                vel = Vector2.zero;
                Apply(Vector2.zero);
                return;
            }

            // Прыжок: с земли или в пределах coyote time → парирование → двойной прыжок.
            // Пока буфер жив, проверка повторяется каждый кадр: нажатие чуть раньше времени не пропадает.
            if (jumpBufferTimer > 0f && !ControlLocked)
            {
                if (coyoteTimer > 0f)
                {
                    DoJump(config.JumpVelocity);
                    Jumped?.Invoke();
                }
                else if (TryAirAction != null && TryAirAction())
                {
                    jumpBufferTimer = 0f;
                }
                else if (airJumpsLeft > 0)
                {
                    airJumpsLeft--;
                    DoJump(config.AirJumpVelocity);
                    AirJumped?.Invoke();
                }
            }

            if (dashBufferTimer > 0f && canDash && dashTimer <= 0f && !ControlLocked)
            {
                dashBufferTimer = 0f;
                canDash = false;
                dashTimer = config.dashDuration;
                DashDirection = Facing;
                Dashed?.Invoke();
            }

            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                vel = new Vector2(DashDirection * config.dashSpeed, 0f);
                if (WallAhead(DashDirection)) { dashTimer = 0f; vel.x = 0f; }
                else if (dashTimer <= 0f) vel.x = DashDirection * config.dashExitSpeed;
            }
            else
            {
                float input = ControlLocked || holdPosition || knockbackTimer > 0f ? 0f : moveX;
                float target = input * config.runSpeed;
                float accel;
                if (knockbackTimer > 0f) accel = config.airAccel * 0.2f;
                else if (IsGrounded)
                    accel = Mathf.Abs(input) > 0.01f
                        ? (vel.x * input < 0f ? config.groundAccel * config.turnBoost : config.groundAccel)
                        : config.groundDecel;
                else accel = config.airAccel;
                vel.x = Mathf.MoveTowards(vel.x, target, accel * dt);

                if (IsGrounded && vel.y <= 0f)
                {
                    vel.y = -1f; // лёгкий прижим, чтобы не терять контакт с землёй
                }
                else
                {
                    float g = config.Gravity * (vel.y < 0f ? config.fallGravityMult : 1f);
                    if (jumpHeld && Mathf.Abs(vel.y) < config.apexThreshold) g = config.Gravity * config.apexGravityMult;
                    vel.y = Mathf.Max(vel.y - g * dt, -config.maxFallSpeed);

                    if (config.canGlide && jumpHeld && !ControlLocked && vel.y < -config.glideFallSpeed)
                    {
                        vel.y = -config.glideFallSpeed;
                        IsGliding = true;
                    }
                    if (!jumpHeld && vel.y > 0f && !jumpCut)
                    {
                        vel.y *= config.jumpCutMult;
                        jumpCut = true;
                    }
                }
            }

            if (vel.y > 0f) CornerCorrection(vel.y * dt);
            if (!IsGrounded) airVy = vel.y;
            Apply(IsGrounded && dashTimer <= 0f ? platformVel : Vector2.zero);
        }

        void Apply(Vector2 platformVel)
        {
            lastPlatformVel = platformVel;
            rb.SetVelocity(vel + platformVel);
            inStep = false;
        }

        void DoJump(float speed)
        {
            vel.y = speed;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCut = false;
            dashTimer = 0f;
            IsGrounded = false;
        }

        bool ProbeGround(out Vector2 platformVel)
        {
            platformVel = Vector2.zero;
            int n = rb.GetContacts(groundFilter, contacts);
            for (int i = 0; i < n; i++)
            {
                var platform = contacts[i].collider.GetComponentInParent<MovingPlatform>();
                if (platform != null)
                {
                    platformVel = platform.Velocity;
                    break;
                }
            }
            return n > 0;
        }
        void OnCollisionStay2D(Collision2D c)
        {
            foreach (var p in c.contacts)
                Debug.DrawRay(p.point, p.normal * 0.5f, Mathf.Abs(p.normal.x) > 0.7f ? Color.red : Color.green);
        }
        bool WallAhead(int dir)
        {
            Bounds b = box.bounds;
            Vector2 size = new Vector2(b.size.x * 0.9f, b.size.y * 0.8f);
            float distance = 0.05f + config.dashSpeed * Time.fixedDeltaTime * 0.5f;
            return Physics2D.BoxCast(b.center, size, 0f, Vector2.right * dir, distance, solidMask).collider != null;
        }

        // Задел потолок углом головы — сдвинуть вбок, если рядом есть свободное место.
        void CornerCorrection(float dy)
        {
            Bounds b = box.bounds;
            Vector2 size = new Vector2(b.size.x * 0.98f, b.size.y * 0.98f);
            if (Physics2D.BoxCast(b.center, size, 0f, Vector2.up, dy + 0.02f, solidMask).collider == null) return;

            const int steps = 6;
            for (int i = 1; i <= steps; i++)
            {
                float off = config.cornerCorrection * i / steps;
                foreach (int s in Signs)
                {
                    Vector2 test = (Vector2)b.center + new Vector2(s * off, dy + 0.02f);
                    if (Physics2D.OverlapBox(test, size, 0f, solidMask) == null)
                    {
                        rb.position += new Vector2(s * off, 0f);
                        return;
                    }
                }
            }
        }
    }
}
