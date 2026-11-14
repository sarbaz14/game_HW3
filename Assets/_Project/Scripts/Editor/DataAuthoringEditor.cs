using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace ChickenShooter.EditorTools
{
    /// <summary>
    /// Authors every ScriptableObject: weapons, waves (5/7/10 rounds), levels, audio, config.
    /// Assets are reused when present so scene/prefab references (GUIDs) stay valid on rebuild.
    /// </summary>
    public static class DataAuthoringEditor
    {
        public const string VolumeProfilePath = EditorAssets.MaterialDir + "/GameplayVolumeProfile.asset";

        [MenuItem("ChickenShooter/4. Build ScriptableObjects", false, 40)]
        public static void BuildAllData()
        {
            BuildWeapons();
            var level1 = BuildLevel1();
            var level2 = BuildLevel2();
            var level3 = BuildLevel3();
            var audio = BuildAudioLibrary();
            BuildGameConfig(audio, level1, level2, level3);
            BuildVolumeProfile();

            AssetDatabase.SaveAssets();
            Debug.Log("[Data] ScriptableObjects built (22 waves, 3 levels, 3 weapons, audio library, GameConfig, bloom profile).");
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory)) ProceduralArt.EnsureFolder(directory);

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------------------------------------------------------------- weapons

        private static void BuildWeapons()
        {
            CreateWeapon("Weapon_Lv1", "Pulse Blaster I", 1, "Laser1", "Laser_Lv1", 1f, 17f, 5.5f, FirePattern.Single, 0f);
            CreateWeapon("Weapon_Lv2", "Twin Blaster II", 2, "Laser2", "Laser_Lv2", 1.1f, 18f, 6.5f, FirePattern.DoubleParallel, 0f);
            CreateWeapon("Weapon_Lv3", "Spread Cannon III", 3, "Laser3", "Laser_Lv3", 1.2f, 19f, 7.5f, FirePattern.TripleSpread, 15f);
        }

        private static void CreateWeapon(string assetName, string displayName, int power, string poolKey, string spriteName,
            float damage, float speed, float fireRate, FirePattern pattern, float spread)
        {
            string path = EditorAssets.WeaponDir + "/" + assetName + ".asset";
            var weapon = GetOrCreate<WeaponDataSO>(path);

            weapon.name = assetName;
            weapon.weaponName = displayName;
            weapon.powerLevel = power;
            weapon.projectilePoolKey = poolKey;
            weapon.projectileSprite = EditorAssets.Sprite(spriteName);
            weapon.damage = damage;
            weapon.projectileSpeed = speed;
            weapon.fireRate = fireRate;
            weapon.pattern = pattern;
            weapon.spreadAngle = spread;

            EditorUtility.SetDirty(weapon);
        }

        // ---------------------------------------------------------------- waves

        private static WaveDataSO ChickenWave(string assetName, int level, int count, float health,
            FormationType formation, EntryPathType entryPath, float speedMultiplier, float fireRateMultiplier,
            float eggInterval, float diveInterval, float swayAmplitude, float swaySpeed)
        {
            var wave = GetOrCreate<WaveDataSO>(EditorAssets.WaveDir + "/" + assetName + ".asset");

            wave.name = assetName;
            wave.waveName = assetName;
            wave.waveType = WaveType.ChickenFormation;
            wave.poolKey = "Chicken" + level;
            wave.enemyOrAsteroidPrefab = EditorAssets.Prefab("Enemies/Chicken_Lv" + level);
            wave.spawnCount = count;
            wave.enemyHealth = health;
            wave.moveSpeed = 1f;
            wave.speedMultiplier = speedMultiplier;
            wave.fireRateMultiplier = fireRateMultiplier;
            wave.eggDropInterval = eggInterval;
            wave.diveBombInterval = diveInterval;
            wave.scoreValue = 50f + level * 15f;
            wave.formationType = formation;
            wave.entryPathType = entryPath;
            wave.swayAmplitude = swayAmplitude;
            wave.swaySpeed = swaySpeed;
            wave.descentSpeed = 0.10f + level * 0.03f;
            wave.roundTimeout = 100f;

            EditorUtility.SetDirty(wave);
            return wave;
        }

        private static WaveDataSO AsteroidWave(string assetName, int level, int count, float health, float speed,
            float spawnInterval, float speedMultiplier)
        {
            var wave = GetOrCreate<WaveDataSO>(EditorAssets.WaveDir + "/" + assetName + ".asset");

            wave.name = assetName;
            wave.waveName = assetName;
            wave.waveType = WaveType.AsteroidBelt;
            wave.poolKey = "Asteroid";
            wave.enemyOrAsteroidPrefab = EditorAssets.Prefab("Enemies/Asteroid");
            wave.spawnCount = count;
            wave.asteroidHealth = health;
            wave.asteroidSpeed = speed;
            wave.asteroidSpawnInterval = spawnInterval;
            wave.speedMultiplier = speedMultiplier;
            wave.scoreValue = 50f + level * 15f;
            wave.roundTimeout = 110f;

            EditorUtility.SetDirty(wave);
            return wave;
        }

        // ---------------------------------------------------------------- levels

        private static LevelDataSO BuildLevel1()
        {
            var rounds = new[]
            {
                ChickenWave("Wave_L1_R1", 1, 12, 1f, FormationType.Grid, EntryPathType.TopArcLeftRight, 1f, 1f, 2.8f, 6.5f, 1.7f, 0.8f),
                ChickenWave("Wave_L1_R2", 1, 15, 1f, FormationType.VShape, EntryPathType.SCurveSplit, 1.05f, 1.05f, 2.5f, 5.8f, 1.9f, 0.9f),
                AsteroidWave("Wave_L1_R3", 1, 16, 2f, 2.2f, 0.5f, 1f),
                ChickenWave("Wave_L1_R4", 1, 18, 1.2f, FormationType.CompactCluster, EntryPathType.SpiralCenter, 1.1f, 1.15f, 2.2f, 5f, 2.1f, 1f),
                ChickenWave("Wave_L1_R5", 1, 21, 1.4f, FormationType.Grid, EntryPathType.TopArcLeftRight, 1.2f, 1.25f, 1.9f, 4.4f, 2.4f, 1.1f)
            };

            return CreateLevel("Level1Data", 1, "Level1", "Level2", "Earth Orbit",
                new Color(0.03f, 0.05f, 0.13f), new Color(0.4f, 0.7f, 1f),
                "BGM_Level1", rounds, "Boss_Lv1", "Giant Chicken Boss", 300f);
        }

        private static LevelDataSO BuildLevel2()
        {
            var rounds = new[]
            {
                ChickenWave("Wave_L2_R1", 2, 16, 2f, FormationType.VShape, EntryPathType.SCurveSplit, 1.15f, 1.15f, 2.1f, 5.5f, 1.8f, 0.9f),
                ChickenWave("Wave_L2_R2", 2, 21, 2.2f, FormationType.VShape, EntryPathType.TopArcLeftRight, 1.2f, 1.2f, 1.9f, 4.8f, 2.1f, 1f),
                AsteroidWave("Wave_L2_R3", 2, 20, 3f, 2.6f, 0.42f, 1.1f),
                ChickenWave("Wave_L2_R4", 2, 24, 2.5f, FormationType.CompactCluster, EntryPathType.SpiralCenter, 1.28f, 1.3f, 1.7f, 4.2f, 2.2f, 1.1f),
                ChickenWave("Wave_L2_R5", 2, 26, 2.8f, FormationType.VShape, EntryPathType.SCurveSplit, 1.34f, 1.4f, 1.55f, 3.8f, 2.4f, 1.15f),
                AsteroidWave("Wave_L2_R6", 2, 26, 4f, 3f, 0.36f, 1.2f),
                ChickenWave("Wave_L2_R7", 2, 30, 3.2f, FormationType.CompactCluster, EntryPathType.SpiralCenter, 1.4f, 1.5f, 1.4f, 3.4f, 2.6f, 1.2f)
            };

            return CreateLevel("Level2Data", 2, "Level2", "Level3", "Asteroid Belt & Jupiter",
                new Color(0.1f, 0.055f, 0.045f), new Color(1f, 0.66f, 0.32f),
                "BGM_Level2", rounds, "Boss_Lv2", "Mecha-Chicken Overlord", 520f);
        }

        private static LevelDataSO BuildLevel3()
        {
            var rounds = new[]
            {
                ChickenWave("Wave_L3_R1", 3, 20, 3f, FormationType.CompactCluster, EntryPathType.SCurveSplit, 1.25f, 1.3f, 1.8f, 5f, 1.9f, 0.95f),
                ChickenWave("Wave_L3_R2", 3, 24, 3.2f, FormationType.VShape, EntryPathType.TopArcLeftRight, 1.3f, 1.35f, 1.7f, 4.6f, 2.1f, 1f),
                ChickenWave("Wave_L3_R3", 3, 28, 3.5f, FormationType.CompactCluster, EntryPathType.SpiralCenter, 1.35f, 1.45f, 1.55f, 4f, 2.3f, 1.1f),
                AsteroidWave("Wave_L3_R4", 3, 30, 5f, 3.2f, 0.32f, 1.3f),
                ChickenWave("Wave_L3_R5", 3, 32, 4f, FormationType.CompactCluster, EntryPathType.SCurveSplit, 1.42f, 1.5f, 1.4f, 3.6f, 2.4f, 1.15f),
                ChickenWave("Wave_L3_R6", 3, 32, 4.2f, FormationType.VShape, EntryPathType.TopArcLeftRight, 1.48f, 1.6f, 1.3f, 3.3f, 2.5f, 1.2f),
                AsteroidWave("Wave_L3_R7", 3, 34, 6f, 3.5f, 0.28f, 1.4f),
                ChickenWave("Wave_L3_R8", 3, 34, 4.5f, FormationType.Grid, EntryPathType.SpiralCenter, 1.55f, 1.7f, 1.2f, 3f, 2.6f, 1.25f),
                ChickenWave("Wave_L3_R9", 3, 36, 5f, FormationType.VShape, EntryPathType.SCurveSplit, 1.62f, 1.8f, 1.1f, 2.8f, 2.7f, 1.3f),
                ChickenWave("Wave_L3_R10", 3, 40, 5.5f, FormationType.CompactCluster, EntryPathType.SpiralCenter, 1.7f, 1.9f, 1f, 2.5f, 2.8f, 1.35f)
            };

            return CreateLevel("Level3Data", 3, "Level3", "", "Deep Galaxy",
                new Color(0.05f, 0.02f, 0.1f), new Color(0.75f, 0.35f, 1f),
                "BGM_Level3", rounds, "Boss_Lv3", "Galactic Emperor", 900f);
        }

        private static LevelDataSO CreateLevel(string assetName, int levelNumber, string sceneName, string nextSceneName,
            string themeName, Color primaryTint, Color secondaryTint, string bgmName, WaveDataSO[] rounds,
            string bossPrefabName, string bossName, float bossHealth)
        {
            var level = GetOrCreate<LevelDataSO>(EditorAssets.LevelDir + "/" + assetName + ".asset");

            level.name = assetName;
            level.levelNumber = levelNumber;
            level.sceneName = sceneName;
            level.nextSceneName = nextSceneName;
            level.themeName = themeName;
            level.primaryBgTint = primaryTint;
            level.secondaryBgTint = secondaryTint;
            level.levelBGM = EditorAssets.Bgm(bgmName);
            level.rounds = rounds;
            level.bossPrefab = EditorAssets.Prefab("Bosses/" + bossPrefabName);
            level.bossName = bossName;
            level.bossMaxHealth = bossHealth;
            level.minionPrefab = EditorAssets.Prefab("Enemies/Minion_Lv" + levelNumber);
            level.minionPoolKey = "Minion" + levelNumber;

            EditorUtility.SetDirty(level);
            return level;
        }

        // ---------------------------------------------------------------- audio & config

        private static AudioLibrary BuildAudioLibrary()
        {
            var library = GetOrCreate<AudioLibrary>(EditorAssets.ResourcesDir + "/AudioLibrary.asset");
            library.name = "AudioLibrary";

            library.bgm = new[]
            {
                EditorAssets.Bgm("BGM_MainMenu"),
                EditorAssets.Bgm("BGM_Level1"),
                EditorAssets.Bgm("BGM_Level2"),
                EditorAssets.Bgm("BGM_Level3")
            };

            library.sfx = new[]
            {
                EditorAssets.Sfx("SFX_Laser"),
                EditorAssets.Sfx("SFX_MissileExplosion"),
                EditorAssets.Sfx("SFX_ChickenCluck"),
                EditorAssets.Sfx("SFX_ChickenDeath"),
                EditorAssets.Sfx("SFX_EggCrack"),
                EditorAssets.Sfx("SFX_PickupFood"),
                EditorAssets.Sfx("SFX_PickupGift"),
                EditorAssets.Sfx("SFX_PlayerHit")
            };

            EditorUtility.SetDirty(library);
            return library;
        }

        private static void BuildGameConfig(AudioLibrary audio, LevelDataSO level1, LevelDataSO level2, LevelDataSO level3)
        {
            var config = GetOrCreate<GameConfig>(EditorAssets.ResourcesDir + "/GameConfig.asset");
            config.name = "GameConfig";
            config.inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsGenerator.AssetPath);
            config.audio = audio;
            config.levels = new[] { level1, level2, level3 };
            config.weapons = new[]
            {
                AssetDatabase.LoadAssetAtPath<WeaponDataSO>(EditorAssets.WeaponDir + "/Weapon_Lv1.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponDataSO>(EditorAssets.WeaponDir + "/Weapon_Lv2.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponDataSO>(EditorAssets.WeaponDir + "/Weapon_Lv3.asset")
            };

            config.pools = new[]
            {
                Pool("Laser1", "Projectiles/Laser_Lv1", 16),
                Pool("Laser2", "Projectiles/Laser_Lv2", 16),
                Pool("Laser3", "Projectiles/Laser_Lv3", 24),
                Pool("SuperMissile", "Projectiles/SuperMissile", 3),
                Pool("EnemyEgg", "Enemies/EnemyEgg", 40),
                Pool("EnergyFeather", "Enemies/EnergyFeather", 32),
                Pool("Asteroid", "Enemies/Asteroid", 24),
                Pool("Chicken1", "Enemies/Chicken_Lv1", 24),
                Pool("Chicken2", "Enemies/Chicken_Lv2", 32),
                Pool("Chicken3", "Enemies/Chicken_Lv3", 44),
                Pool("Minion1", "Enemies/Minion_Lv1", 6),
                Pool("Minion2", "Enemies/Minion_Lv2", 6),
                Pool("Minion3", "Enemies/Minion_Lv3", 6),
                Pool("Pickup_Drumstick", "Pickups/Pickup_Drumstick", 8),
                Pool("Pickup_Burger", "Pickups/Pickup_Burger", 6),
                Pool("Pickup_Gift", "Pickups/Pickup_Gift", 4),
                Pool("VFX_Explosion", "VFX/VFX_Explosion", 12),
                Pool("VFX_FeatherSplat", "VFX/VFX_FeatherSplat", 12),
                Pool("VFX_EggCrack", "VFX/VFX_EggCrack", 10),
                Pool("VFX_Shockwave", "VFX/VFX_Shockwave", 3)
            };

            config.playerMoveSpeed = 8f;

            EditorUtility.SetDirty(config);
        }

        private static PoolDefinition Pool(string key, string prefabPath, int initialSize)
        {
            return new PoolDefinition
            {
                key = key,
                prefab = EditorAssets.Prefab(prefabPath),
                initialSize = initialSize
            };
        }

        private static void BuildVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                ProceduralArt.EnsureFolder(EditorAssets.MaterialDir);
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "GameplayVolumeProfile";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(0.85f, 0.92f, 1f));

            if (profile.TryGet(out Vignette vignette))
            {
                vignette.active = true;
                vignette.intensity.Override(0.22f);
                vignette.smoothness.Override(0.4f);
            }

            EditorUtility.SetDirty(profile);
        }

        public static VolumeProfile LoadVolumeProfile()
        {
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        }
    }
}
