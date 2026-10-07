using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoalShoot
{
    /// <summary>Узел карты: название, открыт или закрыт, найденный уголь, лучшая оценка.</summary>
    public class LevelNodeView : MonoBehaviour
    {
        [SerializeField] LevelData level;
        [SerializeField] Button button;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text meta;
        [SerializeField] GameObject lockedMark;
        [SerializeField] GameObject clearedMark;

        public LevelData Level => level;

        void Awake()
        {
            if (button != null) button.onClick.AddListener(Play);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Play);
        }

        public void Refresh()
        {
            var prog = ProgressionService.Instance;
            bool unlocked = prog == null || prog.IsUnlocked(level);
            LevelRecord rec = prog != null ? prog.GetRecord(level) : null;

            if (button != null) button.interactable = unlocked;
            if (title != null) title.text = level.displayName;
            if (subtitle != null)
                subtitle.text = unlocked ? level.subtitle : (level.requires != null ? $"Сначала пройди «{level.requires.displayName}»" : "Закрыто");
            if (meta != null)
            {
                string grade = rec != null ? " · " + rec.grade : "";
                meta.text = level.isBoss
                    ? (rec != null ? "побеждён" + grade : $"награда {level.firstClearReward} угля")
                    : $"уголь {(prog != null ? prog.CoalFound(level) : 0)}/{level.coalCount}{grade}";
            }
            if (lockedMark != null) lockedMark.SetActive(!unlocked);
            if (clearedMark != null) clearedMark.SetActive(rec != null);
        }

        void Play()
        {
            var prog = ProgressionService.Instance;
            if (prog != null && !prog.IsUnlocked(level)) return;
            SceneManager.LoadScene(level.sceneName);
        }
    }
}
