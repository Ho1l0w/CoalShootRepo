using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Скорость Rigidbody2D одинаково для Unity 2022 (velocity) и Unity 6 (linearVelocity).
    /// </summary>
    public static class Rb2DExtensions
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }
}
