using UnityEngine;

namespace CoalShoot
{
    /// <summary>Дверь в конце уровня. Коллайдер-триггер.</summary>
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] GameObject reachedFx;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (LevelManager.Instance == null || LevelManager.Instance.Finished) return;
            FX.Spawn(reachedFx, transform.position, 2f);
            CameraShake.Add(0.35f);
            LevelManager.Instance.CompleteLevel();
        }
    }
}
