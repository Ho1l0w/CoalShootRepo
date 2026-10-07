using UnityEngine;

namespace CoalShoot
{
    public enum CharmEffect
    {
        /// <summary>Четыре сердца вместо трёх.</summary>
        ExtraHeart,
        /// <summary>Во время рывка игрока нельзя задеть.</summary>
        SmokeDash,
        /// <summary>Супер-шкала растёт сама.</summary>
        Coffee
    }

    /// <summary>Амулет из лавки. Надет может быть только один.</summary>
    [CreateAssetMenu(menuName = "CoalShoot/Charm", fileName = "Charm")]
    public class CharmData : ScriptableObject
    {
        public string id = "heart";
        public string displayName = "Лишнее сердце";
        [TextArea] public string description = "Четыре сердца вместо трёх.";
        public int price = 3;
        public CharmEffect effect = CharmEffect.ExtraHeart;
        public Sprite icon;
    }
}
