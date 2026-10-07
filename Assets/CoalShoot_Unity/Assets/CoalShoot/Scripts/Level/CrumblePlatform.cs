using System.Collections;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Плита, которая дрожит и рассыпается через полсекунды после того, как на неё встали,
    /// а потом возвращается, когда на её месте никого нет.
    /// </summary>
    public class CrumblePlatform : MonoBehaviour
    {
        [Tooltip("32 кадра в прототипе")]
        [SerializeField] float delay = 0.53f;
        [SerializeField] float respawnAfter = 2.8f;
        [SerializeField] float shakeAmount = 0.06f;
        [SerializeField] Collider2D solid;
        [Tooltip("Дрожит только спрайт, коллайдер стоит на месте")]
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] LayerMask playerMask;
        [SerializeField] GameObject breakFx;
        [SerializeField] GameObject restoreFx;

        bool triggered;
        Vector3 spriteBase;

        void Awake()
        {
            if (solid == null) solid = GetComponent<Collider2D>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) spriteBase = sprite.transform.localPosition;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (triggered || collision.collider.GetComponentInParent<PlayerController>() == null) return;
            if (collision.collider.bounds.min.y >= solid.bounds.max.y - 0.1f) StartCoroutine(Crumble());
        }

        IEnumerator Crumble()
        {
            triggered = true;
            float t = 0f;
            while (t < delay)
            {
                t += Time.deltaTime;
                if (sprite != null) sprite.transform.localPosition = spriteBase + (Vector3)(Random.insideUnitCircle * shakeAmount);
                yield return null;
            }
            if (sprite != null) sprite.transform.localPosition = spriteBase;

            Bounds area = solid.bounds;
            FX.Spawn(breakFx, area.center, 1.5f);
            solid.enabled = false;
            if (sprite != null) sprite.enabled = false;

            yield return new WaitForSeconds(respawnAfter);
            while (Physics2D.OverlapBox(area.center, area.size, 0f, playerMask) != null) yield return null;

            solid.enabled = true;
            if (sprite != null) sprite.enabled = true;
            FX.Spawn(restoreFx, area.center, 1f);
            triggered = false;
        }
    }
}
