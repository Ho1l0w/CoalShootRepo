using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Сфера способности (двойной прыжок на уровне «Мосты»). Открывает способность навсегда.
    /// Если способность уже есть, сфера не появляется. Коллайдер-триггер.
    /// </summary>
    public class AbilityPickup : MonoBehaviour
    {
        [SerializeField] Ability ability = Ability.DoubleJump;
        [SerializeField] string bannerTitle = "ДВОЙНОЙ ПРЫЖОК";
        [SerializeField, TextArea] string bannerText = "Открыт навсегда. Нажми прыжок ещё раз в воздухе.";
        [SerializeField] GameObject collectFx;

        void Start()
        {
            var prog = ProgressionService.Instance;
            if (prog != null && prog.HasAbility(ability)) gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            var prog = ProgressionService.Instance;
            if (prog != null) prog.UnlockAbility(ability);
            foreach (var p in PlayerRegistry.All) if (p != null) p.RefreshAbilities();

            HitStop.Freeze(0.17f);
            CameraShake.Add(0.55f);
            FX.Spawn(collectFx, transform.position, 2f);
            GameEvents.RaiseBanner(bannerTitle, bannerText);
            Destroy(gameObject);
        }
    }
}
