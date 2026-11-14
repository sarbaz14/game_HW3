using System.Collections;
using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Stage boss. Behaviour lives in three IBossState phases:
    /// ClusterBarrage -> DiveFeather -> (Enraged when HP drops below 30%).
    /// </summary>
    public class BossController : MonoBehaviour, IDamageable
    {
        public SpriteRenderer spriteRenderer;
        public string bossName = "Giant Chicken Boss";
        public float maxHealth = 300f;
        public float moveSpeed = 3.4f;
        public Vector3 anchor = new Vector3(0f, 3f, 0f);
        public Color enragedTint = new Color(1f, 0.28f, 0.36f);
        public Color telegraphTint = new Color(1f, 0.82f, 0.25f);
        public float superMissileDamageRatio = 0.18f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public float HealthRatio => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);
        public bool IsEnraged => _enraged;
        public bool IsDead => _dead;
        public Vector3 Anchor => anchor;
        public Vector3 MoveTarget { get; set; }
        public float SpeedMultiplier => _enraged ? 1.5f : 1f;
        public Vector3 PlayerPosition => PlayerLocator.PlayerPositionOr(new Vector3(0f, -3f, 0f));
        public WaveManager WaveManager => _waveManager;

        public IBossState ClusterState => _clusterState;
        public IBossState DiveState => _diveState;
        public IBossState EnragedState => _enragedState;

        private LevelDataSO _levelData;
        private WaveManager _waveManager;
        private IBossState _state;
        private BossState_ClusterBarrage _clusterState;
        private BossState_DiveFeather _diveState;
        private BossState_Enraged _enragedState;
        private bool _enraged;
        private bool _dead;
        private bool _entering = true;
        private float _flashTimer;
        private Color _tint = Color.white;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingLayerName = "Gameplay";
            }

            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            GameEvents.OnScreenClearTriggered += HandleScreenClear;
        }

        private void OnDisable()
        {
            GameEvents.OnScreenClearTriggered -= HandleScreenClear;
        }

        public void Initialize(LevelDataSO levelData, WaveManager waveManager)
        {
            _levelData = levelData;
            _waveManager = waveManager;

            if (levelData != null)
            {
                if (!string.IsNullOrEmpty(levelData.bossName)) bossName = levelData.bossName;
                maxHealth = Mathf.Max(10f, levelData.bossMaxHealth);
            }

            CurrentHealth = maxHealth;
            _enraged = false;
            _dead = false;
            _entering = true;
            _tint = Color.white;
            MoveTarget = anchor;

            _clusterState = new BossState_ClusterBarrage();
            _diveState = new BossState_DiveFeather();
            _enragedState = new BossState_Enraged();

            GameEvents.RaiseBossSpawned(bossName, CurrentHealth, maxHealth);
        }

        private void Update()
        {
            if (_dead) return;

            float dt = Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, MoveTarget, moveSpeed * SpeedMultiplier * dt);

            UpdateVisuals(dt);

            if (_entering)
            {
                MoveTarget = anchor;
                if (Vector3.Distance(transform.position, anchor) < 0.2f)
                {
                    _entering = false;
                    ChangeState(_clusterState);
                }

                return;
            }

            CheckEnrage();
            _state?.Tick(this, dt);
        }

        private void UpdateVisuals(float dt)
        {
            if (spriteRenderer != null)
            {
                if (_flashTimer > 0f)
                {
                    spriteRenderer.color = new Color(1.9f, 1.9f, 1.9f, 1f);
                    _flashTimer -= dt;
                }
                else
                {
                    spriteRenderer.color = _tint;
                }
            }

            float pulse = 1f + Mathf.Sin(Time.time * 3.2f) * 0.015f;
            transform.localScale = _baseScale * pulse;
        }

        private void CheckEnrage()
        {
            if (_enraged || _dead) return;
            if (HealthRatio >= 0.3f) return;

            _enraged = true;
            Debug.Log("[BossController] " + bossName + " is ENRAGED (below 30% HP).");
            ChangeState(_enragedState);
        }

        public void ChangeState(IBossState next)
        {
            if (next == null || ReferenceEquals(next, _state)) return;

            _state?.Exit(this);
            _state = next;
            _state.Enter(this);
        }

        public void SetTint(Color tint) => _tint = tint;

        public void FinishDive()
        {
            ChangeState(HealthRatio < 0.3f ? _enragedState : _clusterState);
        }

        // ------------------------------------------------------------ combat

        public void TakeDamage(float amount)
        {
            if (_dead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            _flashTimer = 0.08f;
            GameEvents.RaiseBossHealthChanged(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f) Die();
        }

        private void HandleScreenClear()
        {
            if (_dead) return;
            TakeDamage(maxHealth * superMissileDamageRatio);
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;

            _state?.Exit(this);
            _state = null;

            GameEvents.RaiseBossDefeated();
            GameEvents.RaiseBossHealthChanged(0f, maxHealth);
            GameEvents.RaiseScreenClearTriggered();

            StopAllCoroutines();
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            for (int i = 0; i < 9; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(-1.2f, 1.2f), 0f);
                if (EffectsManager.Instance != null)
                    EffectsManager.Instance.Explosion(transform.position + offset, new Color(1f, 0.75f, 0.35f), 1.4f);

                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.18f, 0.28f);
                AudioCue.Play(AudioLibrary.Sfx.MissileExplosion, 0.08f);
                yield return new WaitForSecondsRealtime(0.12f);
            }

            if (GameManager.Instance != null) GameManager.Instance.HandleBossDefeated(_levelData);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ attack building blocks

        public void FireEggVolley(int count, float spreadDegrees, float speed = 4.9f)
        {
            if (ObjectPooler.Instance == null) return;

            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-spreadDegrees, spreadDegrees, t);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;

                var eggObject = ObjectPooler.Instance.Spawn("EnemyEgg", transform.position + Vector3.down * 0.9f,
                    Quaternion.identity);
                if (eggObject == null) continue;

                var egg = eggObject.GetComponent<EnemyEgg>();
                if (egg != null) egg.Launch(direction * speed);
            }

            AudioCue.Play(AudioLibrary.Sfx.ChickenCluck, 0.10f);
        }

        public void FireFeatherFan(int count, float spreadDegrees, float speed = 5.6f)
        {
            if (ObjectPooler.Instance == null) return;

            count = Mathf.Max(1, count);
            Vector3 origin = transform.position + Vector3.down * 0.6f;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-spreadDegrees, spreadDegrees, t);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;

                var featherObject = ObjectPooler.Instance.Spawn("EnergyFeather", origin, Quaternion.identity);
                if (featherObject == null) continue;

                var feather = featherObject.GetComponent<EnergyFeatherProjectile>();
                if (feather != null) feather.Configure(direction, speed);
            }
        }

        public void FireFeatherRadial(int count, float speed = 4.6f)
        {
            if (ObjectPooler.Instance == null) return;

            count = Mathf.Max(1, count);
            Vector3 origin = transform.position;

            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;

                var featherObject = ObjectPooler.Instance.Spawn("EnergyFeather", origin, Quaternion.identity);
                if (featherObject == null) continue;

                var feather = featherObject.GetComponent<EnergyFeatherProjectile>();
                if (feather != null) feather.Configure(direction, speed);
            }

            AudioCue.Play(AudioLibrary.Sfx.ChickenCluck, 0.05f);
        }

        public void SummonMinions(int count)
        {
            if (_waveManager == null) return;

            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                Vector3 spawn = transform.position + new Vector3(Random.Range(-2.6f, 2.6f), Random.Range(-0.8f, 0.4f), 0f);
                _waveManager.SpawnMinion(spawn);
            }

            AudioCue.Play(AudioLibrary.Sfx.ChickenCluck, 0.03f);
            if (EffectsManager.Instance != null)
                EffectsManager.Instance.HitSpark(transform.position, new Color(1f, 0.45f, 0.5f));
        }
    }
}
