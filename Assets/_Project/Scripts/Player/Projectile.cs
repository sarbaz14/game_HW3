using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Pooled player laser bolt.</summary>
    public class Projectile : MonoBehaviour, IPooledObject
    {
        public float speed = 16f;
        public float damage = 1f;
        public float lifeTime = 4f;

        private Vector2 _direction = Vector2.up;
        private float _life;
        private SpriteRenderer _spriteRenderer;
        private Camera _camera;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Configure(float damageAmount, float speedValue, Sprite sprite, Vector2 direction)
        {
            damage = Mathf.Max(0.01f, damageAmount);
            speed = Mathf.Max(0.1f, speedValue);

            if (direction.sqrMagnitude > 0.0001f) _direction = direction.normalized;
            else _direction = Vector2.up;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (sprite != null && _spriteRenderer != null) _spriteRenderer.sprite = sprite;

            // Slight HDR boost so URP Bloom makes the bolt glow.
            if (_spriteRenderer != null) _spriteRenderer.color = new Color(1.6f, 1.6f, 1.6f, 1f);
        }

        public void OnObjectSpawn()
        {
            _life = 0f;
        }

        public void OnObjectReturn()
        {
            if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
        }

        private void Update()
        {
            transform.position += (Vector3)(_direction * (speed * Time.deltaTime));

            _life += Time.deltaTime;
            if (_life > lifeTime || IsOffScreen()) Despawn();
        }

        private bool IsOffScreen()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Vector3 viewport = _camera.WorldToViewportPoint(transform.position);
            return viewport.y > 1.15f || viewport.y < -0.2f || viewport.x < -0.2f || viewport.x > 1.2f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            // Never damage the player ship with player projectiles.
            if (other.GetComponentInParent<PlayerHealth>() != null) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;

            target.TakeDamage(damage);
            Despawn();
        }

        private void Despawn()
        {
            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
