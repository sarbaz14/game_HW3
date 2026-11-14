using System;

namespace ChickenShooter
{
    [Serializable]
    public class SaveData
    {
        public int levelIndex = 1;          // 1..3
        public int roundIndex = 1;          // 1..N
        public int totalScore = 0;
        public int remainingLives = 3;
        public int secondaryMissiles = 2;
        public int weaponPowerLevel = 1;    // 1..3
        public float foodProgress = 0f;     // 0..100
    }
}
