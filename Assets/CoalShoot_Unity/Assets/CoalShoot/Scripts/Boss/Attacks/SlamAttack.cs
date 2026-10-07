using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Удар сверху: босс поднимается над игроком, дрожит (предупреждение), падает и пускает
    /// по полу две ударные волны. Через волны надо перепрыгивать.
    /// </summary>
    public class SlamAttack : BossAttack
    {
        [Tooltip("Что двигаем — обычно корень босса")]
        [SerializeField] Transform body;
        [Tooltip("Высота пола арены")]
        [SerializeField] float floorY = 0f;
        [Tooltip("Расстояние от пивота босса до его нижнего края")]
        [SerializeField] float pivotToBottom = 0f;
        [SerializeField] float hoverHeight = 13f;
        [SerializeField] float trackTime = 0.7f;
        [SerializeField] float shakeBeforeDrop = 0.25f;
        [Tooltip("0.9 px/кадр² в прототипе")]
        [SerializeField] float slamGravity = 324f;
        [SerializeField] float holdOnGround = 0.5f;
        [SerializeField] float riseTime = 0.75f;
        [SerializeField] float minX = -12f;
        [SerializeField] float maxX = 12f;
        [SerializeField] Shockwave shockwavePrefab;
        [SerializeField] float shockwaveSpeed = 15.6f;
        [SerializeField] GameObject impactFx;

        Vector3 restPosition;
        bool running;

        public override IEnumerator Run(BossController boss)
        {
            running = true;
            restPosition = body.position;

            PlayerController target = PlayerRegistry.RandomAlive();
            float tx = Mathf.Clamp(target != null ? target.Center.x : restPosition.x, minX, maxX);
            Vector3 start = body.position;
            Vector3 top = new Vector3(tx, floorY + pivotToBottom + hoverHeight, start.z);

            float t = 0f;
            while (t < trackTime)
            {
                t += Time.deltaTime;
                body.position = Vector3.Lerp(start, top, Mathf.SmoothStep(0f, 1f, t / trackTime));
                yield return null;
            }

            t = 0f;
            while (t < shakeBeforeDrop)
            {
                t += Time.deltaTime;
                body.position = top + (Vector3)(Random.insideUnitCircle * 0.08f);
                yield return null;
            }
            body.position = top;

            float vy = 0f;
            float groundPivotY = floorY + pivotToBottom;
            while (body.position.y > groundPivotY)
            {
                vy += slamGravity * Time.deltaTime;
                body.position += Vector3.down * (vy * Time.deltaTime);
                yield return null;
            }
            body.position = new Vector3(body.position.x, groundPivotY, body.position.z);

            CameraShake.Add(0.75f);
            HitStop.Freeze(0.07f);
            FX.Spawn(impactFx, new Vector3(body.position.x, floorY, 0f), 2f);
            SpawnWave(boss, -1);
            SpawnWave(boss, 1);

            yield return new WaitForSeconds(holdOnGround);

            start = body.position;
            t = 0f;
            while (t < riseTime)
            {
                t += Time.deltaTime;
                body.position = Vector3.Lerp(start, restPosition, Mathf.SmoothStep(0f, 1f, t / riseTime));
                yield return null;
            }
            body.position = restPosition;
            running = false;
        }

        void SpawnWave(BossController boss, int dir)
        {
            if (shockwavePrefab == null) return;
            var wave = Instantiate(shockwavePrefab, new Vector3(body.position.x, floorY, 0f), Quaternion.identity);
            wave.Launch(dir, shockwaveSpeed);
            boss.Track(wave.gameObject);
        }

        public override void Cancel(BossController boss)
        {
            if (!running) return;
            body.position = restPosition;
            running = false;
        }
    }
}
