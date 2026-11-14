using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Drives the round sequence of a stage: chicken formations, asteroid belts, auto-save on
    /// round transitions and the boss spawn after the final round.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public LevelDataSO levelData;
        public float roundBreather = 1.5f;

        private int _currentRound;
        private EnemyFormationController _formation;
        private WaveDataSO _activeWave;
        private int _pendingSpawns;
        private int _aliveEnemies;
        private bool _roundActive;
        private bool _bossPhase;
        private float _roundTimer;
        private float _asteroidTimer;
        private float _asteroidInterval = 0.45f;
        private int _asteroidsToSpawn;

        public int CurrentRound => _currentRound;
        public bool BossPhase => _bossPhase;
        public LevelDataSO LevelData => levelData;
        public bool IsRoundActive => _roundActive;

        private void Start()
        {
            if (GameManager.Instance == null) GameManager.EnsureInstance();

            if (levelData == null) levelData = ResolveLevelData();

            if (levelData == null || levelData.RoundCount == 0)
            {
                Debug.LogError("[WaveManager] No LevelDataSO available for scene '" + gameObject.scene.name +
                               "'. Assign one in the inspector or add the level to Resources/GameConfig.");
                return;
            }

            AudioCue.PlayBgm(levelData.levelBGM);

            int round = Mathf.Clamp(GameManager.Instance.RoundIndex, 1, levelData.RoundCount);
            StartRound(round);
        }

        private LevelDataSO ResolveLevelData()
        {
            var config = ConfigProvider.Config;
            string sceneName = gameObject.scene.name;

            if (config != null && config.levels != null)
            {
                for (int i = 0; i < config.levels.Length; i++)
                {
                    var level = config.levels[i];
                    if (level != null && level.sceneName == sceneName) return level;
                }
            }

            return GameManager.GetLevelData(GameManager.ResolveLevelIndex(sceneName));
        }

        // ------------------------------------------------------------ round control

        public void StartRound(int round)
        {
            if (levelData == null || levelData.RoundCount == 0) return;

            _currentRound = Mathf.Clamp(round, 1, levelData.RoundCount);
            _activeWave = levelData.rounds[_currentRound - 1];

            if (GameManager.Instance != null) GameManager.Instance.SetRound(_currentRound);

            _roundActive = true;
            _bossPhase = false;
            _pendingSpawns = 0;
            _aliveEnemies = 0;
            _roundTimer = 0f;
            _asteroidsToSpawn = 0;

            if (_activeWave == null)
            {
                Debug.LogError("[WaveManager] Round " + _currentRound + " of " + levelData.name + " has no WaveDataSO.");
                return;
            }

            if (_activeWave.waveType == WaveType.AsteroidBelt) BeginAsteroidRound(_activeWave);
            else BeginChickenRound(_activeWave);
        }

        private void BeginChickenRound(WaveDataSO wave)
        {
            int count = Mathf.Max(1, wave.spawnCount);
            var offsets = BuildFormationOffsets(wave.formationType, count);

            var formationObject = new GameObject("Formation_Round" + _currentRound);
            _formation = formationObject.AddComponent<EnemyFormationController>();
            _formation.swayAmplitude = Mathf.Max(0.2f, wave.swayAmplitude);
            _formation.swaySpeed = Mathf.Max(0.1f, wave.swaySpeed);
            _formation.descentSpeed = Mathf.Max(0f, wave.descentSpeed);
            _formation.diveInterval = Mathf.Max(1.2f, wave.diveBombInterval / Mathf.Max(0.25f, wave.speedMultiplier));
            _formation.SetSlotOffsets(offsets);

            _pendingSpawns = count;

            for (int i = 0; i < count; i++)
            {
                StartCoroutine(SpawnEnemyRoutine(wave, i, i * 0.07f));
            }
        }

        private IEnumerator SpawnEnemyRoutine(WaveDataSO wave, int index, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (!_roundActive || _formation == null || wave == null) yield break;

            Vector3 slot = _formation.GetBaseSlotPosition(index);
            var path = BuildEntryPath(slot, wave, index);

            var enemyObject = ObjectPooler.Instance != null
                ? ObjectPooler.Instance.Spawn(wave.poolKey, path.Evaluate(0f), Quaternion.identity)
                : null;

            _pendingSpawns = Mathf.Max(0, _pendingSpawns - 1);

            if (enemyObject == null)
            {
                Debug.LogWarning("[WaveManager] Pool '" + wave.poolKey + "' could not spawn a chicken.");
                TryCompleteRound();
                yield break;
            }

            var enemy = enemyObject.GetComponent<EnemyController>();
            if (enemy == null)
            {
                ObjectPooler.Instance.Despawn(enemyObject);
                TryCompleteRound();
                yield break;
            }

            _aliveEnemies++;
            enemy.Initialize(wave, _formation, index, path, this);
        }

        private WaypointPath BuildEntryPath(Vector3 slot, WaveDataSO wave, int index)
        {
            var camera = Camera.main;
            float top = camera != null ? camera.transform.position.y + camera.orthographicSize + 1.4f : 7f;
            float midTop = Mathf.Lerp(top, slot.y, 0.45f);
            float midBottom = Mathf.Lerp(top, slot.y, 0.8f);

            switch (wave.entryPathType)
            {
                case EntryPathType.SCurveSplit:
                {
                    float side = index % 2 == 0 ? -1f : 1f;
                    return new WaypointPath(
                        new Vector3(slot.x + side * 3.6f, top + 1.6f, 0f),
                        new Vector3(slot.x + side * 4.8f, midTop + 1f, 0f),
                        new Vector3(slot.x - side * 2.6f, midBottom, 0f),
                        slot);
                }
                case EntryPathType.SpiralCenter:
                {
                    float angle = index * 0.75f;
                    return new WaypointPath(
                        new Vector3(Mathf.Sin(angle) * 1.4f, top + 2f, 0f),
                        new Vector3(Mathf.Sin(angle + 2f) * 4.8f, midTop, 0f),
                        new Vector3(Mathf.Cos(angle + 2f) * -4.8f, midBottom, 0f),
                        slot);
                }
                default: // TopArcLeftRight
                {
                    float direction = (index + _currentRound) % 2 == 0 ? -1f : 1f;
                    return new WaypointPath(
                        new Vector3(direction * 8.5f, top, 0f),
                        new Vector3(direction * 4.2f, midTop + 1.2f, 0f),
                        new Vector3(-direction * 1.8f, midBottom, 0f),
                        slot);
                }
            }
        }

        private static List<Vector2> BuildFormationOffsets(FormationType type, int count)
        {
            var offsets = new List<Vector2>(count);

            switch (type)
            {
                case FormationType.VShape:
                {
                    const float spacingX = 0.95f;
                    const float spacingY = 0.38f;
                    int chevronWidth = 7;
                    for (int i = 0; i < count; i++)
                    {
                        int chevronIndex = i / chevronWidth;
                        int posInChevron = i % chevronWidth;
                        int arm = posInChevron == 0 ? 0 : (posInChevron % 2 == 1 ? -(posInChevron + 1) / 2 : (posInChevron + 1) / 2);
                        float x = arm * spacingX;
                        float y = 1.6f - chevronIndex * 0.92f - Mathf.Abs(arm) * spacingY;
                        offsets.Add(new Vector2(x, y));
                    }
                    break;
                }
                case FormationType.CompactCluster:
                {
                    const float spacingX = 0.95f;
                    const float spacingY = 0.78f;
                    int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count) + 0.6f), 3, 8);
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / columns;
                        int column = i % columns;
                        float x = (column - (columns - 1) * 0.5f) * spacingX + (row % 2 == 0 ? 0f : spacingX * 0.35f);
                        float y = 1.6f - row * spacingY;
                        offsets.Add(new Vector2(x * 0.9f, y));
                    }
                    break;
                }
                default: // Grid
                {
                    const float spacingX = 1.12f;
                    const float spacingY = 0.82f;
                    int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f), 3, 7);
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / columns;
                        int column = i % columns;
                        float x = (column - (columns - 1) * 0.5f) * spacingX;
                        float y = 1.9f - row * spacingY;
                        offsets.Add(new Vector2(x, y));
                    }
                    break;
                }
            }

            // Compress horizontally so even huge waves stay inside the viewport.
            float maxAbsX = 0.01f;
            for (int i = 0; i < offsets.Count; i++) maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(offsets[i].x));

            if (maxAbsX > 6.2f)
            {
                float scale = 6.2f / maxAbsX;
                for (int i = 0; i < offsets.Count; i++) offsets[i] = new Vector2(offsets[i].x * scale, offsets[i].y);
            }

            return offsets;
        }

        // ------------------------------------------------------------ asteroid rounds

        private void BeginAsteroidRound(WaveDataSO wave)
        {
            _asteroidsToSpawn = Mathf.Max(1, wave.spawnCount);
            _asteroidInterval = Mathf.Max(0.08f, wave.asteroidSpawnInterval / Mathf.Max(0.25f, wave.speedMultiplier));
            _asteroidTimer = 0f;
            _pendingSpawns = _asteroidsToSpawn;
        }

        private void Update()
        {
            if (levelData == null) return;

            var gameManager = GameManager.Instance;
            if (gameManager != null && (gameManager.IsGameOver || gameManager.IsVictory || gameManager.IsPaused)) return;

            if (!_roundActive) return;

            _roundTimer += Time.deltaTime;

            if (_activeWave != null && _activeWave.waveType == WaveType.AsteroidBelt) TickAsteroidSpawner(_activeWave);

            float timeout = _activeWave != null ? Mathf.Max(20f, _activeWave.roundTimeout) : 95f;
            if (_roundTimer > timeout) ForceClearRound();
        }

        private void TickAsteroidSpawner(WaveDataSO wave)
        {
            if (_asteroidsToSpawn <= 0) return;

            _asteroidTimer -= Time.deltaTime;
            if (_asteroidTimer > 0f) return;

            _asteroidTimer = _asteroidInterval;
            SpawnAsteroid(wave);

            _asteroidsToSpawn--;
            _pendingSpawns = Mathf.Max(0, _pendingSpawns - 1);
            _aliveEnemies++;
        }

        private void SpawnAsteroid(WaveDataSO wave)
        {
            if (ObjectPooler.Instance == null) return;

            var camera = Camera.main;
            float top = camera != null ? camera.transform.position.y + camera.orthographicSize + 1.4f : 7f;
            float halfWidth = camera != null ? camera.orthographicSize * camera.aspect : 8f;
            float x = Random.Range(-halfWidth * 0.92f, halfWidth * 0.92f);

            string key = string.IsNullOrEmpty(wave.poolKey) ? "Asteroid" : wave.poolKey;
            var asteroidObject = ObjectPooler.Instance.Spawn(key, new Vector3(x, top, 0f),
                Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            if (asteroidObject == null) return;

            var asteroid = asteroidObject.GetComponent<Asteroid>();
            if (asteroid != null)
            {
                asteroid.Initialize(wave, this, new Vector2(Random.Range(-1.3f, 1.3f),
                    -wave.asteroidSpeed * Random.Range(0.8f, 1.25f)));
            }
        }

        // ------------------------------------------------------------ completion

        public void OnEnemyRemoved(EnemyController enemy)
        {
            if (enemy != null && !enemy.CountsForWave) return;

            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
            TryCompleteRound();
        }

        public void OnAsteroidRemoved()
        {
            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
            TryCompleteRound();
        }

        private void TryCompleteRound()
        {
            if (!_roundActive) return;
            if (_pendingSpawns > 0 || _aliveEnemies > 0) return;
            CompleteRound();
        }

        private void CompleteRound()
        {
            if (!_roundActive) return;

            _roundActive = false;

            if (_formation != null)
            {
                Destroy(_formation.gameObject);
                _formation = null;
            }

            if (_currentRound >= levelData.RoundCount)
            {
                SpawnBoss();
                return;
            }

            int nextRound = _currentRound + 1;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetRound(nextRound);
                GameManager.Instance.AutoSaveProgress(); // auto-save at the round transition
            }

            StartCoroutine(NextRoundRoutine(nextRound));
        }

        private IEnumerator NextRoundRoutine(int nextRound)
        {
            yield return new WaitForSeconds(roundBreather);

            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory)) yield break;
            StartRound(nextRound);
        }

        private void SpawnBoss()
        {
            _bossPhase = true;

            if (levelData.bossPrefab == null)
            {
                Debug.LogError("[WaveManager] LevelData '" + levelData.name + "' has no boss prefab.");
                return;
            }

            var instance = Instantiate(levelData.bossPrefab, new Vector3(0f, 8.5f, 0f), Quaternion.identity);
            instance.name = levelData.bossName;

            var boss = instance.GetComponent<BossController>();
            if (boss != null) boss.Initialize(levelData, this);
            else Debug.LogError("[WaveManager] Boss prefab is missing a BossController component.");
        }

        private void ForceClearRound()
        {
            Debug.LogWarning("[WaveManager] Round watchdog fired - clearing remaining hazards.");

            var enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].CountsForWave) enemies[i].TakeDamage(99999f);
            }

            var asteroids = FindObjectsByType<Asteroid>(FindObjectsInactive.Exclude);
            for (int i = 0; i < asteroids.Length; i++)
            {
                if (asteroids[i] != null) asteroids[i].TakeDamage(99999f);
            }

            _pendingSpawns = 0;
            TryCompleteRound();
        }

        // ------------------------------------------------------------ boss support

        public EnemyController SpawnMinion(Vector3 position)
        {
            if (levelData == null || ObjectPooler.Instance == null) return null;

            string key = string.IsNullOrEmpty(levelData.minionPoolKey) ? "Minion1" : levelData.minionPoolKey;
            var minionObject = ObjectPooler.Instance.Spawn(key, position, Quaternion.identity);
            if (minionObject == null) return null;

            var minion = minionObject.GetComponent<EnemyController>();
            if (minion == null) return null;

            minion.InitializeMinion(this, position + new Vector3(Random.Range(-1.4f, 1.4f), -1.6f, 0f));
            return minion;
        }

        private void OnDestroy()
        {
            if (_formation != null) Destroy(_formation.gameObject);
            if (ObjectPooler.Instance != null) ObjectPooler.Instance.DespawnAll();
        }
    }
}
