using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Щит щитоносца. Дочерний коллайдер-триггер на слое Enemy перед телом врага.
    /// Гасит снаряды, летящие в лицевую сторону. Проходят: выстрелы по диагонали сверху,
    /// снаряды со спины, Уголёк на спуске дуги, пробивающие выстрелы и супер.
    /// </summary>
    public class Shield : MonoBehaviour
    {
        [Tooltip("Объект, чей scale.x задаёт направление взгляда (обычно визуал врага)")]
        [SerializeField] Transform facingSource;
        [Tooltip("Насколько «в лоб» должен лететь снаряд, чтобы его заблокировало")]
        [SerializeField, Range(-1f, 0f)] float frontDot = -0.3f;
        [Tooltip("Снаряды, летящие вниз круче этого, проходят сверху")]
        [SerializeField, Range(-1f, 0f)] float steepFallPasses = -0.6f;
        [SerializeField] GameObject blockFx;

        public event Action Blocked;

        public int Facing => (facingSource != null ? facingSource : transform).lossyScale.x >= 0f ? 1 : -1;

        public bool Blocks(Vector2 projectileVelocity)
        {
            if (projectileVelocity.sqrMagnitude < 0.0001f) return false;
            Vector2 d = projectileVelocity.normalized;
            return d.x * Facing < frontDot && d.y > steepFallPasses;
        }

        public void OnBlocked(Vector2 point)
        {
            FX.Spawn(blockFx, point, 1f);
            Blocked?.Invoke();
        }
    }
}
