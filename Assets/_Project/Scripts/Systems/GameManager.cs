using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChickenShooter
{
    /// <summary>
    /// Persistent run state (DontDestroyOnLoad). Owns the save/load rules, round transitions
    /// and stage flow. Never holds references to UI widgets - it only raises GameEvents.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const int StartingLives = 3;
        public const int StartingMissiles = 2;
        public const int MaxWeaponPower = 3;
        public const string MenuSceneName = "MainMenu";

        public int LevelIndex { get; private set; } = 1;
        public int RoundIndex { get; private set; } = 1;
        public int TotalScore { get; private set; }
        public int RemainingLives { get; private set; } = StartingLives;
        public int SecondaryMissiles { get; private set; } = StartingMissiles;
        public int WeaponPowerLevel { get; private set; } = 1;
        public float FoodProgress { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }

        private bool _sceneHookAttached;

        public static GameManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!_sceneHookAttached)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                _sceneHookAttached = true;
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (_sceneHookAttached) SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }

        // ---------------------------------------------------------------- scene flow

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            IsPaused = false;
            IsGameOver = false;
            IsVictory = false;

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.DespawnAll();

            if (scene.name.StartsWith("Level"))
            {
                int sceneLevel = ResolveLevelIndex(scene.name);
                if (sceneLevel != LevelIndex)
                {
                    // Player launched a level scene directly (Play button in the editor) or jumped scenes.
                    LevelIndex = sceneLevel;
                    RoundIndex = 1;
                    RemainingLives = StartingLives;
                    SecondaryMissiles = StartingMissiles;
                    WeaponPowerLevel = 1;
                    FoodProgress = 0f;
                    TotalScore = 0;
                }
                else if (RemainingLives <= 0)
                {
                    RemainingLives = StartingLives;
                }

                var level = GetLevelData(LevelIndex);
                if (level != null && level.RoundCount > 0)
                {
                    RoundIndex = Mathf.Clamp(RoundIndex, 1, level.RoundCount);
                }
            }

            SystemsBootstrap.Ensure();
        }

        public static int ResolveLevelIndex(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return 1;

            var config = ConfigProvider.Config;
            if (config != null && config.levels != null)
            {
                for (int i = 0; i < config.levels.Length; i++)
                {
                    var level = config.levels[i];
                    if (level != null && string.Equals(level.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return Mathf.Clamp(level.levelNumber, 1, 3);
                    }
                }
            }

            if (sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring(5), out int parsed))
            {
                return Mathf.Clamp(parsed, 1, 3);
            }

            return 1;
        }

        public static LevelDataSO GetLevelData(int levelNumber)
        {
            var config = ConfigProvider.Config;
            if (config == null || config.levels == null) return null;

            for (int i = 0; i < config.levels.Length; i++)
            {
                var level = config.levels[i];
                if (level != null && level.levelNumber == levelNumber) return level;
            }

            if (config.levels.Length > 0)
            {
                int index = Mathf.Clamp(levelNumber - 1, 0, config.levels.Length - 1);
                return config.levels[index];
            }

            return null;
        }

        // ---------------------------------------------------------------- lifecycle

        public void StartNewGame()
        {
            ResetState();
            SaveSystem.Save(BuildSaveData());
            LoadLevelScene(1);
        }

        public void ContinueGame()
        {
            var data = SaveSystem.Load();
            if (data == null)
            {
                Debug.LogWarning("[GameManager] Continue pressed without a save file. Starting a new game.");
                StartNewGame();
                return;
            }

            ApplySaveData(data);
            LoadLevelScene(data.levelIndex);
        }

        public void AutoSaveProgress()
        {
            SaveSystem.Save(BuildSaveData());
        }

        public void HandleGameOver()
        {
            if (IsGameOver) return;

            IsGameOver = true;
            RemainingLives = 0;

            // Rule: the exact moment lives reach 0 the save is overwritten to Round 1 of the current level.
            var data = BuildSaveData();
            data.levelIndex = LevelIndex;
            data.roundIndex = 1;
            data.remainingLives = StartingLives;
            data.weaponPowerLevel = 1;
            data.foodProgress = 0f;
            SaveSystem.Save(data);

            Time.timeScale = 0f;
            Debug.Log("[GameManager] Game Over - save overwritten to Level " + data.levelIndex + " Round 1.");
            GameEvents.RaiseGameOver();
        }

        public void RetryCurrentLevel()
        {
            Time.timeScale = 1f;
            IsGameOver = false;

            var data = SaveSystem.Load();
            if (data != null) ApplySaveData(data);

            RoundIndex = 1;
            RemainingLives = StartingLives;
            WeaponPowerLevel = 1;
            FoodProgress = 0f;
            SecondaryMissiles = StartingMissiles;
            TotalScore = data != null ? data.totalScore : TotalScore;

            LoadLevelScene(Mathf.Clamp(LevelIndex, 1, 3));
        }

        public void HandleBossDefeated(LevelDataSO completedLevel)
        {
            if (completedLevel == null)
            {
                HandleVictory();
                return;
            }

            if (completedLevel.levelNumber < 3 && !string.IsNullOrEmpty(completedLevel.nextSceneName))
            {
                LevelIndex = Mathf.Clamp(completedLevel.levelNumber + 1, 1, 3);
                RoundIndex = 1;
                FoodProgress = 0f;
                AutoSaveProgress();

                GameEvents.RaiseStageCompleted("STAGE " + completedLevel.levelNumber + " COMPLETE!\nEntering " +
                                              completedLevel.nextSceneName.Replace("Level", "Stage ") + " - " +
                                              GetThemeName(LevelIndex));
                StartCoroutine(LoadNextLevelRoutine(completedLevel.nextSceneName, 2.5f));
            }
            else
            {
                HandleVictory();
            }
        }

        private string GetThemeName(int levelNumber)
        {
            var level = GetLevelData(levelNumber);
            return level != null ? level.themeName : "Deep Space";
        }

        private IEnumerator LoadNextLevelRoutine(string sceneName, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            LoadSceneByName(sceneName);
        }

        public void HandleVictory()
        {
            IsVictory = true;
            SaveSystem.DeleteSave(); // Rule: save is deleted/reset on Level 3 victory.
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Victory! Save file reset.");
            GameEvents.RaiseGameVictory();
        }

        public void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            IsGameOver = false;
            IsVictory = false;
            RemainingLives = StartingLives;
            LoadSceneByName(MenuSceneName);
        }

        public void LoadLevelScene(int levelNumber)
        {
            LoadSceneByName("Level" + Mathf.Clamp(levelNumber, 1, 3));
        }

        public void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[GameManager] Tried to load a scene with an empty name.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[GameManager] Scene '" + sceneName + "' is not registered in Build Settings.");
                return;
            }

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.DespawnAll();

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        // ---------------------------------------------------------------- gameplay state

        public void SetRound(int round)
        {
            RoundIndex = Mathf.Max(1, round);
            GameEvents.RaiseRoundChanged(LevelIndex, RoundIndex);
        }

        public void AddScore(int amount)
        {
            if (amount == 0) return;
            TotalScore = Mathf.Max(0, TotalScore + amount);
            GameEvents.RaiseScoreChanged(TotalScore);
        }

        public void AddMissile(int amount = 1)
        {
            SecondaryMissiles = Mathf.Max(0, SecondaryMissiles + amount);
            GameEvents.RaiseMissilesChanged(SecondaryMissiles);
        }

        public bool SpendMissile()
        {
            if (SecondaryMissiles <= 0) return false;
            SecondaryMissiles--;
            GameEvents.RaiseMissilesChanged(SecondaryMissiles);
            return true;
        }

        public void AddFoodProgress(float amount)
        {
            FoodProgress = Mathf.Clamp(FoodProgress + amount, 0f, 100f);

            if (FoodProgress >= 100f)
            {
                FoodProgress = 0f;
                AddMissile(1);
                Debug.Log("[GameManager] Food meter full - granted a Super Missile.");
            }

            GameEvents.RaiseFoodProgressChanged(FoodProgress / 100f);
        }

        public void UpgradeWeapon()
        {
            WeaponPowerLevel = Mathf.Clamp(WeaponPowerLevel + 1, 1, MaxWeaponPower);
            GameEvents.RaiseWeaponPowerChanged(WeaponPowerLevel);
        }

        public void DowngradeWeapon()
        {
            WeaponPowerLevel = Mathf.Clamp(WeaponPowerLevel - 1, 1, MaxWeaponPower);
            GameEvents.RaiseWeaponPowerChanged(WeaponPowerLevel);
        }

        public void LoseLife()
        {
            if (IsGameOver) return;

            RemainingLives = Mathf.Max(0, RemainingLives - 1);
            GameEvents.RaiseLivesChanged(RemainingLives);

            if (RemainingLives <= 0) HandleGameOver();
        }

        public void SetPaused(bool paused)
        {
            if (IsGameOver || IsVictory) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            GameEvents.RaisePauseStateChanged(paused);
        }

        public void TogglePause() => SetPaused(!IsPaused);

        // ---------------------------------------------------------------- helpers

        public SaveData BuildSaveData()
        {
            return new SaveData
            {
                levelIndex = Mathf.Clamp(LevelIndex, 1, 3),
                roundIndex = Mathf.Max(1, RoundIndex),
                totalScore = TotalScore,
                remainingLives = RemainingLives,
                secondaryMissiles = SecondaryMissiles,
                weaponPowerLevel = WeaponPowerLevel,
                foodProgress = FoodProgress
            };
        }

        public void ApplySaveData(SaveData data)
        {
            if (data == null) return;

            LevelIndex = Mathf.Clamp(data.levelIndex, 1, 3);
            RoundIndex = Mathf.Max(1, data.roundIndex);
            TotalScore = Mathf.Max(0, data.totalScore);
            RemainingLives = Mathf.Clamp(data.remainingLives, 1, StartingLives);
            SecondaryMissiles = Mathf.Max(0, data.secondaryMissiles);
            WeaponPowerLevel = Mathf.Clamp(data.weaponPowerLevel, 1, MaxWeaponPower);
            FoodProgress = Mathf.Clamp(data.foodProgress, 0f, 100f);
        }

        private void ResetState()
        {
            LevelIndex = 1;
            RoundIndex = 1;
            TotalScore = 0;
            RemainingLives = StartingLives;
            SecondaryMissiles = StartingMissiles;
            WeaponPowerLevel = 1;
            FoodProgress = 0f;
            IsPaused = false;
            IsGameOver = false;
            IsVictory = false;
            Time.timeScale = 1f;
        }
    }
}
