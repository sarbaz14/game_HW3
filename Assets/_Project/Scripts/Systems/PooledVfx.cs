using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Pooled visual effect: single-burst particles and expanding sprites (shockwaves).</summary>
    public class PooledVfx : MonoBehaviour, IPooledObject
    {
        public float lifetime = 0.6f;
        public bool growSprite;
        public float startScale = 0.25f;
        public float endScale = 1.7f;

        private ParticleSystem[] _systems;
        private SpriteRenderer _sprite;
        private float _timer;
        private bool _active;
        private Color _baseColor = Color.white;

        private void Awake()
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        public void OnObjectSpawn()
        {
            _timer = 0f;
            _active = true;
        }

        public void OnObjectReturn()
        {
            _active = false;
            if (_systems == null) return;
            for (int i = 0; i < _systems.Length; i++)
            {
                if (_systems[i] == null) continue;
                _systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void Play(Color color, float scale)
        {
            _baseColor = color;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            if (_sprite != null && !growSprite) _sprite.color = color;

            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                {
                    var system = _systems[i];
                    if (system == null) continue;
                    var main = system.main;
                    main.startColor = color;
                    system.Clear();
                    system.Play();
                }
            }

            if (_sprite != null && growSprite)
            {
                _sprite.color = color;
                _sprite.transform.localScale = Vector3.one * startScale;
            }
        }

        private void Update()
        {
            if (!_active) return;

            _timer += Time.deltaTime;

            if (_sprite != null && growSprite)
            {
                float t = Mathf.Clamp01(_timer / Mathf.Max(0.01f, lifetime));
                float scale = Mathf.Lerp(startScale, endScale, t);
                _sprite.transform.localScale = Vector3.one * scale;
                var color = _baseColor;
                color.a = 1f - t;
                _sprite.color = color;
            }

            if (_timer >= lifetime)
            {
                _active = false;
                if (ObjectPooler.Instance != null) ObjectPooler.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
