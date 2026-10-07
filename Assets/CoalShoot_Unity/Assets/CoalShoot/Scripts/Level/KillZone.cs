using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Триггер под уровнем. Упавший игрок возвращается на последнюю надёжную точку и теряет сердце.
    /// Упавшие враги удаляются.
    /// </summary>
    public class KillZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                if (pc.Health.IsDown) return;
                pc.Motor.Teleport(pc.Motor.LastSafePosition);
                pc.Health.TakeDamage(new DamageInfo { Amount = 1f, Point = pc.Center, SourceTeam = Team.Enemy, Kind = DamageKind.Fall });
                return;
            }
            var enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null) Destroy(enemy.gameObject);
        }
    }
}
