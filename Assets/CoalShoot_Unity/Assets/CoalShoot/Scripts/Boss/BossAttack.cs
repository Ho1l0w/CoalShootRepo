using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Одна атака босса — корутина. Хорошая атака состоит из трёх частей:
    /// предупреждение (игрок видит, что будет), действие, восстановление (окно для ответного урона).
    /// </summary>
    public abstract class BossAttack : MonoBehaviour
    {
        public abstract IEnumerator Run(BossController boss);

        /// <summary>Вызывается, если атаку прервала смена фазы или нокаут: верните босса в исходную позу.</summary>
        public virtual void Cancel(BossController boss) { }
    }
}
