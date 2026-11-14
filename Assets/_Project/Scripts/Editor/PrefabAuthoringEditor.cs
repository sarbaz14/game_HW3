using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Creates every gameplay prefab with sprites, colliders, layers, tags and behaviours.</summary>
    public static class PrefabAuthoringEditor
    {
        [MenuItem("ChickenShooter/2. Build Prefabs", false, 20)]
        public static void BuildAllPrefabs()
        {
            // Tags/layers must exist before assigning them to prefabs.
            ProjectBootstrapEditor.ConfigureProject();

            BuildPlayer();
            BuildProjectiles();
            BuildEnemies();
            BuildBosses();
            BuildPickups();
            BuildVfx();

            AssetDatabase.SaveAssets();
            Debug.Log("[Prefabs] All gameplay prefabs built.");
        }

        // ---------------------------------------------------------------- player

        private static void BuildPlayer()
        {
            CreatePrefab(EditorAssets.PlayerPrefabDir + "/Player.prefab", root =>
            {
                root.tag = "Player";
                root.layer = SafeLayer("Player");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite("PlayerShip"), "Gameplay", 10);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.28f;
                collider.isTrigger = true;

                root.AddComponent<PlayerWeaponController>();
                root.AddComponent<PlayerHealth>();
                var controller = root.AddComponent<PlayerController>();

                var particleMaterial = EditorAssets.ParticleMaterial("StarParticle");
                controller.exhaustLeft = CreateExhaust(root.transform, new Vector3(-0.14f, -0.42f, 0f),
                    new Color(0.4f, 0.85f, 1f), particleMaterial);
                controller.exhaustRight = CreateExhaust(root.transform, new Vector3(0.14f, -0.42f, 0f),
                    new Color(0.4f, 0.85f, 1f), particleMaterial);
                controller.exhaust = controller.exhaustLeft;
            });
        }

        private static ParticleSystem CreateExhaust(Transform parent, Vector3 localPosition, Color color, Material material)
        {
            var exhaust = new GameObject("Exhaust");
            exhaust.transform.SetParent(parent, false);
            exhaust.transform.localPosition = localPosition;

            var system = exhaust.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.3f;
            main.startSpeed = 2.2f;
            main.startSize = 0.18f;
            main.startColor = color;
            main.gravityModifier = 0.05f;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 45f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(180f, 0f, 0f);

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.6f, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

            var renderer = exhaust.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.sortingLayerName = SafeSortingLayer("VFX");
            renderer.sortingOrder = 2;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            return system;
        }

        // ---------------------------------------------------------------- projectiles

        private static void BuildProjectiles()
        {
            for (int level = 1; level <= 3; level++)
            {
                int captured = level;
                CreatePrefab(EditorAssets.ProjectilePrefabDir + "/Laser_Lv" + level + ".prefab", root =>
                {
                    root.tag = "PlayerProjectile";
                    root.layer = SafeLayer("PlayerProjectile");

                    var spriteRenderer = root.AddComponent<SpriteRenderer>();
                    ConfigureSprite(spriteRenderer, EditorAssets.Sprite("Laser_Lv" + captured), "Projectiles", 20);

                    var body = root.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.gravityScale = 0f;

                    var collider = root.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(0.12f, 0.42f);
                    collider.isTrigger = true;

                    root.AddComponent<Projectile>();
                });
            }

            CreatePrefab(EditorAssets.ProjectilePrefabDir + "/SuperMissile.prefab", root =>
            {
                root.tag = "PlayerProjectile";
                root.layer = SafeLayer("PlayerProjectile");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite("SuperMissile"), "Projectiles", 24);
                spriteRenderer.color = new Color(1.7f, 1.7f, 1.7f, 1f);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;

                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.22f;
                collider.isTrigger = true;

                root.AddComponent<SuperMissile>();
            });
        }

        // ---------------------------------------------------------------- enemies

        private static void BuildEnemies()
        {
            for (int level = 1; level <= 3; level++)
            {
                int captured = level;

                CreatePrefab(EditorAssets.EnemyPrefabDir + "/Chicken_Lv" + level + ".prefab", root =>
                {
                    root.tag = "Enemy";
                    root.layer = SafeLayer("Enemy");
                    ConfigureEnemyRoot(root, "Chicken_Lv" + captured, 0.38f, 50f);
                });

                CreatePrefab(EditorAssets.EnemyPrefabDir + "/Minion_Lv" + level + ".prefab", root =>
                {
                    root.tag = "Enemy";
                    root.layer = SafeLayer("Enemy");
                    root.transform.localScale = Vector3.one * 0.72f;
                    ConfigureEnemyRoot(root, "Chicken_Lv" + captured, 0.38f, 75f);
                });
            }

            CreatePrefab(EditorAssets.EnemyPrefabDir + "/EnemyEgg.prefab", root =>
            {
                root.tag = "EnemyEgg";
                root.layer = SafeLayer("EnemyProjectile");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite("EnemyEgg"), "Projectiles", 12);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;

                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.2f;
                collider.isTrigger = true;

                root.AddComponent<EnemyEgg>();
            });

            CreatePrefab(EditorAssets.EnemyPrefabDir + "/EnergyFeather.prefab", root =>
            {
                root.tag = "EnemyEgg";
                root.layer = SafeLayer("EnemyProjectile");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite("EnergyFeather"), "Projectiles", 13);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;

                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.18f, 0.42f);
                collider.isTrigger = true;

                root.AddComponent<EnergyFeatherProjectile>();
            });

            CreatePrefab(EditorAssets.EnemyPrefabDir + "/Asteroid.prefab", root =>
            {
                root.tag = "Asteroid";
                root.layer = SafeLayer("Enemy");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite("Asteroid"), "Gameplay", 6);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;

                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.52f;
                collider.isTrigger = true;

                root.AddComponent<Asteroid>();
            });
        }

        private static void ConfigureEnemyRoot(GameObject root, string spriteName, float colliderRadius, float score)
        {
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            ConfigureSprite(spriteRenderer, EditorAssets.Sprite(spriteName), "Gameplay", 8);

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = colliderRadius;
            collider.isTrigger = true;

            var enemy = root.AddComponent<EnemyController>();
            enemy.spriteRenderer = spriteRenderer;
            enemy.baseScore = score;
        }

        // ---------------------------------------------------------------- bosses

        private static void BuildBosses()
        {
            CreateBoss("Boss_Lv1", "Giant Chicken Boss", 300f, 3.4f);
            CreateBoss("Boss_Lv2", "Mecha-Chicken Overlord", 520f, 3.8f);
            CreateBoss("Boss_Lv3", "Galactic Emperor", 900f, 4.2f);
        }

        private static void CreateBoss(string spriteName, string bossName, float health, float moveSpeed)
        {
            CreatePrefab(EditorAssets.BossPrefabDir + "/" + spriteName + ".prefab", root =>
            {
                root.tag = "Boss";
                root.layer = SafeLayer("Enemy");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite(spriteName), "Gameplay", 9);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;

                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 1.05f;
                collider.isTrigger = true;

                var boss = root.AddComponent<BossController>();
                boss.spriteRenderer = spriteRenderer;
                boss.bossName = bossName;
                boss.maxHealth = health;
                boss.moveSpeed = moveSpeed;
                boss.anchor = new Vector3(0f, 3f, 0f);
            });
        }

        // ---------------------------------------------------------------- pickups

        private static void BuildPickups()
        {
            CreatePickup("Pickup_Drumstick", "Food_Drumstick", PickupItem.PickupType.Drumstick, 50, 12.5f);
            CreatePickup("Pickup_Burger", "Food_Burger", PickupItem.PickupType.Burger, 120, 25f);
            CreatePickup("Pickup_Gift", "GiftBox", PickupItem.PickupType.GiftBox, 150, 0f);
        }

        private static void CreatePickup(string name, string spriteName, PickupItem.PickupType type, int score, float food)
        {
            CreatePrefab(EditorAssets.PickupPrefabDir + "/" + name + ".prefab", root =>
            {
                root.tag = "Pickup";
                root.layer = SafeLayer("Pickup");

                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite(spriteName), "Gameplay", 14);

                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;

                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.32f;
                collider.isTrigger = true;

                var pickup = root.AddComponent<PickupItem>();
                pickup.pickupType = type;
                pickup.scoreValue = score;
                pickup.foodValue = food;
                pickup.spriteRenderer = spriteRenderer;
            });
        }

        // ---------------------------------------------------------------- vfx

        private static void BuildVfx()
        {
            CreateBurst("VFX_Explosion", "StarParticle", 26, 6.5f, 0.3f, 0.7f, Color.white);
            CreateBurst("VFX_FeatherSplat", "FeatherParticle", 14, 3.4f, 0.28f, 0.6f, new Color(1f, 0.97f, 0.9f));
            CreateBurst("VFX_EggCrack", "StarParticle", 12, 3.2f, 0.2f, 0.45f, new Color(1f, 0.98f, 0.85f));

            CreatePrefab(EditorAssets.VfxPrefabDir + "/VFX_Shockwave.prefab", root =>
            {
                var spriteRenderer = root.AddComponent<SpriteRenderer>();
                ConfigureSprite(spriteRenderer, EditorAssets.Sprite("RingShockwave"), "VFX", 18);
                spriteRenderer.color = new Color(0.6f, 0.95f, 1f, 1f);

                var vfx = root.AddComponent<PooledVfx>();
                vfx.lifetime = 0.55f;
                vfx.growSprite = true;
                vfx.startScale = 0.3f;
                vfx.endScale = 2.8f;
            });
        }

        private static void CreateBurst(string name, string texture, int count, float speed, float size, float lifetime, Color color)
        {
            CreatePrefab(EditorAssets.VfxPrefabDir + "/" + name + ".prefab", root =>
            {
                var system = root.AddComponent<ParticleSystem>();

                var main = system.main;
                main.loop = false;
                main.playOnAwake = false;
                main.duration = lifetime;
                main.startLifetime = lifetime * 0.65f;
                main.startSpeed = speed;
                main.startSize = size;
                main.startColor = color;
                main.gravityModifier = 0.1f;
                main.maxParticles = 200;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = system.emission;
                emission.enabled = true;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

                var shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.08f;

                var colorOverLifetime = system.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

                var sizeOverLifetime = system.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.material = EditorAssets.ParticleMaterial(texture);
                renderer.sortingLayerName = SafeSortingLayer("VFX");
                renderer.sortingOrder = 15;

                var vfx = root.AddComponent<PooledVfx>();
                vfx.lifetime = lifetime;
                vfx.growSprite = false;
            });
        }

        // ---------------------------------------------------------------- helpers

        private static void ConfigureSprite(SpriteRenderer renderer, Sprite sprite, string sortingLayer, int order)
        {
            renderer.sprite = sprite;
            renderer.sharedMaterial = EditorAssets.SpriteMaterial();
            renderer.sortingLayerName = SafeSortingLayer(sortingLayer);
            renderer.sortingOrder = order;
            if (sprite == null) Debug.LogWarning("[Prefabs] Sprite missing for " + renderer.name);
        }

        public static GameObject CreatePrefab(string path, Action<GameObject> configure)
        {
            string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory)) ProceduralArt.EnsureFolder(directory);

            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            configure?.Invoke(root);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        public static int SafeLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }

        public static string SafeSortingLayer(string layerName)
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName) return layerName;
            }

            return "Default";
        }
    }
}
