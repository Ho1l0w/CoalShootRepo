using System;
using UnityEngine;

namespace CoalShoot
{
    /// <summary>Итог уровня для экрана результатов.</summary>
    public class LevelResult
    {
        public LevelData level;
        public bool won;
        public float time;
        public int hits;
        public int parries;
        public int deaths;
        /// <summary>Доля здоровья босса, которую успели снять (для экрана поражения).</summary>
        public float bossProgress;
        public string grade;
        public bool firstClear;
        public int reward;
    }

    /// <summary>Оценка от D до A+, как в прототипе.</summary>
    public static class Grading
    {
        public static readonly string[] Order = { "D", "C", "B", "B+", "A-", "A", "A+" };

        public static string Grade(LevelResult r)
        {
            float par = r.level != null ? r.level.parTime : 80f;
            bool isBoss = r.level != null && r.level.isBoss;
            float score = isBoss
                ? 100f - r.hits * 10f + r.parries * 5f - Mathf.Max(0f, r.time - par) * 0.4f
                : 100f - r.hits * 9f - r.deaths * 12f + r.parries * 3f - Mathf.Max(0f, r.time - par) * 0.5f;
            return FromScore(score);
        }

        public static string FromScore(float s)
        {
            if (s >= 95f) return "A+";
            if (s >= 85f) return "A";
            if (s >= 75f) return "A-";
            if (s >= 65f) return "B+";
            if (s >= 55f) return "B";
            if (s >= 40f) return "C";
            return "D";
        }

        /// <summary>&gt;0 — a лучше b.</summary>
        public static int Compare(string a, string b) => Array.IndexOf(Order, a).CompareTo(Array.IndexOf(Order, b));
    }
}
