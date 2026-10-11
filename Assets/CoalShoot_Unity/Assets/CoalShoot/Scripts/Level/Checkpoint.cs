using UnityEngine;

namespace CoalShoot
{
    /// <summary>Флажок: запоминает точку возрождения и лечит игроков. Коллайдер-триггер.</summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] Transform spawnOverride;
        [Tooltip("Необязательно: триггер Activate")]
        [SerializeField] Animator animator;
        [SerializeField] GameObject activateFx;

        public bool Active { get; private set; }
        public Vector2 SpawnPoint => spawnOverride != null ? (Vector2)spawnOverride.position : (Vector2)transform.position;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Active || other.GetComponentInParent<PlayerController>() == null) return;
            Active = true;
            if (animator != null) animator.SetTrigger("Activate");
            FX.Spawn(activateFx, transform.position, 1.5f);
            CameraShake.Add(0.12f);
            other.GetComponentInParent<PlayerMotor2D>().LastSafePosition = transform.position;
            if (LevelManager.Instance != null) LevelManager.Instance.SetCheckpoint(this);
        }
    }
}
