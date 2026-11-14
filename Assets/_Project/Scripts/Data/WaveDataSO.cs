using UnityEngine;

namespace ChickenShooter
{
    public enum WaveType
    {
        ChickenFormation,
        AsteroidBelt
    }

    public enum FormationType
    {
        Grid,
        VShape,
        CompactCluster
    }

    public enum EntryPathType
    {
        TopArcLeftRight,
        SCurveSplit,
        SpiralCenter
    }

    [CreateAssetMenu(menuName = "ChickenShooter/WaveData", fileName = "WaveData")]
    public class WaveDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string waveName = "Wave";
        public WaveType waveType = WaveType.ChickenFormation;

        [Header("Spawn")]
        public GameObject enemyOrAsteroidPrefab;
        public string poolKey = "Chicken1";
        public int spawnCount = 12;

        [Header("Enemy Stats")]
        public float enemyHealth = 1f;
        public float moveSpeed = 1f;
        public float eggDropInterval = 2.5f;
        public float diveBombInterval = 5f;
        public float scoreValue = 50f;

        [Header("Formation")]
        public FormationType formationType = FormationType.Grid;
        public EntryPathType entryPathType = EntryPathType.TopArcLeftRight;
        public float swayAmplitude = 1.8f;
        public float swaySpeed = 0.8f;
        public float descentSpeed = 0.12f;

        [Header("Asteroids")]
        public float asteroidSpawnInterval = 0.45f;
        public float asteroidHealth = 2f;
        public float asteroidSpeed = 2.2f;

        [Header("Difficulty Multipliers (reuse templates across stages)")]
        public float speedMultiplier = 1f;
        public float fireRateMultiplier = 1f;

        [Header("Pacing")]
        public float roundTimeout = 95f;
    }
}
