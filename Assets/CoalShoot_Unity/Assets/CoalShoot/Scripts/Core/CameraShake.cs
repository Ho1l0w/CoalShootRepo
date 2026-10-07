using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Тряска на основе «травмы»: CameraShake.Add(0.3f). Сила растёт квадратично и сама затухает.
    /// Вешается на промежуточный объект между риг-объектом камеры и самой камерой:
    /// CameraRig (CameraFollow2D) → Shake (CameraShake) → Main Camera.
    /// Работает в unscaled time, поэтому трясёт и во время HitStop.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }
        public static bool Enabled = true;

        [SerializeField] float maxOffset = 0.35f;
        [SerializeField] float maxAngle = 2.5f;
        [SerializeField] float frequency = 22f;
        [SerializeField] float decayPerSecond = 1.8f;

        float trauma;
        float seed;

        void Awake()
        {
            Instance = this;
            seed = Random.value * 100f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Add(float amount)
        {
            if (Instance != null) Instance.trauma = Mathf.Clamp01(Instance.trauma + amount);
        }

        void LateUpdate()
        {
            trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.unscaledDeltaTime);
            float k = Enabled ? trauma * trauma : 0f;
            float t = Time.unscaledTime * frequency;
            float nx = Mathf.PerlinNoise(seed, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(seed + 10f, t) * 2f - 1f;
            float nr = Mathf.PerlinNoise(seed + 20f, t) * 2f - 1f;
            transform.localPosition = new Vector3(nx, ny, 0f) * (maxOffset * k);
            transform.localRotation = Quaternion.Euler(0f, 0f, nr * maxAngle * k);
        }
    }
}
