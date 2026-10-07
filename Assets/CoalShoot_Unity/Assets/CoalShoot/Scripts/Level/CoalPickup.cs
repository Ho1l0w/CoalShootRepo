using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Кусок угля — валюта лавки. Каждый подбирается один раз за всю игру (сохраняется по id).
    /// Уже собранный показывается полупрозрачным и ничего не даёт. Коллайдер-триггер.
    /// </summary>
    public class CoalPickup : MonoBehaviour
    {
        [Tooltip("Уникальный id: номер уровня и номер куска, например 1-0, 1-1 … 1-4")]
        [SerializeField] string id = "1-0";
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Color collectedTint = new Color(0.84f, 0.75f, 0.58f, 0.6f);
        [SerializeField] GameObject collectFx;
        [SerializeField] float bobAmplitude = 0.15f;
        [SerializeField] float bobSpeed = 4.2f;

        public string Id => id;

        bool alreadyCollected;
        Vector3 basePosition;

        void Start()
        {
            basePosition = transform.position;
            var prog = ProgressionService.Instance;
            alreadyCollected = prog != null && prog.IsCollected(id);
            if (alreadyCollected && sprite != null) sprite.color = collectedTint;
        }

        void Update()
        {
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + basePosition.x) * bobAmplitude);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            var prog = ProgressionService.Instance;
            if (!alreadyCollected && prog != null)
            {
                prog.CollectCoal(id);
                HitStop.Freeze(0.05f);
                CameraShake.Add(0.15f);
            }
            FX.Spawn(collectFx, transform.position, 1.5f);
            Destroy(gameObject);
        }
    }
}
