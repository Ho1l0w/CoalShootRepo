using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Короткая заморозка времени при ударе: HitStop.Freeze(0.05f).
    /// В прототипе 1 кадр = 1/60 с: 3 кадра = 0.05 с, 8 кадров = 0.13 с.
    /// Перекрывающиеся вызовы не складываются, берётся самая поздняя точка окончания.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        static HitStop instance;

        float freezeUntil;
        bool running;
        float savedScale = 1f;

        /// <summary>Глобальный выключатель, например для настроек доступности.</summary>
        public static bool Enabled = true;

        static HitStop Get()
        {
            if (instance == null)
            {
                var go = new GameObject("[HitStop]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<HitStop>();
            }
            return instance;
        }

        public static void Freeze(float seconds)
        {
            if (!Enabled || seconds <= 0f || GamePause.IsPaused) return;
            var h = Get();
            h.freezeUntil = Mathf.Max(h.freezeUntil, Time.unscaledTime + seconds);
            if (!h.running) h.StartCoroutine(h.Run());
        }

        IEnumerator Run()
        {
            running = true;
            savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            while (Time.unscaledTime < freezeUntil) yield return null;
            Time.timeScale = GamePause.IsPaused ? 0f : savedScale;
            running = false;
        }

        void OnDisable()
        {
            if (!running) return;
            Time.timeScale = GamePause.IsPaused ? 0f : savedScale;
            running = false;
        }
    }
}
