using UnityEngine;
using UnityEngine.UI;

namespace CoalShoot
{
    /// <summary>Полоска здоровья босса. Отметки фаз ставятся вручную в макете (дочерние Image на 66% и 33%).</summary>
    public class BossBarView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Image с Type = Filled, Fill Method = Horizontal")]
        [SerializeField] Image fill;
        [SerializeField] float smooth = 12f;

        float target = 1f;

        void OnEnable()
        {
            GameEvents.BossHealthChanged += OnHealth;
            if (root != null) root.SetActive(false);
        }

        void OnDisable() => GameEvents.BossHealthChanged -= OnHealth;

        void OnHealth(float normalized)
        {
            target = Mathf.Clamp01(normalized);
            if (root != null) root.SetActive(true);
        }

        void Update()
        {
            if (fill == null) return;
            fill.fillAmount = Mathf.Lerp(fill.fillAmount, target, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
        }
    }
}
