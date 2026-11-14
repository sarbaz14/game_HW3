using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Glowing boss projectile fired during dive strikes and the enraged phase.</summary>
    public class EnergyFeatherProjectile : MonoBehaviour, IPooledObject, IDamageable
    {
        public float speed = 5.5f;
        public float health = 1f;
        public SpriteRenderer spriteRenderer;

        private Vector2 _direction = Vector2.down;
        private Camera _camera;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void OnObjectSpawn()
        {
            health = 1f;
            _direction = Vector2.down;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1.5f, 1.2f, 2.2f, 1f);
        }

        public void OnObjectReturn()
        {
        }

        public void Configure(Vector2 direction, float speedValue)
        {
            if (direction.sqrMagnitude > 0.0001f) _direction = direction.normalized;
            speed = Mathf.Max(0.5f, speedValue);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f);
        }

        private void Update()
        {
            transform.position += (Vector3)(_direction * (speed * Time.deltaTime));
            transform.Rotate(0f, 0f, 180f * Time.deltaTime);

            if (IsOffScreen())
            {
                if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
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

            if (EffectsManager.Instance != null) EffectsManager.Instance.HitSpark(transform.position, new Color(0.8f, 0.6f, 1f));
            Despawn();
        }

        private void Despawn()
        {
            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private bool IsOffScreen()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return transform.position.y < -8f;

            Vector3 viewport = _camera.WorldToViewportPoint(transform.position);
            return viewport.y < -0.25f || viewport.y > 1.25f || viewport.x < -0.25f || viewport.x > 1.25f;
        }
    }
}
