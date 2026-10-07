namespace CoalShoot
{
    /// <summary>Чей урон: снаряды и контакт не бьют своих.</summary>
    public enum Team
    {
        Player,
        Enemy
    }

    public enum CharacterId
    {
        Square,
        Blob
    }

    /// <summary>Настройки текущей сессии: выбираются в меню и читаются уровнем.</summary>
    public static class GameSettings
    {
        public static int PlayerCount = 1;
        public static CharacterId Player1Character = CharacterId.Square;
    }
}
