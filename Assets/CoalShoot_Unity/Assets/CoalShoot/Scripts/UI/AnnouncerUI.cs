using System.Collections;
using TMPro;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Крупные надписи («ГОТОВ?», «К БОЮ!», «НОКАУТ!») с ударом по масштабу и подсказки-плашки.
    /// Работает в unscaled time, чтобы не замирать во время HitStop.
    /// </summary>
    public class AnnouncerUI : MonoBehaviour
    {
        [Header("Крупная надпись")]
        [SerializeField] TMP_Text bigText;
        [SerializeField] CanvasGroup bigGroup;
        [SerializeField] float bigDuration = 0.95f;

        [Header("Плашка-подсказка")]
        [SerializeField] TMP_Text bannerTitle;
        [SerializeField] TMP_Text bannerText;
        [SerializeField] CanvasGroup bannerGroup;
        [SerializeField] float bannerDuration = 4.2f;

        Coroutine bigRoutine;
        Coroutine bannerRoutine;

        void OnEnable()
        {
            GameEvents.Announce += ShowBig;
            GameEvents.Banner += ShowBanner;
            if (bigGroup != null) bigGroup.alpha = 0f;
            if (bannerGroup != null) bannerGroup.alpha = 0f;
        }

        void OnDisable()
        {
            GameEvents.Announce -= ShowBig;
            GameEvents.Banner -= ShowBanner;
        }

        void ShowBig(string text)
        {
            if (bigText == null) return;
            if (bigRoutine != null) StopCoroutine(bigRoutine);
            bigRoutine = StartCoroutine(Big(text));
        }

        IEnumerator Big(string text)
        {
            bigText.text = text;
            Transform t = bigText.transform;
            float time = 0f;
            while (time < bigDuration)
            {
                time += Time.unscaledDeltaTime;
                float k = time / bigDuration;
                float scale = k < 0.12f ? Mathf.Lerp(1.6f, 0.95f, k / 0.12f) : k < 0.2f ? Mathf.Lerp(0.95f, 1.05f, (k - 0.12f) / 0.08f) : 1f;
                t.localScale = Vector3.one * scale;
                if (bigGroup != null) bigGroup.alpha = k < 0.12f ? k / 0.12f : k > 0.8f ? (1f - k) / 0.2f : 1f;
                yield return null;
            }
            if (bigGroup != null) bigGroup.alpha = 0f;
        }

        void ShowBanner(string title, string text)
        {
            if (bannerGroup == null) return;
            if (bannerTitle != null) bannerTitle.text = title;
            if (bannerText != null) bannerText.text = text;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(Banner());
        }

        IEnumerator Banner()
        {
            float time = 0f;
            while (time < bannerDuration)
            {
                time += Time.unscaledDeltaTime;
                bannerGroup.alpha = Mathf.Min(1f, time / 0.25f, (bannerDuration - time) / 0.25f);
                yield return null;
            }
            bannerGroup.alpha = 0f;
        }
    }
}
