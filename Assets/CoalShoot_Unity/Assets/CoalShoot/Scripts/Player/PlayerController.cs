using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Связующее звено игрока: берёт ввод и раздаёт его движению, прицелу и оружию.
    /// Применяет прогресс (двойной прыжок, амулеты, оружие в слотах). Превращает павшего игрока в призрака.
    /// Порядок -10, чтобы ввод попадал в мотор в том же физическом кадре.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(PlayerMotor2D), typeof(PlayerHealth), typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerAim), typeof(SuperMeter))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] int playerIndex;
        [Tooltip("Объект со спрайтами: прячется, пока игрок пал")]
        [SerializeField] GameObject visualRoot;
        [SerializeField] PlayerGhost ghostPrefab;

        public int Index => playerIndex;
        public PlayerMotor2D Motor { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerAim Aim { get; private set; }
        public SuperMeter Super { get; private set; }
        public WeaponController Weapons { get; private set; }
        public PlayerInputReader InputReader { get; private set; }
        public PlayerInputFrame LastInput { get; private set; }
        /// <summary>Ввод игнорируется: интро босса, финиш уровня, кат-сцены.</summary>
        public bool Locked { get; set; }
        public Vector2 Center => body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;

        Collider2D body;
        Rigidbody2D rb;
        PlayerGhost ghost;

        void Awake()
        {
            Motor = GetComponent<PlayerMotor2D>();
            Health = GetComponent<PlayerHealth>();
            Aim = GetComponent<PlayerAim>();
            Super = GetComponent<SuperMeter>();
            Weapons = GetComponent<WeaponController>();
            InputReader = GetComponent<PlayerInputReader>();
            body = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();
        }

        void OnEnable()
        {
            PlayerRegistry.Register(this);
            Health.Downed += OnDowned;
            Health.Revived += OnRevived;
        }

        void OnDisable()
        {
            PlayerRegistry.Unregister(this);
            Health.Downed -= OnDowned;
            Health.Revived -= OnRevived;
        }

        void Start() => ApplyLoadout();

        public void SetIndex(int index) => playerIndex = index;

        /// <summary>Перечитать прогресс: способности, амулеты, оружие в слотах. Вызывайте на старте уровня.</summary>
        public void ApplyLoadout()
        {
            var prog = ProgressionService.Instance;
            bool has = prog != null;
            Motor.AirJumpsMax = has && prog.HasAbility(Ability.DoubleJump) ? 1 : 0;
            Health.Configure(has && prog.IsCharmEquipped(CharmEffect.ExtraHeart) ? 1 : 0,
                             has && prog.IsCharmEquipped(CharmEffect.SmokeDash));
            Super.PassivePerSecond = has && prog.IsCharmEquipped(CharmEffect.Coffee) ? 0.17f : 0f;
            if (Weapons != null) Weapons.SetLoadout(has ? prog.GetEquippedWeapon(0) : null, has ? prog.GetEquippedWeapon(1) : null);
            Motor.RefreshAbilities();
        }

        /// <summary>Только способности, без сброса здоровья: после подбора двойного прыжка посреди уровня.</summary>
        public void RefreshAbilities()
        {
            var prog = ProgressionService.Instance;
            Motor.AirJumpsMax = prog != null && prog.HasAbility(Ability.DoubleJump) ? 1 : 0;
            Motor.RefreshAbilities();
        }

        void FixedUpdate()
        {
            PlayerInputFrame input = InputReader.Consume();
            LastInput = input;
            if (Health.IsDown) return;

            Motor.ControlLocked = Locked;
            Vector2 move = Locked ? Vector2.zero : input.Move;
            Aim.UpdateAim(move, input.AimLockHeld && !Locked, Motor.IsGrounded, Motor.Facing);
            Motor.SetInput(move.x, input.JumpHeld && !Locked, Aim.Locked);
            if (Locked) return;

            if (input.JumpPressed) Motor.RequestJump();
            if (input.DashPressed) Motor.RequestDash();
            if (Weapons != null) Weapons.Tick(input, Time.fixedDeltaTime);
        }

        void OnDowned()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
            rb.simulated = false;
            Motor.enabled = false;
            if (ghostPrefab != null)
            {
                ghost = Instantiate(ghostPrefab, Center, Quaternion.identity);
                ghost.Init(this);
            }
        }

        void OnRevived()
        {
            if (visualRoot != null) visualRoot.SetActive(true);
            rb.simulated = true;
            Motor.enabled = true;
            Motor.RefreshAbilities();
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
        }
    }
}
