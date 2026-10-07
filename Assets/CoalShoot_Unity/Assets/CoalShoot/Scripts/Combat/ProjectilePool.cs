using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace CoalShoot
{
    /// <summary>
    /// Пул снарядов по префабу. В WebGL это важно: сотни Instantiate/Destroy в секунду дают рывки сборщика мусора.
    /// При выгрузке сцены пулы очищаются (объекты сцены уничтожаются вместе с ней).
    /// </summary>
    public static class ProjectilePool
    {
        static readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new Dictionary<Projectile, ObjectPool<Projectile>>();
        static readonly Dictionary<Projectile, Projectile> prefabOf = new Dictionary<Projectile, Projectile>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            pools.Clear();
            prefabOf.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneUnloaded(Scene scene)
        {
            pools.Clear();
            prefabOf.Clear();
        }

        public static Projectile Get(Projectile prefab)
        {
            if (!pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<Projectile>(
                    () => Object.Instantiate(prefab),
                    p => p.gameObject.SetActive(true),
                    p => p.gameObject.SetActive(false),
                    p => { if (p != null) Object.Destroy(p.gameObject); },
                    false, 32, 512);
                pools[prefab] = pool;
            }

            Projectile instance = pool.Get();
            prefabOf[instance] = prefab;
            return instance;
        }

        public static void Release(Projectile p)
        {
            if (p == null) return;
            if (prefabOf.TryGetValue(p, out var prefab) && pools.TryGetValue(prefab, out var pool)) pool.Release(p);
            else Object.Destroy(p.gameObject);
        }
    }
}
