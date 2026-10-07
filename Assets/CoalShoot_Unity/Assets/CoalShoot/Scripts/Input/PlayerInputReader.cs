using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CoalShoot
{
    /// <summary>
    /// Читает клавиатуру и геймпад без ассета Input Actions.
    /// Работает и с новым Input System (если пакет установлен и включён), и со старым Input Manager.
    ///
    /// Нажатия копятся в Update и забираются один раз за FixedUpdate через Consume(),
    /// поэтому короткое нажатие между физическими кадрами не теряется.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public ControlScheme scheme = ControlScheme.KeyboardSolo;
        [Tooltip("Номер геймпада: 0 — первый подключённый")]
        public int gamepadIndex = 0;
        [Tooltip("Читать геймпад с номером gamepadIndex вместе с клавиатурой")]
        public bool alsoReadGamepad = true;
        [Tooltip("Выключите, чтобы игнорировать ввод (меню, пауза)")]
        public bool inputEnabled = true;

        enum A { Left, Right, Up, Down, Jump, Shoot, Dash, Ex, Swap, AimLock }

        PlayerInputFrame frame;
        bool jumpPressed, dashPressed, exPressed, swapPressed;

        void Update()
        {
            if (!inputEnabled)
            {
                frame = default;
                return;
            }

            bool left = Held(A.Left), right = Held(A.Right), up = Held(A.Up), down = Held(A.Down);
            bool jump = Held(A.Jump), shoot = Held(A.Shoot), aimLock = Held(A.AimLock);
            bool jp = Pressed(A.Jump), dp = Pressed(A.Dash), ep = Pressed(A.Ex), sp = Pressed(A.Swap);

            ReadGamepad(ref left, ref right, ref up, ref down, ref jump, ref shoot, ref aimLock, ref jp, ref dp, ref ep, ref sp);

            frame.Move = new Vector2((right ? 1f : 0f) - (left ? 1f : 0f), (up ? 1f : 0f) - (down ? 1f : 0f));
            frame.JumpHeld = jump;
            frame.ShootHeld = shoot;
            frame.AimLockHeld = aimLock;
            jumpPressed |= jp;
            dashPressed |= dp;
            exPressed |= ep;
            swapPressed |= sp;
        }

        /// <summary>Вызывайте один раз за FixedUpdate: возвращает удержания и все нажатия с прошлого вызова.</summary>
        public PlayerInputFrame Consume()
        {
            PlayerInputFrame f = frame;
            f.JumpPressed = jumpPressed;
            f.DashPressed = dashPressed;
            f.ExPressed = exPressed;
            f.SwapPressed = swapPressed;
            jumpPressed = dashPressed = exPressed = swapPressed = false;
            return f;
        }

#if ENABLE_INPUT_SYSTEM
        static readonly Key[][] Solo =
        {
            new[] { Key.LeftArrow, Key.A }, new[] { Key.RightArrow, Key.D }, new[] { Key.UpArrow, Key.W }, new[] { Key.DownArrow, Key.S },
            new[] { Key.Space, Key.Z, Key.K }, new[] { Key.X, Key.F, Key.J }, new[] { Key.C, Key.L }, new[] { Key.V, Key.E },
            new[] { Key.Q, Key.Tab }, new[] { Key.LeftShift, Key.RightShift }
        };
        static readonly Key[][] LeftHalf =
        {
            new[] { Key.A }, new[] { Key.D }, new[] { Key.W }, new[] { Key.S },
            new[] { Key.Space }, new[] { Key.F }, new[] { Key.C }, new[] { Key.E },
            new[] { Key.Q }, new[] { Key.LeftShift }
        };
        static readonly Key[][] RightHalf =
        {
            new[] { Key.LeftArrow }, new[] { Key.RightArrow }, new[] { Key.UpArrow }, new[] { Key.DownArrow },
            new[] { Key.Enter, Key.NumpadEnter }, new[] { Key.Slash, Key.Numpad1 }, new[] { Key.Period, Key.Numpad2 }, new[] { Key.Comma, Key.Numpad3 },
            new[] { Key.Quote, Key.Numpad5 }, new[] { Key.RightShift, Key.Numpad4 }
        };
        static readonly Key[][] NoKeys =
        {
            new Key[0], new Key[0], new Key[0], new Key[0], new Key[0], new Key[0], new Key[0], new Key[0], new Key[0], new Key[0]
        };

        Key[][] Table
        {
            get
            {
                switch (scheme)
                {
                    case ControlScheme.KeyboardSolo: return Solo;
                    case ControlScheme.KeyboardLeft: return LeftHalf;
                    case ControlScheme.KeyboardRight: return RightHalf;
                    default: return NoKeys;
                }
            }
        }

        bool Held(A action)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in Table[(int)action]) if (kb[k].isPressed) return true;
            return false;
        }

        bool Pressed(A action)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in Table[(int)action]) if (kb[k].wasPressedThisFrame) return true;
            return false;
        }

        void ReadGamepad(ref bool l, ref bool r, ref bool u, ref bool d, ref bool jump, ref bool shoot, ref bool aimLock,
                         ref bool jp, ref bool dp, ref bool ep, ref bool sp)
        {
            if (scheme != ControlScheme.Gamepad && !alsoReadGamepad) return;
            if (gamepadIndex < 0 || gamepadIndex >= Gamepad.all.Count) return;
            Gamepad pad = Gamepad.all[gamepadIndex];

            Vector2 stick = pad.leftStick.ReadValue();
            Vector2 dpad = pad.dpad.ReadValue();
            l |= stick.x < -0.4f || dpad.x < -0.5f;
            r |= stick.x > 0.4f || dpad.x > 0.5f;
            u |= stick.y > 0.5f || dpad.y > 0.5f;
            d |= stick.y < -0.5f || dpad.y < -0.5f;

            jump |= pad.buttonSouth.isPressed;
            shoot |= pad.buttonWest.isPressed;
            aimLock |= pad.leftTrigger.isPressed || pad.rightTrigger.isPressed;
            jp |= pad.buttonSouth.wasPressedThisFrame;
            dp |= pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
            ep |= pad.buttonEast.wasPressedThisFrame;
            sp |= pad.buttonNorth.wasPressedThisFrame;
        }
