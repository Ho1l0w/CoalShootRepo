using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Прогресс игрока между сценами: уголь, собранные куски, пройденные уровни и оценки,
    /// купленное оружие и амулеты, что надето, открытые способности.
    /// Положите префаб с этим компонентом на каждую сцену: лишние копии удаляются сами.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ProgressionService : MonoBehaviour
    {
        public static ProgressionService Instance { get; private set; }

        [Tooltip("Все виды оружия в игре, включая стартовые")]
        [SerializeField] List<WeaponData> weapons = new List<WeaponData>();
        [SerializeField] List<CharmData> charms = new List<CharmData>();
        [Tooltip("Уровни по порядку карты")]
        [SerializeField] List<LevelData> levels = new List<LevelData>();
        [SerializeField] string[] startingWeapons = { "pen", "blot" };

        public IReadOnlyList<WeaponData> Weapons => weapons;
        public IReadOnlyList<CharmData> Charms => charms;
        public IReadOnlyList<LevelData> Levels => levels;
        public SaveData Data { get; private set; }
        /// <summary>Замените до первого Load(), чтобы сохранять в облако.</summary>
        public ISaveStorage Storage { get; set; } = new PlayerPrefsSaveStorage();
        public int Coal => Data.coal;

        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- сохранение ----------

        public void Load()
        {
            string json = Storage.Load();
            SaveData loaded = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { loaded = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("Сохранение повреждено, начинаем заново: " + e.Message); }
            }
            Data = loaded ?? new SaveData();
            Sanitize();
            Changed?.Invoke();
        }

        public void Save()
        {
            Storage.Save(JsonUtility.ToJson(Data));
            Changed?.Invoke();
        }

        public void ResetProgress()
        {
            Data = new SaveData();
            Sanitize();
            Save();
        }

        void Sanitize()
        {
            if (Data.collected == null) Data.collected = new List<string>();
            if (Data.levels == null) Data.levels = new List<LevelRecord>();
            if (Data.ownedWeapons == null) Data.ownedWeapons = new List<string>();
            if (Data.ownedCharms == null) Data.ownedCharms = new List<string>();
            if (Data.abilities == null) Data.abilities = new List<string>();
            foreach (var id in startingWeapons) if (!Data.ownedWeapons.Contains(id)) Data.ownedWeapons.Add(id);
            if (Data.slots == null || Data.slots.Length != 2)
                Data.slots = new[] { startingWeapons.Length > 0 ? startingWeapons[0] : "", startingWeapons.Length > 1 ? startingWeapons[1] : "" };
            if (Data.charm == null) Data.charm = "";
        }

        // ---------- уровни ----------

        public LevelRecord GetRecord(LevelData level) => level == null ? null : Data.levels.Find(r => r.id == level.id);
        public bool IsCleared(LevelData level) => GetRecord(level) != null;
        public bool IsUnlocked(LevelData level) => level != null && (level.requires == null || IsCleared(level.requires));

        public int CoalFound(LevelData level)
        {
            if (level == null) return 0;
            string prefix = level.id + "-";
            int n = 0;
            foreach (var id in Data.collected) if (id.StartsWith(prefix, StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>Записать прохождение. Возвращает true при первом прохождении; reward — выданный уголь.</summary>
        public bool CompleteLevel(LevelData level, string grade, float time, out int reward)
        {
            reward = 0;
            LevelRecord rec = GetRecord(level);
            bool first = rec == null;
            if (first)
            {
                Data.levels.Add(new LevelRecord { id = level.id, grade = grade, bestTime = time });
                reward = level.firstClearReward;
                Data.coal += reward;
            }
            else
            {
                if (Grading.Compare(grade, rec.grade) > 0) rec.grade = grade;
                rec.bestTime = Mathf.Min(rec.bestTime, time);
            }
            Save();
            return first;
        }

        // ---------- уголь ----------

        public bool IsCollected(string coalId) => Data.collected.Contains(coalId);

        public void CollectCoal(string coalId)
        {
            if (string.IsNullOrEmpty(coalId) || IsCollected(coalId)) return;
            Data.collected.Add(coalId);
            Data.coal++;
            Save();
        }

        public void AddCoal(int amount)
        {
            Data.coal = Mathf.Max(0, Data.coal + amount);
            Save();
        }

        // ---------- оружие ----------

        public WeaponData FindWeapon(string weaponId) => weapons.Find(w => w != null && w.id == weaponId);
        public bool Owns(WeaponData weapon) => weapon != null && Data.ownedWeapons.Contains(weapon.id);
        public WeaponData GetEquippedWeapon(int slot) => slot >= 0 && slot < 2 ? FindWeapon(Data.slots[slot]) : null;
        public int SlotOf(WeaponData weapon) => weapon == null ? -1 : Array.IndexOf(Data.slots, weapon.id);

        public bool TryBuy(WeaponData weapon)
        {
            if (weapon == null || Owns(weapon) || Data.coal < weapon.price) return false;
            Data.coal -= weapon.price;
            Data.ownedWeapons.Add(weapon.id);
            Save();
            return true;
        }

        /// <summary>Надеть в слот. Если оружие уже во втором слоте — слоты меняются местами.</summary>
        public void Equip(int slot, WeaponData weapon)
        {
            if (!Owns(weapon) || slot < 0 || slot > 1) return;
            int other = 1 - slot;
            if (Data.slots[other] == weapon.id) Data.slots[other] = Data.slots[slot];
            Data.slots[slot] = weapon.id;
            Save();
        }

        // ---------- амулеты ----------

        public CharmData FindCharm(string charmId) => charms.Find(c => c != null && c.id == charmId);
        public bool Owns(CharmData charm) => charm != null && Data.ownedCharms.Contains(charm.id);
        public CharmData EquippedCharm => string.IsNullOrEmpty(Data.charm) ? null : FindCharm(Data.charm);

        public bool IsCharmEquipped(CharmEffect effect)
        {
            CharmData c = EquippedCharm;
            return c != null && c.effect == effect;
        }

        /// <summary>Купить и сразу надеть.</summary>
        public bool TryBuy(CharmData charm)
        {
            if (charm == null || Owns(charm) || Data.coal < charm.price) return false;
            Data.coal -= charm.price;
            Data.ownedCharms.Add(charm.id);
            Data.charm = charm.id;
            Save();
            return true;
        }

        /// <summary>Надеть амулет или снять (null).</summary>
        public void EquipCharm(CharmData charm)
        {
            if (charm != null && !Owns(charm)) return;
            Data.charm = charm != null ? charm.id : "";
            Save();
        }

        // ---------- способности ----------

        public bool HasAbility(Ability ability) => Data.abilities.Contains(ability.ToString());

        public void UnlockAbility(Ability ability)
        {
            if (HasAbility(ability)) return;
            Data.abilities.Add(ability.ToString());
            Save();
        }
    }
}
