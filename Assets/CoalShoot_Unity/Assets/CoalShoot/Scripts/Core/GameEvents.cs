using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Глобальные события для интерфейса и звука. Геймплей поднимает события и ничего не знает про UI.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Подсказка-плашка: заголовок и текст.</summary>
        public static event Action<string, string> Banner;
        /// <summary>Крупная надпись: «ГОТОВ?», «К БОЮ!», «НОКАУТ!».</summary>
        public static event Action<string> Announce;
        /// <summary>Здоровье босса от 0 до 1.</summary>
        public static event Action<float> BossHealthChanged;
        public static event Action<int> BossPhaseChanged;
        public static event Action<LevelResult> LevelFinished;

        public static void RaiseBanner(string title, string text) => Banner?.Invoke(title, text);
        public static void RaiseAnnounce(string text) => Announce?.Invoke(text);
        public static void RaiseBossHealth(float normalized) => BossHealthChanged?.Invoke(normalized);
        public static void RaiseBossPhase(int phase) => BossPhaseChanged?.Invoke(phase);
        public static void RaiseLevelFinished(LevelResult result) => LevelFinished?.Invoke(result);

        // Сброс статики, если в проекте отключена перезагрузка домена (Enter Play Mode Options).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Banner = null;
            Announce = null;
            BossHealthChanged = null;
            BossPhaseChanged = null;
            LevelFinished = null;
        }
    }
}
