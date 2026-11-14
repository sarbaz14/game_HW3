using UnityEngine;

namespace ChickenShooter
{
    [CreateAssetMenu(menuName = "ChickenShooter/LevelData", fileName = "LevelData")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Stage")]
        [Range(1, 3)] public int levelNumber = 1;
        public string sceneName = "Level1";
        public string nextSceneName = "Level2";
        public string themeName = "Earth Orbit";

        [Header("Theme")]
        public Color primaryBgTint = new Color(0.06f, 0.09f, 0.24f);
        public Color secondaryBgTint = new Color(0.15f, 0.4f, 0.85f);
        public AudioClip levelBGM;

        [Header("Rounds (5 / 7 / 10 - boss spawns after the last one)")]
        public WaveDataSO[] rounds = new WaveDataSO[0];

        [Header("Boss")]
        public GameObject bossPrefab;
        public string bossName = "Giant Chicken Boss";
        public float bossMaxHealth = 300f;

        [Header("Minions summoned in the Enraged phase")]
        public GameObject minionPrefab;
        public string minionPoolKey = "Minion1";

        public int RoundCount => rounds != null ? rounds.Length : 0;
    }
}
