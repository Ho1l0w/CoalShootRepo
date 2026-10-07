using UnityEngine;

namespace CoalShoot
{
    /// <summary>Подсказка, которая показывается один раз, когда игрок входит в зону. Коллайдер-триггер.</summary>
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] string title = "ЦЕЛЬ НАВЕРХУ";
        [SerializeField, TextArea] string text = "Зажми ↑ вместе со стрелкой: выстрел по диагонали. Shift — стоять и целиться.";

        bool shown;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (shown || other.GetComponentInParent<PlayerController>() == null) return;
            shown = true;
            GameEvents.RaiseBanner(title, text);
        }
    }
}
