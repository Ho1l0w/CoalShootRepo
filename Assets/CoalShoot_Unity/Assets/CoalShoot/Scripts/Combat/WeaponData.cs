using UnityEngine;

namespace CoalShoot
{
    public enum FireMode
    {
        /// <summary>Перо: быстрые одиночные выстрелы.</summary>
        Straight,
        /// <summary>Клякстрел: веер дробин вблизи.</summary>
        Spread,
        /// <summary>Искра: самонаведение.</summary>
        Homing,
        /// <summary>Уголёк: дуга с отскоком от пола.</summary>
        Lob,
        /// <summary>Заряд: держи огонь, отпусти — выстрел.</summary>
        Charge
    }

    public enum ExMode
    {
        PiercingStar,
        Radial,
        HomingSwarm,
        BigLob,
        Lance
    }

    /// <summary>
    /// Оружие как ассет: создайте пять штук через Create → CoalShoot → Weapon.
    /// Значения по умолчанию — Перо. Таблица для остальных — в README.
    /// </summary>
    [CreateAssetMenu(menuName = "CoalShoot/Weapon", fileName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Лавка")]
        public string id = "pen";
        public string displayName = "Перо";
        [TextArea] public string description = "Быстрые точные выстрелы. Супер: пробивающая звезда.";
        public int price = 0;
        public Sprite icon;

        [Header("Обычный огонь")]
        public FireMode mode = FireMode.Straight;
        public Projectile projectile;
        [Tooltip("Секунд между выстрелами")]
        public float fireInterval = 0.133f;
        public float speed = 39f;
        public float damage = 1f;
        public float lifetime = 0.92f;
        [Tooltip("Spread: число дробин")]
        public int pellets = 3;
        [Tooltip("Spread: крайняя дробина отклоняется на столько градусов")]
        public float spreadDegrees = 12.6f;
        [Tooltip("Homing: скорость поворота, градусов в секунду")]
        public float homingTurnDegPerSec = 450f;
        [Tooltip("Lob: добавка скорости вверх")]
        public float lobUpSpeed = 14.4f;
        [Tooltip("Lob: гравитация снаряда")]
        public float gravity = 72f;
        [Tooltip("Lob: отскоков от пола")]
        public int bounces = 1;
        [Tooltip("Отдача по горизонтали за выстрел")]
        public float recoil = 0f;
        public float shake = 0.025f;

        [Header("Заряд (FireMode.Charge)")]
        public float chargeTime = 0.67f;
        public Projectile chargedProjectile;
        public float chargedDamage = 6f;
        public float chargedSpeed = 42f;

        [Header("Супер (одна карта)")]
        public ExMode exMode = ExMode.PiercingStar;
        public Projectile exProjectile;
        public float exDamage = 2.2f;
        public float exSpeed = 25f;
        public int exCount = 1;
        public float exLifetime = 1.6f;
    }
}
