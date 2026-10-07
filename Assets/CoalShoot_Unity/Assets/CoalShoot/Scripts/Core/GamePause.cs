using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Пауза игры: меню, реклама, уход со вкладки. Останавливает время и звук.
    /// HitStop учитывает паузу и не «размораживает» игру во время рекламы.
    /// Для Яндекс Игр: SetPaused(true) при открытии рекламы и потере фокуса, SetPaused(false) после.
    /// </summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsPaused = false;
    }
}
