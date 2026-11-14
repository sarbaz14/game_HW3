using UnityEngine;

namespace ChickenShooter
{
    public enum FirePattern
    {
        Single,
        DoubleParallel,
        TripleSpread
    }

    [CreateAssetMenu(menuName = "ChickenShooter/WeaponData", fileName = "WeaponData")]
    public class WeaponDataSO : ScriptableObject
    {
        public string weaponName = "Blaster";
        [Range(1, 3)] public int powerLevel = 1;
        public Sprite projectileSprite;
        public string projectilePoolKey = "Laser1";
        public float damage = 1f;
        public float projectileSpeed = 16f;
        public float fireRate = 5f;
        public FirePattern pattern = FirePattern.Single;
        public float spreadAngle = 15f;
    }
}
