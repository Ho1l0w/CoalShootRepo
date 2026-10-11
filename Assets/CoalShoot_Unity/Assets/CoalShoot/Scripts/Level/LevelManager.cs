using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoalShoot
{
    /// <summary>
    /// Управляет уровнем: создаёт 1–2 игроков, хранит чекпоинт, считает время, урон и парирования,
    /// возрождает всех у чекпоинта, если пали оба, и выдаёт итог (оценку и награду) через GameEvents.LevelFinished.
    /// На уровне с боссом: поражение, если пали все; победа — по нокауту.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] LevelData level;
        [SerializeField] PlayerController squarePrefab;
        [SerializeField] PlayerController blobPrefab;
        [SerializeField] Transform spawnPoint;
        [Tooltip("Заполните только на сцене босса")]
        [SerializeField] BossController boss;
        [SerializeField] float respawnDelay = 1.3f;
        [SerializeField] float resultDelay = 0.9f;
        [SerializeField] float bossResultDelay = 2.8f;
        [SerializeField] string mapScene = "Map";

        public LevelData Level => level;
        public float ElapsedTime { get; private set; }
        public int HitsTaken { get; private set; }
        public int Parries { get; private set; }
        public int Deaths { get; private set; }
        public bool Finished { get; private set; }
        public IReadOnlyList<PlayerController> Players => players;

        readonly List<PlayerController> players = new List<PlayerController>();
        public Vector2 respawnPoint;

        void Awake()
        {
            Instance = this;
            GamePause.SetPaused(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            respawnPoint = spawnPoint != null ? (Vector2)spawnPoint.position : Vector2.zero;
            int count = Mathf.Clamp(GameSettings.PlayerCount, 1, 2);
            for (int i = 0; i < count; i++) SpawnPlayer(i, count);
            if (boss != null) boss.Knockout += OnBossKnockout;
        }

        void SpawnPlayer(int index, int count)
        {
            bool p1Square = GameSettings.Player1Character == CharacterId.Square;
            PlayerController prefab = (index == 0) == p1Square ? squarePrefab : blobPrefab;
            if (prefab == null) prefab = squarePrefab != null ? squarePrefab : blobPrefab;
            if (prefab == null)
            {
                Debug.LogError("LevelManager: не назначен префаб игрока");
                return;
            }

            PlayerController p = Instantiate(prefab, respawnPoint + Vector2.right * (index * 1.2f), Quaternion.identity);
            p.SetIndex(index);
            var input = p.GetComponent<PlayerInputReader>();
            input.scheme = count == 1 ? ControlScheme.KeyboardSolo : (index == 0 ? ControlScheme.KeyboardLeft : ControlScheme.KeyboardRight);
            input.gamepadIndex = index;
            input.alsoReadGamepad = true;

            p.Health.Hurt += () => HitsTaken++;
            p.Health.Downed += OnPlayerDowned;
            var parry = p.GetComponent<PlayerParry>();
            if (parry != null) parry.Parried += () => Parries++;
            players.Add(p);
        }

        void Update()
        {
            if (Finished) return;
            if (boss == null || boss.Fighting) ElapsedTime += Time.deltaTime;
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            respawnPoint = checkpoint.SpawnPoint;
            foreach (var p in players) p.Health.HealFull();
        }

        void OnPlayerDowned()
        {
            foreach (var p in players) if (!p.Health.IsDown) return;
            StartCoroutine(AllDown());
        }

        IEnumerator AllDown()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (Finished) yield break;
            if (boss != null)
            {
                Finish(false);
                yield break;
            }
            Deaths++;
            for (int i = 0; i < players.Count; i++)
                players[i].Health.Revive(players[i].Health.MaxHp, respawnPoint );
        }

        /// <summary>Призрак улетел: на обычном уровне игрок возвращается к напарнику с двумя сердцами.</summary>
        public void OnGhostExpired(PlayerController owner)
        {
            if (boss != null || Finished || !owner.Health.IsDown) return;
            foreach (var p in players)
            {
                if (p == owner || p.Health.IsDown) continue;
                owner.Health.Revive(2, p.transform.position);
                return;
            }
        }

        public void CompleteLevel()
        {
            if (!Finished) Finish(true);
        }

        void OnBossKnockout()
        {
            if (!Finished) Finish(true);
        }

        void Finish(bool won)
        {
            Finished = true;
            foreach (var p in players) p.Locked = true;

            var result = new LevelResult
            {
                level = level,
                won = won,
                time = ElapsedTime,
                hits = HitsTaken,
                parries = Parries,
                deaths = Deaths,
                bossProgress = boss != null ? 1f - boss.Health.Normalized : 1f
            };
            result.grade = Grading.Grade(result);

            var prog = ProgressionService.Instance;
            if (won && prog != null && level != null)
                result.firstClear = prog.CompleteLevel(level, result.grade, result.time, out result.reward);

            StartCoroutine(ShowResult(result, boss != null && won ? bossResultDelay : resultDelay));
        }

        IEnumerator ShowResult(LevelResult result, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            GameEvents.RaiseLevelFinished(result);
        }

        public void Retry()
        {
            GamePause.SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void BackToMap()
        {
            GamePause.SetPaused(false);
            SceneManager.LoadScene(mapScene);
        }
    }
}
