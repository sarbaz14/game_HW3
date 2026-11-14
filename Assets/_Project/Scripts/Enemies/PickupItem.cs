using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Falling food/gift pickup. Food fills the missile meter, gift boxes upgrade the weapon.</summary>
    public class PickupItem : MonoBehaviour, IPooledObject
    {
        public enum PickupType
        {
            Drumstick,
            Burger,
            GiftBox
        }

        public PickupType pickupType = PickupType.Drumstick;
        public float fallSpeed = 3f;
        public int scoreValue = 50;
        public float foodValue = 12.5f;
        public SpriteRenderer spriteRenderer;

        private float _wobblePhase;
        private Camera _camera;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void OnObjectSpawn()
        {
            _wobblePhase = Random.Range(0f, Mathf.PI * 2f);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        public void OnObjectReturn()
        {
        }

        private void Update()
        {
            _wobblePhase += Time.deltaTime * 3f;
            transform.position += new Vector3(Mathf.Sin(_wobblePhase) * 0.35f * Time.deltaTime, -fallSpeed * Time.deltaTime, 0f);
            transform.Rotate(0f, 0f, 60f * Time.deltaTime);

            if (IsOffScreen())
            {
                if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            Collect();
        }

        private void Collect()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.AddScore(scoreValue);

                if (pickupType == PickupType.GiftBox)
                {
                    gameManager.UpgradeWeapon();
                    AudioCue.Play(AudioLibrary.Sfx.PickupGift, 0.03f);
                    if (EffectsManager.Instance != null)
                        EffectsManager.Instance.HitSpark(transform.position, new Color(1f, 0.85f, 0.35f));
                }
                else
                {
                    gameManager.AddFoodProgress(foodValue);
                    AudioCue.Play(AudioLibrary.Sfx.PickupFood, 0.06f);
                }
            }

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private bool IsOffScreen()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return transform.position.y < -8f;

            Vector3 viewport = _camera.WorldToViewportPoint(transform.position);
            return viewport.y < -0.2f || viewport.x < -0.25f || viewport.x > 1.25f;
        }
    }
}
