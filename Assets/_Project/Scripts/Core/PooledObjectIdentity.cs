using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Remembers the pool key of an instantiated object so it can be returned correctly.</summary>
    [DisallowMultipleComponent]
    public class PooledObjectIdentity : MonoBehaviour
    {
        public string poolKey;
    }
}
