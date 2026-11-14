using UnityEditor;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Shared asset paths and material helpers for the editor generators.</summary>
    public static class EditorAssets
    {
        public const string Root = "Assets/_Project";
        public const string SpriteDir = Root + "/Art/Sprites";
        public const string MaterialDir = Root + "/Art/Materials";
        public const string BgmDir = Root + "/Audio/BGM";
        public const string SfxDir = Root + "/Audio/SFX";
        public const string InputDir = Root + "/Input";
        public const string PrefabRoot = Root + "/Prefabs";
        public const string PlayerPrefabDir = PrefabRoot + "/Player";
        public const string ProjectilePrefabDir = PrefabRoot + "/Projectiles";
        public const string EnemyPrefabDir = PrefabRoot + "/Enemies";
        public const string BossPrefabDir = PrefabRoot + "/Bosses";
        public const string PickupPrefabDir = PrefabRoot + "/Pickups";
        public const string VfxPrefabDir = PrefabRoot + "/VFX";
        public const string LevelDir = Root + "/ScriptableObjects/Levels";
        public const string WaveDir = Root + "/ScriptableObjects/Waves";
        public const string WeaponDir = Root + "/ScriptableObjects/Weapons";
        public const string ResourcesDir = Root + "/Resources";
        public const string SceneDir = "Assets/Scenes";

        public static Sprite Sprite(string spriteName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/" + spriteName + ".png");
            if (sprite == null) Debug.LogWarning("[EditorAssets] Missing sprite: " + spriteName);
            return sprite;
        }

        public static AudioClip Sfx(string clipName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxDir + "/" + clipName + ".wav");
            if (clip == null) Debug.LogWarning("[EditorAssets] Missing sfx: " + clipName);
            return clip;
        }

        public static AudioClip Bgm(string clipName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmDir + "/" + clipName + ".wav");
            if (clip == null) Debug.LogWarning("[EditorAssets] Missing bgm: " + clipName);
            return clip;
        }

        public static Material SpriteMaterial()
        {
            string path = MaterialDir + "/SpriteUnlit.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            ProceduralArt.EnsureFolder(MaterialDir);
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");

            var material = new Material(shader) { name = "SpriteUnlit" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static Material ParticleMaterial(string textureName = "StarParticle")
        {
            string path = MaterialDir + "/Vfx_" + textureName + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            ProceduralArt.EnsureFolder(MaterialDir);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader) { name = "Vfx_" + textureName };

            var particleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpriteDir + "/" + textureName + ".png");
            if (particleTexture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", particleTexture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", particleTexture);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static GameObject Prefab(string relativePath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/" + relativePath + ".prefab");
            if (prefab == null) Debug.LogWarning("[EditorAssets] Missing prefab: " + relativePath);
            return prefab;
        }
    }
}
