using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Одноразовые эффекты: пыль, искры, вспышки. Префаб может быть null — тогда ничего не происходит,
    /// поэтому эффекты можно подключать постепенно. Для ParticleSystem поставьте Stop Action = Destroy
    /// или оставьте autoDestroy.
    /// </summary>
    public static class FX
    {
        public static GameObject Spawn(GameObject prefab, Vector3 position, float autoDestroy = 3f)
        {
            return Spawn(prefab, position, Quaternion.identity, autoDestroy);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float autoDestroy = 3f)
        {
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, position, rotation);
            if (autoDestroy > 0f) Object.Destroy(go, autoDestroy);
            return go;
        }
    }
}
