using UnityEngine;

namespace CoalShoot
{
    /// <summary>Плавно гасит SpriteRenderer и удаляет объект. Используется для следа рывка.</summary>
    public class FadeAndDestroy : MonoBehaviour
    {
        SpriteRenderer sr;
        Color start;
        float life = 0.2f;
        float t;

        public void Init(float lifetime)
        {
            life = Mathf.Max(0.01f, lifetime);
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) start = sr.color;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                Color c = start;
                c.a = start.a * Mathf.Clamp01(1f - t / life);
                sr.color = c;
            }
            if (t >= life) Destroy(gameObject);
        }
    }
}
