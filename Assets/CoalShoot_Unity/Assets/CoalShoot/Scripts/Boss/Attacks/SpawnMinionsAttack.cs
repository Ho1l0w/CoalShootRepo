using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Миньоны: босс выплёвывает несколько прыгунов-клякс. Префаб миньона — HopperEnemy
    /// с небольшим здоровьем и lifetime ≈ 12 с.
    /// </summary>
    public class SpawnMinionsAttack : BossAttack
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] HopperEnemy minionPrefab;
        [SerializeField] int count = 2;
        [SerializeField] float windup = 0.4f;
        [SerializeField] float interval = 0.17f;
        [SerializeField] float recover = 0.33f;
        [SerializeField] Vector2 launchVelocity = new Vector2(-9.6f, 18f);

        public override IEnumerator Run(BossController boss)
        {
            yield return new WaitForSeconds(windup);
            for (int i = 0; i < count; i++)
            {
                var minion = Instantiate(minionPrefab, spawnPoint.position, Quaternion.identity);
                minion.Launch(new Vector2(launchVelocity.x * Random.Range(0.5f, 1.4f), launchVelocity.y));
                boss.Track(minion.gameObject);
                yield return new WaitForSeconds(interval);
            }
            yield return new WaitForSeconds(recover);
        }
    }
}
