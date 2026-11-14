using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Falling, destructible egg. Can be shot out of the air.</summary>
    public class EnemyEgg : MonoBehaviour, IPooledObject, IDamageable
    {
        public float fallSpeed = 4.6f;
        public float health = 1f;
        public SpriteRenderer spriteRenderer;

        private Vector2 _velocity = Vector2.down;
        private float _spin;
        private Camera _camera;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
            health = 1f;
            _velocity = Vector2.down * fallSpeed;
            _spin = Random.Range(-140f, 140f);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        public void OnObjectReturn()
        {
        }

        public void Launch(Vector2 velocity)
        {
            _velocity = velocity.sqrMagnitude > 0.0001f ? velocity : Vector2.down * fallSpeed;
        }

        private void Update()
        {
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, _spin * Time.deltaTime);

            if (IsOffScreen()) Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            var player = other.GetComponentInParent<PlayerHealth>(true);
            if (player != null)
            {
                player.TakeDamage();
                Despawn();
            }
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
            if (health > 0f) return;

            if (EffectsManager.Instance != null) EffectsManager.Instance.EggCrack(transform.position);
            AudioCue.Play(AudioLibrary.Sfx.EggCrack, 0.1f);
            Despawn();
        }

        private void HandleScreenClear()
        {
            if (!gameObject.activeInHierarchy) return;
            Despawn();
        }

        private bool IsOffScreen()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return transform.position.y < -8f;

            Vector3 viewport = _camera.WorldToViewportPoint(transform.position);
            return viewport.y < -0.2f || viewport.y > 1.2f || viewport.x < -0.2f || viewport.x > 1.2f;
        }

        private void Despawn()
        {
            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
