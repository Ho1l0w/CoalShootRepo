using System;
using UnityEngine;
using UnityEngine.Events;

namespace CoalShoot
{
    /// <summary>
    /// Объект, от которого можно оттолкнуться парированием: синие снаряды, звёзды над пропастью, призрак друга.
    /// Нужен коллайдер-триггер на слое из PlayerParry.parryMask.
    /// Снаряд включает и выключает этот компонент сам (Spec.parryable).
    /// </summary>
    public class Parryable : MonoBehaviour
    {
        [Tooltip("Снаряды и призраки исчезают; звёзды остаются и перезаряжаются")]
        [SerializeField] bool destroyOnParry = true;
        [SerializeField] float cooldown = 2.5f;
        [SerializeField] GameObject parryFx;
        [SerializeField] UnityEvent onParried;

        public bool IsReady => isActiveAndEnabled && Time.time >= readyAt;
        public float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

        public event Action<PlayerController> Parried;

        float readyAt;

        public void Parry(PlayerController by)
        {
            FX.Spawn(parryFx, transform.position, 1.5f);
            onParried?.Invoke();
            Parried?.Invoke(by);
            if (destroyOnParry)
            {
                var projectile = GetComponent<Projectile>();
                if (projectile != null) projectile.Despawn();
                else Destroy(gameObject);
            }
            else
            {
                readyAt = Time.time + cooldown;
            }
        }
    }
}
