using System.Collections;
using UnityEngine;

namespace ChickenShooter
{
    [RequireComponent(typeof(Camera))]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _baseLocalPosition;
        private Coroutine _routine;
        private float _currentMagnitude;

        private void Awake()
        {
            Instance = this;
            _baseLocalPosition = transform.localPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Shake(float duration = 0.2f, float magnitude = 0.2f)
        {
            if (!gameObject.activeInHierarchy) return;

            // Stronger shake wins instead of being cut short by a weaker one.
            if (_routine != null && _currentMagnitude > magnitude) return;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            _currentMagnitude = magnitude;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * strength;
                transform.localPosition = _baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            transform.localPosition = _baseLocalPosition;
            _currentMagnitude = 0f;
            _routine = null;
        }

        public void ResetPosition()
        {
            _baseLocalPosition = transform.localPosition;
        }
    }
}
