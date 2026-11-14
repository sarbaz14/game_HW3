using System.Collections;
using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Player lives, invincibility frames and weapon downgrade feedback.</summary>
    public class PlayerHealth : MonoBehaviour
    {
        public float invulnerabilityDuration = 2f;
        public float flashInterval = 0.1f;
        public SpriteRenderer spriteRenderer;
        public Collider2D hitCollider;

        private bool _invulnerable;
        private Coroutine _routine;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (hitCollider == null) hitCollider = GetComponent<Collider2D>();
            _invulnerable = false;
        }

        private void OnEnable()
        {
            StopInvulnerability();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsHazard(other)) return;
            TakeDamage();
        }

        public void TakeDamage()
        {
            if (_invulnerable) return;

            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.IsGameOver || gameManager.IsVictory) return;
            if (gameManager.RemainingLives <= 0) return;

            var activeBoss = Object.FindAnyObjectByType<BossController>();
            if (activeBoss != null && activeBoss.gameObject.activeInHierarchy && activeBoss.IsDead) return;

            gameManager.LoseLife();
            gameManager.DowngradeWeapon();

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioLibrary.Sfx.PlayerHit, 0.02f);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.32f, 0.38f);
            if (EffectsManager.Instance != null)
                EffectsManager.Instance.Explosion(transform.position, new Color(0.4f, 0.9f, 1f), 1.3f);

            GameEvents.RaisePlayerDamaged();

            if (gameManager.RemainingLives > 0) StartInvulnerability();
        }

        public void Heal(int amount = 1)
        {
            // Reserved for future pickups - lives are restored only by retrying a level.
            if (amount <= 0) return;
        }

        private static bool IsHazard(Collider2D other)
        {
            if (other == null) return false;
            if (other.GetComponentInParent<EnemyController>(true) != null) return true;
            if (other.GetComponentInParent<EnemyEgg>(true) != null) return true;
            if (other.GetComponentInParent<Asteroid>(true) != null) return true;
            if (other.GetComponentInParent<EnergyFeatherProjectile>(true) != null) return true;
            if (other.GetComponentInParent<BossController>(true) != null) return true;
            return false;
        }

        private void StartInvulnerability()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(InvulnerabilityRoutine());
        }

        private void StopInvulnerability()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _invulnerable = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            _invulnerable = true;
            float elapsed = 0f;

            while (elapsed < invulnerabilityDuration)
            {
                if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }

            if (spriteRenderer != null) spriteRenderer.enabled = true;
            _invulnerable = false;
            _routine = null;
        }

        public bool IsInvulnerable => _invulnerable;
    }
}
