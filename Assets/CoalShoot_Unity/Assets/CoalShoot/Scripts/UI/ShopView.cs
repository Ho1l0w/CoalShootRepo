using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Лавка: строит карточки оружия и амулетов из списков ProgressionService
    /// и обновляет их при любой покупке.
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        [SerializeField] ShopItemView itemPrefab;
        [SerializeField] Transform weaponsRoot;
        [SerializeField] Transform charmsRoot;
        [SerializeField] TMP_Text coalLabel;
        [SerializeField] TMP_Text statusLabel;

        readonly List<ShopItemView> items = new List<ShopItemView>();
        ProgressionService progression;

        void OnEnable()
        {
            progression = ProgressionService.Instance;
            if (progression == null) return;
            progression.Changed += Refresh;
            Build();
        }

        void OnDisable()
        {
            if (progression != null) progression.Changed -= Refresh;
        }

        void Build()
        {
            foreach (var item in items) if (item != null) Destroy(item.gameObject);
            items.Clear();
            foreach (var w in progression.Weapons)
            {
                if (w == null) continue;
                var view = Instantiate(itemPrefab, weaponsRoot);
                view.Setup(w, this);
                items.Add(view);
            }
            foreach (var c in progression.Charms)
            {
                if (c == null) continue;
                var view = Instantiate(itemPrefab, charmsRoot);
                view.Setup(c, this);
                items.Add(view);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (coalLabel != null && progression != null) coalLabel.text = progression.Coal.ToString();
            foreach (var item in items) if (item != null) item.Refresh();
        }

        public void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }
    }
}
