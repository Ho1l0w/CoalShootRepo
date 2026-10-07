using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoalShoot
{
    /// <summary>
    /// Карта: обновляет узлы уровней, показывает уголь, переключает карту и лавку,
    /// выбирает число игроков и персонажа для следующего уровня.
    /// </summary>
    public class WorldMapView : MonoBehaviour
    {
        [SerializeField] LevelNodeView[] nodes;
        [SerializeField] TMP_Text coalLabel;
        [SerializeField] GameObject mapPanel;
        [SerializeField] GameObject shopPanel;
        [SerializeField] Button mapTab;
        [SerializeField] Button shopTab;
        [SerializeField] Button playersButton;
        [SerializeField] TMP_Text playersLabel;
        [SerializeField] Button characterButton;
        [SerializeField] TMP_Text characterLabel;

        ProgressionService progression;

        void OnEnable()
        {
            progression = ProgressionService.Instance;
            if (progression != null) progression.Changed += Refresh;
            if (mapTab != null) mapTab.onClick.AddListener(ShowMap);
            if (shopTab != null) shopTab.onClick.AddListener(ShowShop);
            if (playersButton != null) playersButton.onClick.AddListener(TogglePlayers);
            if (characterButton != null) characterButton.onClick.AddListener(ToggleCharacter);
            ShowMap();
            Refresh();
        }

        void OnDisable()
        {
            if (progression != null) progression.Changed -= Refresh;
            if (mapTab != null) mapTab.onClick.RemoveListener(ShowMap);
            if (shopTab != null) shopTab.onClick.RemoveListener(ShowShop);
            if (playersButton != null) playersButton.onClick.RemoveListener(TogglePlayers);
            if (characterButton != null) characterButton.onClick.RemoveListener(ToggleCharacter);
        }

        public void Refresh()
        {
            foreach (var n in nodes) if (n != null) n.Refresh();
            if (coalLabel != null && progression != null) coalLabel.text = progression.Coal.ToString();
            if (playersLabel != null) playersLabel.text = "Игроков: " + GameSettings.PlayerCount;
            if (characterLabel != null) characterLabel.text = "Персонаж: " + (GameSettings.Player1Character == CharacterId.Square ? "Квадрат" : "Клякса");
        }

        void ShowMap()
        {
            if (mapPanel != null) mapPanel.SetActive(true);
            if (shopPanel != null) shopPanel.SetActive(false);
        }

        void ShowShop()
        {
            if (mapPanel != null) mapPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(true);
        }

        void TogglePlayers()
        {
            GameSettings.PlayerCount = GameSettings.PlayerCount == 1 ? 2 : 1;
            Refresh();
        }

        void ToggleCharacter()
        {
            GameSettings.Player1Character = GameSettings.Player1Character == CharacterId.Square ? CharacterId.Blob : CharacterId.Square;
            Refresh();
        }
    }
}
