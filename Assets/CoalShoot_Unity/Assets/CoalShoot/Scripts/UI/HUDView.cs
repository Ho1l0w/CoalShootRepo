using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoalShoot
{
    /// <summary>
    /// Плашка игрока: сердца, 5 карт супера, текущее оружие, уголь.
    /// Игроков создаёт LevelManager, поэтому HUD сам находит своего игрока по номеру.
    /// Карты супера — Image с Type = Filled, Fill Method = Vertical.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [SerializeField] int playerIndex;
        [SerializeField] Image[] hearts;
        [SerializeField] Sprite heartFull;
        [SerializeField] Sprite heartEmpty;
        [SerializeField] Image[] superCards;
        [SerializeField] Color cardFillingColor = new Color(0.14f, 0.27f, 0.88f, 0.6f);
        [SerializeField] Color cardFullColor = new Color(0.14f, 0.27f, 0.88f, 1f);
        [SerializeField] TMP_Text weaponLabel;
        [SerializeField] TMP_Text coalLabel;

        PlayerController player;
        ProgressionService progression;

        void OnEnable()
        {
            progression = ProgressionService.Instance;
            if (progression != null)
            {
                progression.Changed += OnProgressChanged;
                OnProgressChanged();
            }
        }

        void OnDisable()
        {
            if (progression != null) progression.Changed -= OnProgressChanged;
            Unbind();
        }

        void Update()
        {
            if (player != null) return;
            foreach (var p in PlayerRegistry.All)
            {
                if (p != null && p.Index == playerIndex)
                {
                    Bind(p);
                    break;
                }
            }
        }

        void Bind(PlayerController p)
        {
            player = p;
            p.Health.Changed += OnHealth;
            p.Super.Changed += OnSuper;
            if (p.Weapons != null) p.Weapons.WeaponChanged += OnWeapon;
            OnHealth(p.Health.Hp, p.Health.MaxHp);
            OnSuper(p.Super.Value);
            if (p.Weapons != null) OnWeapon(p.Weapons.Active);
        }

        void Unbind()
        {
            if (player == null) return;
            player.Health.Changed -= OnHealth;
            player.Super.Changed -= OnSuper;
            if (player.Weapons != null) player.Weapons.WeaponChanged -= OnWeapon;
            player = null;
        }

        void OnHealth(int hp, int max)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].sprite = i < hp ? heartFull : heartEmpty;
            }
        }

        void OnSuper(float value)
        {
            for (int i = 0; i < superCards.Length; i++)
            {
                if (superCards[i] == null) continue;
                float f = Mathf.Clamp01(value - i);
                superCards[i].fillAmount = f;
                superCards[i].color = f >= 1f ? cardFullColor : cardFillingColor;
            }
        }

        void OnWeapon(WeaponData weapon)
        {
            if (weaponLabel != null) weaponLabel.text = weapon != null ? weapon.displayName : "";
        }

        void OnProgressChanged()
        {
            if (coalLabel != null && progression != null) coalLabel.text = progression.Coal.ToString();
        }
    }
}
