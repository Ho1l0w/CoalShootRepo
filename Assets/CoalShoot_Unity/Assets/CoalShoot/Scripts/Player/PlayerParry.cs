using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Парирование: прыжок в воздухе рядом с синим объектом (Parryable).
    /// Подбрасывает вверх, восстанавливает двойной прыжок и рывок, даёт карту супера и короткую неуязвимость.
    /// Подключается к мотору: тот вызывает проверку при прыжке в воздухе раньше двойного прыжка,
    /// а буфер прыжка даёт ~0.2 с запаса на нажатие.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerParry : MonoBehaviour
    {
        [SerializeField] float radius = 0.9f;
        [Tooltip("Слои вражеских снарядов и парируемых предметов (звёзды, призрак друга)")]
        [SerializeField] LayerMask parryMask;
        [Tooltip("4.8 px/кадр в прототипе")]
        [SerializeField] float bounceSpeed = 28.8f;
        [SerializeField] float invulnerability = 0.4f;
        [SerializeField] float hitStop = 0.13f;
        [SerializeField] float shake = 0.25f;
        [SerializeField] GameObject parryFx;

        public event Action Parried;

        PlayerController pc;
        PlayerMotor2D motor;
        ContactFilter2D filter;
        Func<bool> handler;
        readonly List<Collider2D> found = new List<Collider2D>(8);

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            motor = GetComponent<PlayerMotor2D>();
            filter = new ContactFilter2D { useLayerMask = true, layerMask = parryMask, useTriggers = true };
            handler = TryParry;
        }

        void OnEnable() => motor.TryAirAction = handler;

        void OnDisable()
        {
            if (motor != null && motor.TryAirAction == handler) motor.TryAirAction = null;
        }

        bool TryParry()
        {
            int n = Physics2D.OverlapCircle(pc.Center, radius, filter, found);
            Parryable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var p = found[i].GetComponentInParent<Parryable>();
                if (p == null || !p.IsReady) continue;
                float d = ((Vector2)found[i].bounds.center - pc.Center).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = p; }
            }
            if (best == null) return false;

            motor.LaunchUp(bounceSpeed, true);
            pc.Health.GrantInvulnerability(invulnerability);
            pc.Super.Add(1f);
            HitStop.Freeze(hitStop);
            CameraShake.Add(shake);
            FX.Spawn(parryFx, best.transform.position, 1.5f);
            best.Parry(pc);
            Parried?.Invoke();
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.14f, 0.27f, 0.88f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
