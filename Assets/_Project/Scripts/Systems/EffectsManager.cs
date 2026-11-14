using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Spawns pooled effects by key so gameplay code never handles particle setup.</summary>
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }

        public const string ExplosionKey = "VFX_Explosion";
        public const string FeatherSplatKey = "VFX_FeatherSplat";
        public const string EggCrackKey = "VFX_EggCrack";
        public const string ShockwaveKey = "VFX_Shockwave";

        public static EffectsManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[EffectsManager]");
            go.AddComponent<EffectsManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Explosion(Vector3 position, Color color, float scale = 1f)
        {
            Spawn(ExplosionKey, position, color, scale, 0.7f);
        }

        public void FeatherSplat(Vector3 position)
        {
            Spawn(FeatherSplatKey, position, new Color(1f, 0.95f, 0.85f), 1f, 0.6f);
        }

        public void EggCrack(Vector3 position)
        {
            Spawn(EggCrackKey, position, new Color(1f, 0.98f, 0.85f), 1f, 0.45f);
        }

        public void Shockwave(Vector3 position, Color color, float scale = 1f)
        {
            Spawn(ShockwaveKey, position, color, scale, 0.55f);
        }

        public void HitSpark(Vector3 position, Color color)
        {
            Spawn(ExplosionKey, position, color, 0.4f, 0.3f);
        }

        private void Spawn(string key, Vector3 position, Color color, float scale, float lifetime)
        {
            if (ObjectPooler.Instance == null) return;

            var instance = ObjectPooler.Instance.Spawn(key, position, Quaternion.identity);
            if (instance == null) return;

            var vfx = instance.GetComponent<PooledVfx>();
            if (vfx == null) return;

            vfx.lifetime = lifetime;
            vfx.Play(color, scale);
        }
    }
}
