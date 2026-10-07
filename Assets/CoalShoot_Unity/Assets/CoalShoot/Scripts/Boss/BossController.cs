using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CoalShoot
{
    [Serializable]
    public class BossPhase
    {
        public string name = "Фаза";
        [Tooltip("Фаза заканчивается, когда доля здоровья опускается до этого значения. У последней фазы — 0")]
        [Range(0f, 1f)] public float endsAtHealth = 0.666f;
        [Tooltip("Атаки фазы. Одна и та же не повторяется дважды подряд")]
        public BossAttack[] attacks;
        public float pauseBetweenAttacks = 0.9f;
        public UnityEvent onEnter;
    }

    /// <summary>
    /// Босс в духе Cuphead: интро «ГОТОВ? К БОЮ!», фазы по порогам здоровья, случайные атаки без повтора,
    /// переход между фазами со стоп-кадром и очисткой арены, нокаут.
    /// На том же объекте: Health (Destroy On Death выключен, Team = Enemy), коллайдер на слое Enemy,
    /// дочерний ContactDamage на слое Hazard. Атаки — компоненты BossAttack на дочерних объектах.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] BossPhase[] phases;
        [SerializeField] float introReady = 1.0f;
        [SerializeField] float introGo = 0.25f;
        [SerializeField] float firstAttackDelay = 0.8f;
        [SerializeField] float phaseTransition = 1.4f;
        [SerializeField] GameObject phaseFx;
        [SerializeField] GameObject knockoutFx;
        [Tooltip("Необязательно: параметр Phase (int), триггер Knockout")]
        [SerializeField] Animator animator;

        public Health Health => health;
        public int PhaseIndex { get; private set; }
        public bool Fighting { get; private set; }

        public event Action<int> PhaseChanged;
        public event Action Knockout;

        readonly List<GameObject> spawned = new List<GameObject>();
        Coroutine loopRoutine;
        Coroutine attackRoutine;
        BossAttack current;
        BossAttack last;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        IEnumerator Start()
        {
            health.Invulnerable = true;
            yield return null; // игроки создаются в Start у LevelManager
            LockPlayers(true);
            GameEvents.RaiseBossHealth(1f);
            GameEvents.RaiseAnnounce("ГОТОВ?");
            yield return new WaitForSeconds(introReady);
            GameEvents.RaiseAnnounce("К БОЮ!");
            CameraShake.Add(0.35f);
            HitStop.Freeze(0.1f);
            yield return new WaitForSeconds(introGo);
            LockPlayers(false);
            health.Invulnerable = false;
            Fighting = true;
            if (phases.Length > 0) phases[0].onEnter?.Invoke();
            loopRoutine = StartCoroutine(AttackLoop(firstAttackDelay));
        }

        IEnumerator AttackLoop(float delay)
        {
            yield return new WaitForSeconds(delay);
            while (Fighting && phases.Length > 0)
            {
                BossPhase phase = phases[PhaseIndex];
                current = Pick(phase.attacks);
                if (current != null)
                {
                    attackRoutine = StartCoroutine(current.Run(this));
                    yield return attackRoutine;
                    attackRoutine = null;
                }
                current = null;
                yield return new WaitForSeconds(phase.pauseBetweenAttacks);
            }
        }

        BossAttack Pick(BossAttack[] list)
        {
            if (list == null || list.Length == 0) return null;
            if (list.Length == 1) return list[0];
            BossAttack a;
            int guard = 0;
            do a = list[UnityEngine.Random.Range(0, list.Length)];
            while (a == last && guard++ < 10);
            last = a;
            return a;
        }

        void OnDamaged(DamageInfo info)
        {
            GameEvents.RaiseBossHealth(health.Normalized);
            if (!Fighting || health.Current <= 0f) return;
            int next = PhaseIndex;
            while (next < phases.Length - 1 && health.Normalized <= phases[next].endsAtHealth) next++;
            if (next != PhaseIndex) StartCoroutine(ChangePhase(next));
        }

        IEnumerator ChangePhase(int next)
        {
            Fighting = false;
            StopAttacks();
            health.Invulnerable = true;
            ClearArena();
            PhaseIndex = next;

            HitStop.Freeze(0.2f);
            CameraShake.Add(0.8f);
            FX.Spawn(phaseFx, transform.position, 3f);
            if (animator != null) animator.SetInteger("Phase", next);
            phases[next].onEnter?.Invoke();
            PhaseChanged?.Invoke(next);
            GameEvents.RaiseBossPhase(next);

            yield return new WaitForSeconds(phaseTransition);
            health.Invulnerable = false;
            Fighting = true;
            loopRoutine = StartCoroutine(AttackLoop(0.4f));
        }

        void OnDied()
        {
            Fighting = false;
            StopAttacks();
            ClearArena();
            HitStop.Freeze(0.5f);
            CameraShake.Add(1f);
            FX.Spawn(knockoutFx, transform.position, 4f);
            GameEvents.RaiseBossHealth(0f);
            GameEvents.RaiseAnnounce("НОКАУТ!");
            if (animator != null) animator.SetTrigger("Knockout");
            Knockout?.Invoke();
        }

        void StopAttacks()
        {
            if (loopRoutine != null) StopCoroutine(loopRoutine);
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            loopRoutine = null;
            attackRoutine = null;
            if (current != null) current.Cancel(this);
            current = null;
        }

        /// <summary>Атаки регистрируют созданное (волны, миньоны), чтобы смена фазы их убрала.</summary>
        public void Track(GameObject go)
        {
            if (go != null) spawned.Add(go);
        }

        public void ClearArena()
        {
            Projectile.DespawnTeam(Team.Enemy);
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
        }

        static void LockPlayers(bool locked)
        {
            foreach (var p in PlayerRegistry.All) if (p != null) p.Locked = locked;
        }
    }
}
