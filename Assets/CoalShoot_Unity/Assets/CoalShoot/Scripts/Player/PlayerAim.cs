using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Прицел в 8 направлениях. На земле «вниз» не стреляет (кроме режима «стоять и целиться»),
    /// в воздухе можно стрелять вниз и по диагонали вниз.
    /// </summary>
    public class PlayerAim : MonoBehaviour
    {
        public Vector2 Direction { get; private set; } = Vector2.right;
        public float AngleDegrees => Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        /// <summary>Стоит на месте и целится (держит Shift или курок).</summary>
        public bool Locked { get; private set; }

        public void UpdateAim(Vector2 move, bool aimLockHeld, bool grounded, int facing)
        {
            Locked = aimLockHeld && grounded;
            float x = Mathf.Abs(move.x) > 0.5f ? Mathf.Sign(move.x) : 0f;
            float y = Mathf.Abs(move.y) > 0.5f ? Mathf.Sign(move.y) : 0f;
            if (grounded && !Locked && y < 0f) y = 0f;
            if (x == 0f && y == 0f) x = facing;
            Direction = new Vector2(x, y).normalized;
        }
    }
}
