using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Space chicken. States: Entering (curved waypoint path) -> InFormation (egg drops) ->
    /// Diving (no eggs, rams the player, wraps back to the formation). Boss minions use Hovering.
    /// </summary>
    public class EnemyController : MonoBehaviour, IPooledObject, IDamageable
    {
        public enum EnemyState
        {
            Idle,
            Entering,
            InFormation,
            Diving,
            Hovering
        }

        [Header("Visuals")]
        public SpriteRenderer spriteRenderer;
        public string eggPoolKey = "EnemyEgg";

        [Header("Behaviour")]
        public float baseScore = 50f;
        public float entryDuration = 1.6f;
        public float diveSpeed = 7f;
        public float hoverSpeed = 2.2f;

        public bool CountsForWave { get; set; } = true;
        public bool IsInFormation => _state == EnemyState.InFormation;
        public bool IsDiving => _state == EnemyState.Diving;

        private EnemyState _state = EnemyState.Idle;
        private float _health = 1f;
        private float _score = 50f;
        private float _eggInterval = 2.5f;
        private float _eggTimer;
        private float _pathProgress;
        private float _flashTimer;
        private float _hoverPhase;
        private float _minionDiveTimer;
        private Vector2 _diveDirection = Vector2.down;
        private Vector3 _hoverTarget;
        private bool _removed;
        private WaypointPath _path;
        private EnemyFormationController _formation;
        private WaveManager _waveManager;
        private int _slotIndex;
        private Color _baseColor = Color.white;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) _baseColor = spriteRenderer.color;
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

        public void OnObjectSpawn()
        {
            _removed = false;
            _flashTimer = 0f;
            _pathProgress = 0f;
            _eggTimer = 1f;
            _diveTimerReset();
            transform.rotation = Quaternion.identity;
            transform.localScale = _baseScale;
            if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        }

        public void OnObjectReturn()
        {
            _formation = null;
            _waveManager = null;
            _state = EnemyState.Idle;
        }

        private void _diveTimerReset()
        {
            _minionDiveTimer = Random.Range(4f, 7f);
        }

        // ------------------------------------------------------------ setup

        public void Initialize(WaveDataSO wave, EnemyFormationController formation, int slotIndex,
            WaypointPath path, WaveManager waveManager)
        {
            _waveManager = waveManager;
            _formation = formation;
            _slotIndex = slotIndex;
            _path = path;
            CountsForWave = true;
            _removed = false;
            _state = EnemyState.Entering;

            float difficulty = Mathf.Max(0.2f, wave.speedMultiplier);
            _health = Mathf.Max(1f, wave.enemyHealth);
            _score = Mathf.Max(1f, wave.scoreValue);
            entryDuration = Mathf.Max(0.35f, entryDuration / difficulty);
            _eggInterval = Mathf.Max(0.45f, wave.eggDropInterval /
                                           Mathf.Max(0.15f, difficulty * Mathf.Max(0.2f, wave.fireRateMultiplier)));
            _eggTimer = Random.Range(_eggInterval * 0.35f, _eggInterval);

            if (_path != null) transform.position = _path.Evaluate(0f);
        }

        public void InitializeMinion(WaveManager waveManager, Vector3 hoverTarget)
        {
            _waveManager = waveManager;
            _formation = null;
            _path = null;
            CountsForWave = false;
            _removed = false;
            _health = 1f;
            _score = 75f;
            _eggInterval = 2.4f;
            _eggTimer = Random.Range(0.6f, 1.8f);
            _hoverTarget = hoverTarget;
            _hoverPhase = Random.Range(0f, Mathf.PI * 2f);
            _state = EnemyState.Hovering;
            _diveTimerReset();
        }

        // ------------------------------------------------------------ update

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateFlash(dt);

            switch (_state)
            {
                case EnemyState.Entering:
                    UpdateEntering(dt);
                    break;
                case EnemyState.InFormation:
                    UpdateInFormation(dt);
                    break;
                case EnemyState.Diving:
                    UpdateDiving(dt);
                    break;
                case EnemyState.Hovering:
                    UpdateHovering(dt);
                    break;
            }
        }

        private void UpdateEntering(float dt)
        {
            if (_path == null)
            {
                EnterFormation();
                return;
            }

            _pathProgress += dt / Mathf.Max(0.25f, entryDuration);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_pathProgress));
            transform.position = _path.Evaluate(t);

            if (_pathProgress >= 1f) EnterFormation();
        }

        private void EnterFormation()
        {
            _state = EnemyState.InFormation;
            transform.rotation = Quaternion.identity;
            if (_formation != null) _formation.Register(this);
        }

        private void UpdateInFormation(float dt)
        {
            if (_formation == null)
            {
                DespawnQuietly();
                return;
            }

            Vector3 slot = _formation.GetSlotPosition(_slotIndex);
            transform.position = Vector3.Lerp(transform.position, slot, Mathf.Clamp01(dt * 7f));

            _eggTimer -= dt;
            if (_eggTimer <= 0f)
            {
                DropEgg(Vector2.down * 4.6f);
                _eggTimer = Random.Range(_eggInterval * 0.7f, _eggInterval * 1.35f);
            }
        }

        private void UpdateDiving(float dt)
        {
            transform.position += (Vector3)(_diveDirection * (diveSpeed * dt));
            transform.Rotate(0f, 0f, 260f * dt);

            if (IsBelowScreen())
            {
                // Missed the player: wrap back to the top and re-enter the formation.
                if (_formation == null)
                {
                    DespawnQuietly();
                    return;
                }

                Vector3 slot = _formation.GetSlotPosition(_slotIndex);
                float top = CameraTop() + 1.6f;
                Vector3 start = new Vector3(slot.x, top, 0f);
                _path = new WaypointPath(start,
                    new Vector3(slot.x + 1.7f, top - 2.2f, 0f),
                    new Vector3(slot.x - 1.2f, Mathf.Lerp(top, slot.y, 0.7f), 0f),
                    slot);
                _pathProgress = 0f;
                transform.position = start;
                transform.rotation = Quaternion.identity;
                _state = EnemyState.Entering;
            }
        }

        private void UpdateHovering(float dt)
        {
            _hoverPhase += dt * 1.6f;
            Vector3 target = _hoverTarget + new Vector3(Mathf.Sin(_hoverPhase) * 1.1f, Mathf.Cos(_hoverPhase * 0.8f) * 0.35f, 0f);
            transform.position = Vector3.Lerp(transform.position, target, Mathf.Clamp01(dt * hoverSpeed));

            _eggTimer -= dt;
            if (_eggTimer <= 0f)
            {
                DropEgg(Vector2.down * 4.2f);
                _eggTimer = Random.Range(_eggInterval * 0.8f, _eggInterval * 1.4f);
            }

            _minionDiveTimer -= dt;
            if (_minionDiveTimer <= 0f)
            {
                StartDive();
            }
        }

        private void DropEgg(Vector2 velocity)
        {
            if (ObjectPooler.Instance == null) return;

            var eggObject = ObjectPooler.Instance.Spawn(eggPoolKey, transform.position + Vector3.down * 0.45f, Quaternion.identity);
            if (eggObject == null) return;

            var egg = eggObject.GetComponent<EnemyEgg>();
            if (egg != null) egg.Launch(velocity);

            AudioCue.Play(AudioLibrary.Sfx.ChickenCluck, 0.14f);
        }

        // ------------------------------------------------------------ combat

        public void StartDive()
        {
            if (_state != EnemyState.InFormation && _state != EnemyState.Hovering) return;

            _state = EnemyState.Diving;
            if (_formation != null) _formation.Unregister(this);

            Vector3 playerPosition = PlayerLocator.PlayerPositionOr(new Vector3(0f, -4f, 0f));
            Vector2 direction = playerPosition - transform.position;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;

            direction.Normalize();
            if (direction.y > -0.55f)
            {
                direction.y = -0.55f;
                direction.x = Mathf.Clamp(direction.x, -0.95f, 0.95f);
                direction.Normalize();
            }

            _diveDirection = direction;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        public void TakeDamage(float amount)
        {
            if (_removed) return;

            _health -= amount;
            _flashTimer = 0.08f;

            if (_health <= 0f) Die(true);
        }

        private void UpdateFlash(float dt)
        {
            if (_flashTimer <= 0f || spriteRenderer == null) return;

            spriteRenderer.color = new Color(1.9f, 1.9f, 1.9f, 1f);
            _flashTimer -= dt;

            if (_flashTimer <= 0f) spriteRenderer.color = _baseColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            var player = other.GetComponentInParent<PlayerHealth>(true);
            if (player == null) return;

            player.TakeDamage();

            // A diving chicken is a kamikaze - it dies on impact (the player handles its own damage).
            if (_state == EnemyState.Diving) Die(false);
        }

        private void Die(bool awardScore)
        {
            if (_removed) return;
            _removed = true;

            if (EffectsManager.Instance != null) EffectsManager.Instance.FeatherSplat(transform.position);
            AudioCue.Play(AudioLibrary.Sfx.ChickenDeath, 0.1f);

            if (awardScore && GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(Mathf.RoundToInt(_score));
                RollDrop();
            }

            if (_formation != null)
            {
                _formation.Unregister(this);
                _formation = null;
            }

            if (_waveManager != null) _waveManager.OnEnemyRemoved(this);

            _state = EnemyState.Idle;
            _path = null;

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private void DespawnQuietly()
        {
            if (!_removed)
            {
                _removed = true;
                if (_waveManager != null) _waveManager.OnEnemyRemoved(this);
            }

            if (_formation != null)
            {
                _formation.Unregister(this);
                _formation = null;
            }

            _state = EnemyState.Idle;
            _path = null;
            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private void RollDrop()
        {
            float roll = Random.value;
            string key = null;

            if (roll < 0.45f) key = "Pickup_Drumstick";
            else if (roll < 0.65f) key = "Pickup_Burger";
            else if (roll < 0.77f) key = "Pickup_Gift";

            if (key == null || ObjectPooler.Instance == null) return;
            ObjectPooler.Instance.Spawn(key, transform.position, Quaternion.identity);
        }

        private void HandleScreenClear()
        {
            if (_removed || !gameObject.activeInHierarchy) return;
            Die(true);
        }

        private bool IsBelowScreen()
        {
            var camera = Camera.main;
            if (camera == null) return transform.position.y < -8f;
            return transform.position.y < camera.transform.position.y - camera.orthographicSize - 1.4f;
        }

        private float CameraTop()
        {
            var camera = Camera.main;
            if (camera == null) return 7f;
            return camera.transform.position.y + camera.orthographicSize;
        }
    }
}
