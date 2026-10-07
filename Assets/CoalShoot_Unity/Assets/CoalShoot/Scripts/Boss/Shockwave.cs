using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Ударная волна по полу. Префаб: спрайт + BoxCollider2D (isTrigger) на слое Hazard + ContactDamage
    /// (Stompable выключен, Health пусто) + этот скрипт.
    /// </summary>
    public class Shockwave : MonoBehaviour
    {
        [SerializeField] float lifetime = 1.35f;

        int dir = 1;
        float speed;

        public void Launch(int direction, float moveSpeed)
        {
            dir = direction >= 0 ? 1 : -1;
            speed = moveSpeed;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            transform.position += Vector3.right * (dir * speed * Time.deltaTime);
        }
    }
}
