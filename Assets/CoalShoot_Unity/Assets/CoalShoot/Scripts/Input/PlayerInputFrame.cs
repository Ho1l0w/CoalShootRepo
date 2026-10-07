using UnityEngine;

namespace CoalShoot
{
    /// <summary>Ввод игрока за один физический кадр.</summary>
    public struct PlayerInputFrame
    {
        /// <summary>Цифровое направление: каждая ось -1, 0 или 1.</summary>
        public Vector2 Move;
        public bool JumpHeld;
        public bool ShootHeld;
        public bool AimLockHeld;
        public bool JumpPressed;
        public bool DashPressed;
        public bool ExPressed;
        public bool SwapPressed;
    }

    public enum ControlScheme
    {
        /// <summary>Один игрок: стрелки и WASD, Пробел/Z прыжок, X/F огонь, C рывок, V/E супер, Q смена, Shift прицел.</summary>
        KeyboardSolo,
        /// <summary>Игрок 1 вдвоём: WASD, Пробел, F, C, E, Q, левый Shift.</summary>
        KeyboardLeft,
        /// <summary>Игрок 2 вдвоём: стрелки, Enter, /, ., «,», ', правый Shift (или цифровой блок).</summary>
        KeyboardRight,
        /// <summary>Только геймпад с номером gamepadIndex.</summary>
        Gamepad
    }
}
