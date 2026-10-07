using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Здоровье врага или босса: вспышка при попадании, эффект и стоп-кадр при смерти.
    /// Урон от игрока заполняет его супер-шкалу. У босса снимите галочку Destroy On Death.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 3f;
        [SerializeField] Team team = Team.Enemy;
        [SerializeField] SpriteRenderer[] flashRenderers;
        [SerializeField] Color flashColor = new Color(0.14f, 0.27f, 0.88f);
        [SerializeField] float flashTime = 0.08f;
        [SerializeField] GameObject hitFx;
        [SerializeField] GameObject deathFx;
        [SerializeField] float deathHitStop = 0.05f;
        [SerializeField] float deathShake = 0.25f;
        [SerializeField] bool destroyOnDeath = true;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsDead { get; private set; }
        public bool Invulnerable { get; set; }
        public Team Team => team;
        public bool CanBeDamaged => !IsDead && !Invulnerable;

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        Color[] baseColors;
        float flashTimer;

        void Awake()
        {
            Current = maxHealth;
            if (flashRenderers == null || flashRenderers.Length == 0) flashRenderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i] != null) baseColors[i] = flashRenderers[i].color;
        }

        public void SetMax(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(0.01f, value);
            if (refill) Current = maxHealth;
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (!CanBeDamaged || info.SourceTeam == team) return false;

            Current = Mathf.Max(0f, Current - info.Amount);
            if (info.Source != null) info.Source.AddFromDamage(info.Amount);
            flashTimer = flashTime;
            SetFlash(true);
            FX.Spawn(hitFx, info.Point, 1f);
            Damaged?.Invoke(info);
            if (Current <= 0f && !IsDead) Die();
            return true;
        }

        public void Kill()
        {
            if (!IsDead) Die();
        }

        void Die()
        {
            IsDead = true;
            Current = 0f;
            SetFlash(false);
            FX.Spawn(deathFx, transform.position, 3f);
            HitStop.Freeze(deathHitStop);
            CameraShake.Add(deathShake);
            Died?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }

        void Update()
        {
            if (flashTimer <= 0f) return;
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) SetFlash(false);
        }

        void SetFlash(bool on)
        {
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) flashRenderers[i].color = on ? flashColor : baseColors[i];
        }
    }
}
