using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoalShoot
{
    /// <summary>Экран итогов: оценка, время, урон, уголь, награда; кнопки «Ещё раз» и «На карту».</summary>
    public class ResultsView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text grade;
        [SerializeField] TMP_Text stats;
        [SerializeField] TMP_Text note;
        [SerializeField] Button retryButton;
        [SerializeField] Button mapButton;

        void OnEnable()
        {
            GameEvents.LevelFinished += Show;
            if (root != null) root.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (mapButton != null) mapButton.onClick.AddListener(Map);
        }

        void OnDisable()
        {
            GameEvents.LevelFinished -= Show;
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (mapButton != null) mapButton.onClick.RemoveListener(Map);
        }

        void Show(LevelResult r)
        {
            if (root != null) root.SetActive(true);
            bool boss = r.level != null && r.level.isBoss;
            if (title != null) title.text = r.won ? (boss ? "НОКАУТ" : "ПРОЙДЕНО") : "ПОРАЖЕНИЕ";
            if (grade != null)
            {
                grade.gameObject.SetActive(r.won);
                grade.text = r.grade;
            }

            string time = FormatTime(r.time);
            var prog = ProgressionService.Instance;
            if (stats != null)
            {
                if (!r.won) stats.text = $"Босс побеждён на {Mathf.RoundToInt(r.bossProgress * 100f)}%\nВремя {time} · парирования {r.parries}";
                else if (boss) stats.text = $"Время {time} · урон {r.hits} · парирования {r.parries}";
                else
                {
                    int found = prog != null ? prog.CoalFound(r.level) : 0;
                    int total = r.level != null ? r.level.coalCount : 5;
                    stats.text = $"Время {time} · урон {r.hits} · уголь {found}/{total}";
                }
            }
            if (note != null)
            {
                if (!r.won) note.text = "Попробуй другое оружие из лавки или амулет «Лишнее сердце».";
                else if (r.firstClear) note.text = $"+{r.reward} угля за первое прохождение. Загляни в лавку.";
                else note.text = "";
            }
            if (retryButton != null) retryButton.Select();
        }

        static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m}:{s:00.0}";
        }

        void Retry()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.Retry();
        }

        void Map()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.BackToMap();
        }
    }
}
