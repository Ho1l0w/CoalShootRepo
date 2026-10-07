using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Где лежит сохранение. Для Яндекс Игр замените на облачные сохранения SDK — пример адаптера в README.
    /// </summary>
    public interface ISaveStorage
    {
        string Load();
        void Save(string json);
    }

    /// <summary>Локальное сохранение. В WebGL PlayerPrefs живут в IndexedDB браузера и могут потеряться.</summary>
    public class PlayerPrefsSaveStorage : ISaveStorage
    {
        readonly string key;

        public PlayerPrefsSaveStorage(string key = "coalshoot_save_v1")
        {
            this.key = key;
        }

        public string Load() => PlayerPrefs.GetString(key, string.Empty);

        public void Save(string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
    }
}
