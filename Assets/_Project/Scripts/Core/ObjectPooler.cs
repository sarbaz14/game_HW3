using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChickenShooter
{
    /// <summary>
    /// String-keyed object pool. Pre-warms instances from the GameConfig definitions,
    /// expands on demand and notifies pooled components on spawn/return.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
        private readonly HashSet<GameObject> _activeObjects = new HashSet<GameObject>();
        private Transform _root;
        private bool _initialised;
        private bool _sceneHookAttached;

        public static ObjectPooler EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[ObjectPooler]");
            go.AddComponent<ObjectPooler>();
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
            EnsureRoot();
            InitialiseFromConfig();

            if (!_sceneHookAttached)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                _sceneHookAttached = true;
            }
        }

        private void OnDestroy()
        {
            if (_sceneHookAttached)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _sceneHookAttached = false;
            }

            if (Instance == this) Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DespawnAll();
        }

        private void EnsureRoot()
        {
            if (_root == null)
            {
                var child = transform.Find("PooledObjects");
                if (child != null)
                {
                    _root = child;
                }
                else
                {
                    var rootGo = new GameObject("PooledObjects");
                    rootGo.transform.SetParent(transform, false);
                    _root = rootGo.transform;
                }
            }

            _root.gameObject.SetActive(true);
        }

        public void InitialiseFromConfig()
        {
            if (_initialised) return;
            _initialised = true;

            var config = ConfigProvider.Config;
            if (config == null || config.pools == null) return;

            foreach (var definition in config.pools)
            {
                if (definition == null || definition.prefab == null || string.IsNullOrEmpty(definition.key)) continue;
                RegisterPool(definition.key, definition.prefab, definition.initialSize);
            }
        }

        public void RegisterPool(string key, GameObject prefab, int initialSize)
        {
            if (string.IsNullOrEmpty(key) || prefab == null) return;
            if (_pools.ContainsKey(key)) return;

            EnsureRoot();

            var queue = new Queue<GameObject>(Mathf.Max(1, initialSize));
            _pools[key] = queue;
            _prefabs[key] = prefab;

            for (int i = 0; i < Mathf.Max(0, initialSize); i++)
            {
                var instance = Instantiate(prefab, _root);
                instance.name = prefab.name + " (pooled)";
                var identity = instance.GetComponent<PooledObjectIdentity>();
                if (identity == null) identity = instance.AddComponent<PooledObjectIdentity>();
                identity.poolKey = key;
                instance.SetActive(false);
                queue.Enqueue(instance);
            }
        }

        public bool HasPool(string key) => !string.IsNullOrEmpty(key) && _pools.ContainsKey(key);

        public GameObject Spawn(string key, Vector3 position) => Spawn(key, position, Quaternion.identity);

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(key)) return null;

            EnsureRoot();

            if (!_pools.ContainsKey(key))
            {
                if (_prefabs.TryGetValue(key, out var knownPrefab) && knownPrefab != null)
                {
                    RegisterPool(key, knownPrefab, 1);
                }
                else
                {
                    Debug.LogWarning("[ObjectPooler] No pool registered for key '" + key + "'.");
                    return null;
                }
            }

            var pool = _pools[key];
            GameObject instance = null;

            while (pool.Count > 0 && instance == null)
            {
                instance = pool.Dequeue();
            }

            if (instance == null) instance = ExpandPool(key);
            if (instance == null) return null;

            instance.transform.SetParent(null, false);
            instance.transform.SetPositionAndRotation(position, rotation);

            _activeObjects.Add(instance);

            if (!instance.activeSelf) instance.SetActive(true);

            var pooled = instance.GetComponents<IPooledObject>();
            for (int i = 0; i < pooled.Length; i++) pooled[i].OnObjectSpawn();

            return instance;
        }

        private GameObject ExpandPool(string key)
        {
            EnsureRoot();
            var prefab = _prefabs[key];
            if (prefab == null) return null;

            var instance = Instantiate(prefab, _root);
            instance.name = prefab.name + " (pooled)";
            var identity = instance.GetComponent<PooledObjectIdentity>();
            if (identity == null) identity = instance.AddComponent<PooledObjectIdentity>();
            identity.poolKey = key;
            instance.SetActive(false);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            _activeObjects.Remove(instance);

            EnsureRoot();

            var identity = instance.GetComponent<PooledObjectIdentity>();
            if (identity == null || string.IsNullOrEmpty(identity.poolKey) || !_pools.ContainsKey(identity.poolKey))
            {
                Destroy(instance);
                return;
            }

            var pooled = instance.GetComponents<IPooledObject>();
            for (int i = 0; i < pooled.Length; i++) pooled[i].OnObjectReturn();

            instance.SetActive(false);
            instance.transform.SetParent(_root, false);
            _pools[identity.poolKey].Enqueue(instance);
        }

        public void DespawnAll()
        {
            EnsureRoot();

            if (_activeObjects.Count > 0)
            {
                var activeList = new List<GameObject>(_activeObjects);
                _activeObjects.Clear();

                for (int i = 0; i < activeList.Count; i++)
                {
                    var obj = activeList[i];
                    if (obj == null) continue;

                    var identity = obj.GetComponent<PooledObjectIdentity>();
                    if (identity == null || string.IsNullOrEmpty(identity.poolKey) || !_pools.ContainsKey(identity.poolKey))
                    {
                        Destroy(obj);
                        continue;
                    }

                    var pooled = obj.GetComponents<IPooledObject>();
                    for (int p = 0; p < pooled.Length; p++) pooled[p].OnObjectReturn();

                    obj.SetActive(false);
                    obj.transform.SetParent(_root, false);
                    _pools[identity.poolKey].Enqueue(obj);
                }
            }

            if (_root != null)
            {
                for (int i = 0; i < _root.childCount; i++)
                {
                    var child = _root.GetChild(i);
                    if (child != null && child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>Spawns already configured objects without a key lookup (fallback path).</summary>
        public GameObject SpawnDirect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            return Instantiate(prefab, position, rotation);
        }
    }
}
