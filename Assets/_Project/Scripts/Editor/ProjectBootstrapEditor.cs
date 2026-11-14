using UnityEditor;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Project-wide settings: tags, layers, sorting layers, physics, player settings.</summary>
    public static class ProjectBootstrapEditor
    {
        public static readonly string[] RequiredTags =
        {
            "Player", "Enemy", "Boss", "PlayerProjectile", "EnemyEgg", "Asteroid", "Pickup"
        };

        public static readonly string[] RequiredLayers =
        {
            "Player", "Enemy", "PlayerProjectile", "EnemyProjectile", "Pickup"
        };

        public static readonly string[] RequiredSortingLayers =
        {
            "Background", "Midground", "Gameplay", "Projectiles", "VFX", "UI"
        };

        [MenuItem("ChickenShooter/0. Configure Project", false, 0)]
        public static void ConfigureProject()
        {
            AddTags(RequiredTags);
            AddLayers(RequiredLayers);
            AddSortingLayers(RequiredSortingLayers);
            SetPhysics2DGravityZero();
            ConfigurePlayerSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectBootstrap] Tags, layers, sorting layers, physics and player settings configured.");
        }

        // ---------------------------------------------------------------- tags & layers

        private static SerializedObject GetTagManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[ProjectBootstrap] Could not load ProjectSettings/TagManager.asset");
                return null;
            }

            return new SerializedObject(assets[0]);
        }

        private static void AddTags(string[] tags)
        {
            var manager = GetTagManager();
            if (manager == null) return;

            var tagsProperty = manager.FindProperty("tags");
            if (tagsProperty == null) return;

            foreach (string tag in tags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProperty.arraySize; i++)
                {
                    if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) continue;

                tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
                tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
            }

            manager.ApplyModifiedProperties();
        }

        private static void AddLayers(string[] layers)
        {
            var manager = GetTagManager();
            if (manager == null) return;

            var layersProperty = manager.FindProperty("layers");
            if (layersProperty == null) return;

            foreach (string layerName in layers)
            {
                bool exists = false;
                for (int i = 0; i < layersProperty.arraySize; i++)
                {
                    if (layersProperty.GetArrayElementAtIndex(i).stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) continue;

                for (int i = 8; i < layersProperty.arraySize; i++)
                {
                    var slot = layersProperty.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(slot.stringValue)) continue;

                    slot.stringValue = layerName;
                    break;
                }
            }

            manager.ApplyModifiedProperties();
        }

        private static void AddSortingLayers(string[] layers)
        {
            var manager = GetTagManager();
            if (manager == null) return;

            var sortingProperty = manager.FindProperty("m_SortingLayers");
            if (sortingProperty == null) return;

            foreach (string layerName in layers)
            {
                bool exists = false;
                for (int i = 0; i < sortingProperty.arraySize; i++)
                {
                    var nameProperty = sortingProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                    if (nameProperty != null && nameProperty.stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) continue;

                sortingProperty.InsertArrayElementAtIndex(sortingProperty.arraySize);
                var element = sortingProperty.GetArrayElementAtIndex(sortingProperty.arraySize - 1);
                element.FindPropertyRelative("name").stringValue = layerName;
                element.FindPropertyRelative("uniqueID").intValue = StableId(layerName);
            }

            manager.ApplyModifiedProperties();
        }

        private static int StableId(string name)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in name) hash = hash * 31 + c;
                if (hash == 0) hash = 1;
                return hash & 0x7FFFFFFF;
            }
        }

        private static void SetPhysics2DGravityZero()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (assets != null && assets.Length > 0)
            {
                var settings = new SerializedObject(assets[0]);
                var gravity = settings.FindProperty("m_Gravity");
                if (gravity != null)
                {
                    gravity.vector2Value = Vector2.zero;
                    settings.ApplyModifiedProperties();
                }
            }

            Physics2D.gravity = Vector2.zero;
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "ChickenShooter";
            PlayerSettings.productName = "Multi-Stage Chicken Shooter";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = false;
        }
    }
}
