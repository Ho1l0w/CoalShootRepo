using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Супер-шкала из 5 карт. Заполняется уроном по врагам и парированием (+1 карта).
    /// Одна карта тратится на супер-выстрел. Амулет «Крепкий кофе» включает пассивный рост.
    /// </summary>
    public class SuperMeter : MonoBehaviour
    {
        public const int MaxCards = 5;

        [Tooltip("Карт за единицу нанесённого урона")]
        [SerializeField] float cardsPerDamage = 0.028f;
        [SerializeField] float passivePerSecond = 0f;

        public float Value { get; private set; }
        public int Cards => Mathf.FloorToInt(Value);
        public bool Paused { get; set; }
        public float PassivePerSecond
        {
            get => passivePerSecond;
            set => passivePerSecond = Mathf.Max(0f, value);
        }

        public event Action<float> Changed;

        public void AddFromDamage(float damage) => Add(damage * cardsPerDamage);

        public void Add(float cards)
        {
            float old = Value;
            Value = Mathf.Clamp(Value + cards, 0f, MaxCards);
            if (!Mathf.Approximately(old, Value)) Changed?.Invoke(Value);
        }

        public bool TrySpendCard()
        {
            if (Value < 1f) return false;
            Value -= 1f;
            Changed?.Invoke(Value);
            return true;
        }

        public void ResetMeter()
        {
            Value = 0f;
            Changed?.Invoke(Value);
        }

        void Update()
        {
            if (passivePerSecond > 0f && !Paused && Value < MaxCards) Add(passivePerSecond * Time.deltaTime);
        }
    }
}
