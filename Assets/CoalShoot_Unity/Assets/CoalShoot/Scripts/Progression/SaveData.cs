using System;
using System.Collections.Generic;

namespace CoalShoot
{
    public enum Ability
    {
        DoubleJump
    }

    [Serializable]
    public class LevelRecord
    {
        public int id;
        public string grade;
        public float bestTime;
    }

    /// <summary>
    /// Всё сохранение игрока. Только списки и простые поля — JsonUtility не умеет словари.
    /// Меняете формат — увеличьте version и допишите миграцию в ProgressionService.Sanitize.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int coal;
        public List<string> collected = new List<string>();
        public List<LevelRecord> levels = new List<LevelRecord>();
        public List<string> ownedWeapons = new List<string> { "pen", "blot" };
        public string[] slots = { "pen", "blot" };
        public List<string> ownedCharms = new List<string>();
        public string charm = "";
        public List<string> abilities = new List<string>();
    }
}
