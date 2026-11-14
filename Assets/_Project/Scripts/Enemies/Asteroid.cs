using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Rotating, destructible asteroid used by AsteroidBelt rounds.</summary>
    public class Asteroid : MonoBehaviour, IPooledObject, IDamageable
    {
        public float health = 2f;
        public float fallSpeed = 2.2f;
        public int scoreValue = 30;
        public SpriteRenderer spriteRenderer;

        private Vector2 _velocity = Vector2.down;
        private float _spin;
        private float _flashTimer;
        private WaveManager _waveManager;
        private bool _resolved;
        private Color _baseColor = Color.white;
        private Camera _camera;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) _baseColor = spriteRenderer.color;
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
            _resolved = false;
            _flashTimer = 0f;
            _velocity = Vector2.down * fallSpeed;
            _spin = Random.Range(-45f, 45f);
            if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        }

        public void OnObjectReturn()
        {
            _waveManager = null;
        }

        public void Initialize(WaveDataSO wave, WaveManager waveManager, Vector2 velocity)
        {
            _waveManager = waveManager;
            health = Mathf.Max(1f, wave.asteroidHealth);
            scoreValue = Mathf.RoundToInt(wave.scoreValue * 0.6f);
            _velocity = velocity.sqrMagnitude > 0.0001f ? velocity : Vector2.down * wave.asteroidSpeed;
            _spin = Random.Range(-60f, 60f);
            _resolved = false;
        }

        private void Update()
        {
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, _spin * Time.deltaTime);
            UpdateFlash(Time.deltaTime);

            if (IsOffScreen()) Despawn(false);
        }

        public void TakeDamage(float amount)
        {
            if (_resolved) return;

            health -= amount;
            _flashTimer = 0.08f;

            if (health <= 0f) Despawn(true);
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
            // Player damage is handled by PlayerHealth; the asteroid shatters on impact.
            if (other == null) return;
            var player = other.GetComponentInParent<PlayerHealth>(true);
            if (player == null) return;
            player.TakeDamage();
            Despawn(false);
        }

        private void Despawn(bool destroyedByPlayer)
        {
            if (_resolved) return;
            _resolved = true;

            if (destroyedByPlayer)
            {
                if (EffectsManager.Instance != null) EffectsManager.Instance.Explosion(transform.position, new Color(1f, 0.7f, 0.35f), 1f);
                AudioCue.Play(AudioLibrary.Sfx.EggCrack, 0.15f);
                if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);
            }

            if (_waveManager != null) _waveManager.OnAsteroidRemoved();

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private void HandleScreenClear()
        {
            if (!gameObject.activeInHierarchy || _resolved) return;
            Despawn(true);
        }

        private bool IsOffScreen()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return transform.position.y < -8f;

            Vector3 viewport = _camera.WorldToViewportPoint(transform.position);
            return viewport.y < -0.25f || viewport.x < -0.3f || viewport.x > 1.3f;
        }
    }
}
