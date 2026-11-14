using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Travels to the centre of the screen (0, 1.5) then detonates, clearing the screen.
    /// </summary>
    public class SuperMissile : MonoBehaviour, IPooledObject
    {
        public float travelTime = 0.45f;
        public Vector3 target = new Vector3(0f, 1.5f, 0f);

        private Vector3 _startPosition;
        private float _progress;
        private bool _detonated;

        public void OnObjectSpawn()
        {
            _startPosition = transform.position;
            _progress = 0f;
            _detonated = false;
        }

        public void OnObjectReturn()
        {
            _detonated = false;
        }

        private void Update()
        {
            if (_detonated) return;

            _progress += Time.deltaTime / Mathf.Max(0.01f, travelTime);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_progress));
            transform.position = Vector3.Lerp(_startPosition, target, eased);
            transform.Rotate(0f, 0f, 540f * Time.deltaTime);

            if (_progress >= 1f) Detonate();
        }

        private void Detonate()
        {
            if (_detonated) return;
            _detonated = true;

            ScreenClearUtility.Detonate(transform.position);

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
