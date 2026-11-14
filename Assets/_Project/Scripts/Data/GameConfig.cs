using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChickenShooter
{
    [Serializable]
    public class PoolDefinition
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 8;
    }

    /// <summary>
    /// Single asset registry so every scene (and a directly-played level scene) can resolve
    /// its data without manual inspector wiring.
    /// </summary>
    [CreateAssetMenu(menuName = "ChickenShooter/GameConfig", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public InputActionAsset inputActions;
        public WeaponDataSO[] weapons = new WeaponDataSO[0];
        public LevelDataSO[] levels = new LevelDataSO[0];
        public AudioLibrary audio;
        public PoolDefinition[] pools = new PoolDefinition[0];
        public float playerMoveSpeed = 8f;
    }

    /// <summary>Null-safe accessor for the Resources config asset.</summary>
    public static class ConfigProvider
    {
        private static GameConfig _config;

        public static GameConfig Config
        {
            get
            {
                if (_config != null) return _config;

                _config = Resources.Load<GameConfig>("GameConfig");
                if (_config == null)
                {
                    Debug.LogWarning("[ChickenShooter] Resources/GameConfig.asset was not found. " +
                                     "Falling back to in-memory defaults so the scene still boots cleanly.");
                    _config = ScriptableObject.CreateInstance<GameConfig>();
                    _config.name = "RuntimeGameConfig";
                }

                return _config;
            }
        }

        public static void ResetCache()
        {
            _config = null;
        }
    }
}
