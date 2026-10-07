using System.Collections.Generic;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>Список игроков на сцене: для прицеливания врагов, камеры и босса.</summary>
    public static class PlayerRegistry
    {
        static readonly List<PlayerController> all = new List<PlayerController>();
        static readonly List<PlayerController> alive = new List<PlayerController>();

        public static IReadOnlyList<PlayerController> All => all;

        public static IReadOnlyList<PlayerController> Alive
        {
            get
            {
                alive.Clear();
                foreach (var p in all) if (p != null && !p.Health.IsDown) alive.Add(p);
                return alive;
            }
        }

        public static void Register(PlayerController p)
        {
            if (!all.Contains(p)) all.Add(p);
        }

        public static void Unregister(PlayerController p) => all.Remove(p);

        /// <summary>Ближайший живой игрок в радиусе или null.</summary>
        public static PlayerController Nearest(Vector2 from, float range)
        {
            PlayerController best = null;
            float bestSqr = range * range;
            foreach (var p in all)
            {
                if (p == null || p.Health.IsDown) continue;
                float d = (p.Center - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = p; }
            }
            return best;
        }

        public static PlayerController RandomAlive()
        {
            var list = Alive;
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            alive.Clear();
        }
    }
}
