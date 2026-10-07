using UnityEngine;

namespace CoalShoot
{
    /// <summary>Описание уровня для карты и прогресса.</summary>
    [CreateAssetMenu(menuName = "CoalShoot/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Уникальный номер. Он же префикс id угля: 1-0, 1-1 …")]
        public int id = 1;
        public string displayName = "Шахта";
        [TextArea] public string subtitle = "Бег, прыжок, огонь, рывок";
        public string sceneName = "Level1";
        [Tooltip("Секунды, за которые оценка не снижается")]
        public float parTime = 70f;
        public int coalCount = 5;
        [Tooltip("Уголь за первое прохождение: 2 за уровень, 5 за босса")]
        public int firstClearReward = 2;
        public bool isBoss;
        [Tooltip("Уровень, который нужно пройти, чтобы открылся этот. Пусто — открыт сразу")]
        public LevelData requires;
    }
}
