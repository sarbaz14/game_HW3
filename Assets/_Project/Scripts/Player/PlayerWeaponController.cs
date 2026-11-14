using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Three-tier weapon driven by WeaponDataSO assets, plus the screen-clearing Super Missile.
    /// </summary>
    public class PlayerWeaponController : MonoBehaviour
    {
        public const string SuperMissilePoolKey = "SuperMissile";

        public float muzzleOffset = 0.8f;
        public float lateralOffset = 0.25f;
        public float missileCooldown = 0.9f;

        private float _nextFireTime;
        private float _nextMissileTime;

        public int PowerLevel
        {
            get
            {
                var gameManager = GameManager.Instance;
                int level = gameManager != null ? gameManager.WeaponPowerLevel : 1;
                var weapons = ConfigProvider.Config != null ? ConfigProvider.Config.weapons : null;
                int max = weapons != null && weapons.Length > 0 ? weapons.Length : 3;
                return Mathf.Clamp(level, 1, max);
            }
        }

        public WeaponDataSO CurrentWeapon
        {
            get
            {
                var weapons = ConfigProvider.Config != null ? ConfigProvider.Config.weapons : null;
                if (weapons == null || weapons.Length == 0) return null;

                int index = Mathf.Clamp(PowerLevel - 1, 0, weapons.Length - 1);
                return weapons[index];
            }
        }

        private void Start()
        {
            GameEvents.RaiseWeaponPowerChanged(PowerLevel);
        }

        public void TryFire()
        {
            var weapon = CurrentWeapon;
            if (weapon == null) return;
            if (Time.time < _nextFireTime) return;

            _nextFireTime = Time.time + 1f / Mathf.Max(0.1f, weapon.fireRate);
            FirePatternFor(weapon);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioLibrary.Sfx.Laser, 0.09f);
        }

        private void FirePatternFor(WeaponDataSO weapon)
        {
            Vector3 origin = transform.position + Vector3.up * muzzleOffset;

            switch (weapon.pattern)
            {
                case FirePattern.DoubleParallel:
                    SpawnShot(weapon, origin + Vector3.right * -lateralOffset, 0f);
                    SpawnShot(weapon, origin + Vector3.right * lateralOffset, 0f);
                    break;

                case FirePattern.TripleSpread:
                    SpawnShot(weapon, origin, -weapon.spreadAngle);
                    SpawnShot(weapon, origin, 0f);
                    SpawnShot(weapon, origin, weapon.spreadAngle);
                    break;

                default:
                    SpawnShot(weapon, origin, 0f);
                    break;
            }
        }

        private void SpawnShot(WeaponDataSO weapon, Vector3 position, float angleDegrees)
        {
            if (ObjectPooler.Instance == null) return;

            Quaternion rotation = Quaternion.Euler(0f, 0f, angleDegrees);
            var projectile = ObjectPooler.Instance.Spawn(weapon.projectilePoolKey, position, rotation);
            if (projectile == null) return;

            var shot = projectile.GetComponent<Projectile>();
            if (shot != null)
            {
                shot.Configure(weapon.damage, weapon.projectileSpeed, weapon.projectileSprite,
                    rotation * Vector2.up);
            }
        }

        public void UpgradeWeapon()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.UpgradeWeapon();
        }

        public void FireSuperMissile()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.IsGameOver || gameManager.IsVictory) return;
            if (Time.time < _nextMissileTime) return;
            if (gameManager.SecondaryMissiles <= 0)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioLibrary.Sfx.EggCrack, 0.1f);
                return;
            }

            if (!gameManager.SpendMissile()) return;

            _nextMissileTime = Time.time + missileCooldown;

            Vector3 spawn = transform.position + Vector3.up * muzzleOffset;
            var missile = ObjectPooler.Instance != null
                ? ObjectPooler.Instance.Spawn(SuperMissilePoolKey, spawn, Quaternion.identity)
                : null;

            if (missile == null)
            {
                Debug.LogWarning("[PlayerWeaponController] Super missile pool missing - detonating immediately.");
                ScreenClearUtility.Detonate(spawn);
            }
        }
    }

    /// <summary>Shared Super Missile payload: insta-kills regular enemies/eggs/minions, hurts bosses.</summary>
    public static class ScreenClearUtility
    {
        public static void Detonate(Vector3 position)
        {
            if (EffectsManager.Instance != null)
            {
                EffectsManager.Instance.Shockwave(position, new Color(0.55f, 0.9f, 1f), 1.4f);
                EffectsManager.Instance.Explosion(position, new Color(1f, 0.85f, 0.4f), 2.2f);
            }

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.45f, 0.5f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioLibrary.Sfx.MissileExplosion, 0.02f);

            // All pooled gameplay hazards listen to this event:
            // - regular chickens + boss minions die instantly (score awarded)
            // - eggs and asteroids are wiped
            // - the active boss takes 18% of its max HP
            GameEvents.RaiseScreenClearTriggered();
        }
    }
}
