using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Creates every persistent service on demand. Because each service exposes a null-safe
    /// EnsureInstance(), pressing Play directly inside Level1/2/3 initialises cleanly.
    /// </summary>
    public static class SystemsBootstrap
    {
        private static bool _installed;

        public static void Ensure()
        {
            if (_installed && GameManager.Instance != null && AudioManager.Instance != null && ObjectPooler.Instance != null) return;

            GameManager.EnsureInstance();
            AudioManager.EnsureInstance();
            ObjectPooler.EnsureInstance();
            EffectsManager.EnsureInstance();

            PhysicsSetup.Configure();

            _installed = true;
        }
    }

    /// <summary>Applies deterministic 2D physics settings at runtime (gravity-free top-down shooter).</summary>
    public static class PhysicsSetup
    {
        private static bool _applied;

        public static void Configure()
        {
            if (_applied) return;
            _applied = true;

            Physics2D.gravity = Vector2.zero;

            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            int playerShot = LayerMask.NameToLayer("PlayerProjectile");
            int enemyShot = LayerMask.NameToLayer("EnemyProjectile");
            int pickup = LayerMask.NameToLayer("Pickup");

            Ignore(playerShot, player);
            Ignore(playerShot, playerShot);
            Ignore(playerShot, pickup);
            Ignore(playerShot, enemyShot);
            Ignore(enemyShot, enemyShot);
            Ignore(enemyShot, enemy);
            Ignore(pickup, enemy);
            Ignore(pickup, playerShot);
            Ignore(pickup, pickup);

            // Keep the 3 trigger layers interacting where gameplay needs it:
            // player <-> enemy/enemyShot/pickup, playerShot <-> enemy/enemyShot.
            _ = enemy;
        }

        private static void Ignore(int layerA, int layerB)
        {
            if (layerA < 0 || layerB < 0) return;
            Physics2D.IgnoreLayerCollision(layerA, layerB, true);
        }
    }
}
