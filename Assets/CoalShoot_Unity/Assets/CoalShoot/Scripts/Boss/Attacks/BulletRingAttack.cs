using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Кольцо пуль с просветом снизу: игрок должен найти просвет и встать в него.
    /// Вторая волна сдвинута на полшага. Одна пуля в кольце синяя.
    /// </summary>
    public class BulletRingAttack : BossAttack
    {
        [SerializeField] Transform origin;
        [SerializeField] Projectile bulletPrefab;
        [SerializeField] int bullets = 18;
        [SerializeField] int waves = 2;
        [Tooltip("Ширина просвета в градусах")]
        [SerializeField] float gapDegrees = 37f;
        [Tooltip("Просвет выбирается в этом диапазоне углов (270 — прямо вниз)")]
        [SerializeField] Vector2 gapAngleRange = new Vector2(200f, 340f);
        [Tooltip("1.5 px/кадр в прототипе")]
        [SerializeField] float speed = 9f;
        [SerializeField] float charge = 0.33f;
        [SerializeField] float betweenWaves = 0.67f;
        [SerializeField] float recover = 0.77f;
        [SerializeField] GameObject chargeFx;

        public override IEnumerator Run(BossController boss)
        {
            for (int w = 0; w < waves; w++)
            {
                FX.Spawn(chargeFx, origin.position, charge + 0.1f);
                yield return new WaitForSeconds(charge);

                float gap = Random.Range(gapAngleRange.x, gapAngleRange.y);
                int parry = Random.Range(0, bullets);
                float offset = w % 2 == 1 ? 180f / bullets : 0f;
                for (int i = 0; i < bullets; i++)
                {
                    float a = i * 360f / bullets + offset;
                    if (Mathf.Abs(Mathf.DeltaAngle(a, gap)) < gapDegrees * 0.5f) continue;
                    Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    Projectile.Spawn(bulletPrefab, origin.position, new Projectile.Spec
                    {
                        team = Team.Enemy,
                        damage = 1f,
                        velocity = dir * speed,
                        lifetime = 5f,
                        parryable = i == parry,
                        kind = DamageKind.Projectile
                    });
                }
                CameraShake.Add(0.25f);
                if (w < waves - 1) yield return new WaitForSeconds(betweenWaves);
            }
            yield return new WaitForSeconds(recover);
        }
    }
}
