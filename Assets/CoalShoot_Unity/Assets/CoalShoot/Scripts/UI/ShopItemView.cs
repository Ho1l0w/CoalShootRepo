using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoalShoot
{
    /// <summary>
    /// Карточка лавки. Для оружия видны «Купить» или «В слот 1» / «В слот 2»,
    /// для амулета — «Купить» или «Надеть» / «Снять».
    /// </summary>
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text description;
        [SerializeField] TMP_Text state;
        [SerializeField] Image icon;
        [SerializeField] GameObject equippedMark;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text buyLabel;
        [SerializeField] Button slot1Button;
        [SerializeField] Button slot2Button;
        [SerializeField] Button equipButton;
        [SerializeField] TMP_Text equipLabel;

        WeaponData weapon;
        CharmData charm;
        ShopView shop;

        public void Setup(WeaponData w, ShopView owner)
        {
            weapon = w;
            charm = null;
            shop = owner;
            Fill(w.displayName, w.description, w.icon);
            Wire();
        }

        public void Setup(CharmData c, ShopView owner)
        {
            charm = c;
            weapon = null;
            shop = owner;
            Fill(c.displayName, c.description, c.icon);
            Wire();
        }

        void Fill(string name, string text, Sprite sprite)
        {
            if (title != null) title.text = name;
            if (description != null) description.text = text;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }

        void Wire()
        {
            if (buyButton != null) { buyButton.onClick.RemoveAllListeners(); buyButton.onClick.AddListener(Buy); }
            if (slot1Button != null) { slot1Button.onClick.RemoveAllListeners(); slot1Button.onClick.AddListener(() => EquipSlot(0)); }
            if (slot2Button != null) { slot2Button.onClick.RemoveAllListeners(); slot2Button.onClick.AddListener(() => EquipSlot(1)); }
            if (equipButton != null) { equipButton.onClick.RemoveAllListeners(); equipButton.onClick.AddListener(ToggleCharm); }
        }

        public void Refresh()
        {
            var prog = ProgressionService.Instance;
            if (prog == null) return;

            if (weapon != null)
            {
                bool owned = prog.Owns(weapon);
                int slot = prog.SlotOf(weapon);
                SetActive(buyButton, !owned);
                SetActive(slot1Button, owned);
                SetActive(slot2Button, owned);
                SetActive(equipButton, false);
                if (buyButton != null) buyButton.interactable = prog.Coal >= weapon.price;
                if (buyLabel != null) buyLabel.text = $"Купить · {weapon.price}";
                if (slot1Button != null) slot1Button.interactable = slot != 0;
                if (slot2Button != null) slot2Button.interactable = slot != 1;
                if (state != null) state.text = owned ? (slot >= 0 ? $"слот {slot + 1}" : "куплено") : $"{weapon.price} угля";
                if (equippedMark != null) equippedMark.SetActive(slot >= 0);
            }
            else if (charm != null)
            {
                bool owned = prog.Owns(charm);
                bool on = prog.EquippedCharm == charm;
                SetActive(buyButton, !owned);
                SetActive(slot1Button, false);
                SetActive(slot2Button, false);
                SetActive(equipButton, owned);
                if (buyButton != null) buyButton.interactable = prog.Coal >= charm.price;
                if (buyLabel != null) buyLabel.text = $"Купить · {charm.price}";
                if (equipLabel != null) equipLabel.text = on ? "Снять" : "Надеть";
                if (state != null) state.text = owned ? (on ? "надет" : "куплен") : $"{charm.price} угля";
                if (equippedMark != null) equippedMark.SetActive(on);
            }
        }

        static void SetActive(Button b, bool active)
        {
            if (b != null) b.gameObject.SetActive(active);
        }

        void Buy()
        {
            var prog = ProgressionService.Instance;
            if (prog == null) return;
            bool ok = weapon != null ? prog.TryBuy(weapon) : prog.TryBuy(charm);
            if (shop != null) shop.SetStatus(ok ? $"Куплено: {(weapon != null ? weapon.displayName : charm.displayName)}." : "Не хватает угля.");
        }

        void EquipSlot(int slot)
        {
            var prog = ProgressionService.Instance;
            if (prog == null || weapon == null) return;
            prog.Equip(slot, weapon);
            if (shop != null) shop.SetStatus($"{weapon.displayName} в слоте {slot + 1}.");
        }

        void ToggleCharm()
        {
            var prog = ProgressionService.Instance;
            if (prog == null || charm == null) return;
            bool on = prog.EquippedCharm == charm;
            prog.EquipCharm(on ? null : charm);
            if (shop != null) shop.SetStatus(on ? "Амулет снят." : $"Надет: {charm.displayName}.");
        }
    }
}
