using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Все числа «ощущения» управления в одном ассете. Значения по умолчанию пересчитаны из браузерного прототипа
    /// при масштабе 1 тайл = 1 юнит (персонаж примерно 1.1 юнита ростом).
    /// Прыжок задаётся высотой и временем до верхней точки, гравитация и скорость прыжка считаются из них.
    /// </summary>
    [CreateAssetMenu(menuName = "CoalShoot/Movement Config", fileName = "MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Бег")]
        public float runSpeed = 11.4f;
        [Tooltip("Разгон на земле, юнит/с²")]
        public float groundAccel = 216f;
        [Tooltip("Торможение на земле без ввода")]
        public float groundDecel = 270f;
        [Tooltip("Управление в воздухе")]
        public float airAccel = 122f;
        [Tooltip("Множитель разгона при развороте: персонаж разворачивается резче")]
        public float turnBoost = 1.7f;

        [Header("Прыжок")]
        public float jumpHeight = 4f;
        public float timeToApex = 0.255f;
        [Tooltip("Падение тяжелее подъёма")]
        public float fallGravityMult = 1.47f;
        [Tooltip("Зависание в верхней точке, пока держишь прыжок")]
        public float apexGravityMult = 0.5f;
        [Tooltip("Ниже этой вертикальной скорости считаем, что персонаж у верхней точки")]
        public float apexThreshold = 6.6f;
        public float maxFallSpeed = 42f;
        [Tooltip("Отпустил прыжок на подъёме — скорость вверх умножается на это число")]
        [Range(0f, 1f)] public float jumpCutMult = 0.45f;

        [Header("Прощение ошибок")]
        [Tooltip("Coyote time: сколько секунд после схода с края прыжок ещё срабатывает")]
        public float coyoteTime = 0.17f;
        [Tooltip("Буфер прыжка: нажатие до приземления запоминается на столько секунд")]
        public float jumpBuffer = 0.2f;
        [Tooltip("Если задел потолок углом, персонаж соскользнёт вбок не дальше этого")]
        public float cornerCorrection = 0.3f;

        [Header("Двойной прыжок")]
        public float airJumpHeight = 3.2f;

        [Header("Рывок")]
        public float dashSpeed = 31f;
        public float dashDuration = 0.18f;
        public float dashBuffer = 0.17f;
        [Tooltip("Скорость сразу после рывка")]
        public float dashExitSpeed = 13f;

        [Header("Планирование (Клякса)")]
        public bool canGlide;
        public float glideFallSpeed = 7.2f;

        [Header("Отбрасывание при уроне")]
        public float knockbackSpeed = 13f;
        public float knockbackUp = 18f;
        public float knockbackTime = 0.23f;

        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
        public float JumpVelocity => 2f * jumpHeight / timeToApex;
        public float AirJumpVelocity => Mathf.Sqrt(2f * Gravity * airJumpHeight);
    }
}
