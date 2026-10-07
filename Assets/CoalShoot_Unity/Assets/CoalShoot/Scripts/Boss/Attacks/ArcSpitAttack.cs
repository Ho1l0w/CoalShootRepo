using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Плевок кляксами по дуге: каждая клякса рассчитана так, чтобы упасть рядом с игроком.
    /// Одна из них синяя — её можно парировать.
    /// </summary>
    public class ArcSpitAttack : BossAttack
    {
        [SerializeField] Transform mouth;
        [SerializeField] Projectile blobPrefab;
        [SerializeField] int count = 3;
        [Tooltip("Какая по счёту клякса синяя (-1 — ни одна)")]
        [SerializeField] int parryIndex = 1;
        [SerializeField] float windup = 0.43f;
        [SerializeField] float interval = 0.2f;
        [SerializeField] float recover = 0.5f;
        [Tooltip("0.15 px/кадр² в прототипе")]
        [SerializeField] float gravity = 54f;
        [SerializeField] Vector2 upSpeedRange = new Vector2(18f, 25.8f);
        [SerializeField] float aimSpread = 2.6f;
        [Tooltip("Высота пола арены в мировых координатах")]
        [SerializeField] float floorY = 0f;
        [SerializeField] float minX = -14f;
        [SerializeField] float maxX = 8f;
        [SerializeField] Animator animator;
        [SerializeField] string animatorTrigger = "Spit";

        public override IEnumerator Run(BossController boss)
        {
            if (animator != null) animator.SetTrigger(animatorTrigger);
            yield return new WaitForSeconds(windup);

            for (int i = 0; i < count; i++)
            {
                PlayerController target = PlayerRegistry.RandomAlive();
                Vector2 from = mouth.position;
                float baseX = target != null ? target.Center.x : from.x - 6f;
                float tx = Mathf.Clamp(baseX + Random.Range(-aimSpread, aimSpread), minX, maxX);
                float vy = Random.Range(upSpeedRange.x, upSpeedRange.y);
                float drop = Mathf.Max(0f, from.y - floorY);
                float time = (vy + Mathf.Sqrt(vy * vy + 2f * gravity * drop)) / gravity;
                float vx = (tx - from.x) / Mathf.Max(0.05f, time);

                Projectile.Spawn(blobPrefab, from, new Projectile.Spec
                {
                    team = Team.Enemy,
                    damage = 1f,
                    velocity = new Vector2(vx, vy),
                    gravity = gravity,
                    lifetime = 6f,
                    parryable = i == parryIndex,
                    kind = DamageKind.Projectile
                });
                CameraShake.Add(0.06f);
                yield return new WaitForSeconds(interval);
            }
            yield return new WaitForSeconds(recover);
        }
    }
}