#else
        static readonly KeyCode[][] Solo =
        {
            new[] { KeyCode.LeftArrow, KeyCode.A }, new[] { KeyCode.RightArrow, KeyCode.D }, new[] { KeyCode.UpArrow, KeyCode.W }, new[] { KeyCode.DownArrow, KeyCode.S },
            new[] { KeyCode.Space, KeyCode.Z, KeyCode.K }, new[] { KeyCode.X, KeyCode.F, KeyCode.J }, new[] { KeyCode.C, KeyCode.L }, new[] { KeyCode.V, KeyCode.E },
            new[] { KeyCode.Q, KeyCode.Tab }, new[] { KeyCode.LeftShift, KeyCode.RightShift }
        };
        static readonly KeyCode[][] LeftHalf =
        {
            new[] { KeyCode.A }, new[] { KeyCode.D }, new[] { KeyCode.W }, new[] { KeyCode.S },
            new[] { KeyCode.Space }, new[] { KeyCode.F }, new[] { KeyCode.C }, new[] { KeyCode.E },
            new[] { KeyCode.Q }, new[] { KeyCode.LeftShift }
        };
        static readonly KeyCode[][] RightHalf =
        {
            new[] { KeyCode.LeftArrow }, new[] { KeyCode.RightArrow }, new[] { KeyCode.UpArrow }, new[] { KeyCode.DownArrow },
            new[] { KeyCode.Return, KeyCode.KeypadEnter }, new[] { KeyCode.Slash, KeyCode.Keypad1 }, new[] { KeyCode.Period, KeyCode.Keypad2 }, new[] { KeyCode.Comma, KeyCode.Keypad3 },
            new[] { KeyCode.Quote, KeyCode.Keypad5 }, new[] { KeyCode.RightShift, KeyCode.Keypad4 }
        };
        static readonly KeyCode[][] NoKeys =
        {
            new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0], new KeyCode[0]
        };

        KeyCode[][] Table
        {
            get
            {
                switch (scheme)
                {
                    case ControlScheme.KeyboardSolo: return Solo;
                    case ControlScheme.KeyboardLeft: return LeftHalf;
                    case ControlScheme.KeyboardRight: return RightHalf;
                    default: return NoKeys;
                }
            }
        }

        bool Held(A action)
        {
            foreach (var k in Table[(int)action]) if (Input.GetKey(k)) return true;
            return false;
        }

        bool Pressed(A action)
        {
            foreach (var k in Table[(int)action]) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        // Старый Input Manager: кнопки геймпада как JoystickNButtonM (раскладка Xbox: 0 A, 1 B, 2 X, 3 Y, 4 LB, 5 RB).
        // Оси стика читаются только для первого геймпада через стандартные оси Horizontal/Vertical.
        void ReadGamepad(ref bool l, ref bool r, ref bool u, ref bool d, ref bool jump, ref bool shoot, ref bool aimLock,
                         ref bool jp, ref bool dp, ref bool ep, ref bool sp)
        {
            if (scheme != ControlScheme.Gamepad && !alsoReadGamepad) return;
            if (gamepadIndex < 0 || gamepadIndex > 7) return;
            int b0 = (int)KeyCode.Joystick1Button0 + gamepadIndex * 20;
            KeyCode B(int n) => (KeyCode)(b0 + n);

            if (gamepadIndex == 0 && (scheme == ControlScheme.Gamepad || scheme == ControlScheme.KeyboardSolo))
            {
                float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
                l |= h < -0.4f; r |= h > 0.4f; u |= v > 0.5f; d |= v < -0.5f;
            }
            jump |= Input.GetKey(B(0));
            shoot |= Input.GetKey(B(2));
            jp |= Input.GetKeyDown(B(0));
            dp |= Input.GetKeyDown(B(4)) || Input.GetKeyDown(B(5));
            ep |= Input.GetKeyDown(B(1));
            sp |= Input.GetKeyDown(B(3));
        }
#endif
    }
}
