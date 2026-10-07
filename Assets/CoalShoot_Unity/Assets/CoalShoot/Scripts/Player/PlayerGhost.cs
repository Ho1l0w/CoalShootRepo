using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Призрак павшего игрока в кооперативе. Медленно поднимается; напарник поднимает его парированием.
    /// Если улетел за время жизни: на обычном уровне игрок вернётся к напарнику, в бою с боссом — нет.
    /// Префаб: спрайт + CircleCollider2D (isTrigger) на слое Parry + Parryable + этот скрипт.
    /// </summary>
    [RequireComponent(typeof(Parryable))]
    public class PlayerGhost : MonoBehaviour
    {
        [SerializeField] float riseSpeed = 1.8f;
        [SerializeField] float wobble = 0.6f;
        [SerializeField] float lifetime = 6f;
        [SerializeField] int reviveHp = 1;

        PlayerController owner;
        Parryable parryable;
        float t;

        public void Init(PlayerController ownerPlayer) => owner = ownerPlayer;

        void Awake()
        {
            parryable = GetComponent<Parryable>();
            parryable.Parried += OnParried;
        }

        void OnDestroy()
        {
            if (parryable != null) parryable.Parried -= OnParried;
        }

        void OnParried(PlayerController by)
        {
            if (owner != null && owner.Health.IsDown)
            {
                owner.Health.Revive(reviveHp, transform.position);
                GameEvents.RaiseBanner("ДРУГ В СТРОЮ", "Одно сердце. Береги его.");
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += new Vector3(Mathf.Sin(t * 3f) * wobble * Time.deltaTime, riseSpeed * Time.deltaTime, 0f);
            if (t < lifetime) return;
            if (owner != null && LevelManager.Instance != null) LevelManager.Instance.OnGhostExpired(owner);
            Destroy(gameObject);
        }
    }
}
