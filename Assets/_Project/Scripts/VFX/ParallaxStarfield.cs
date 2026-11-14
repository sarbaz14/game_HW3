using System.Collections.Generic;
using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Three procedurally generated parallax star layers tinted per stage theme
    /// (Earth orbit, asteroid belt, deep galaxy).
    /// </summary>
    public class ParallaxStarfield : MonoBehaviour
    {
        public Color primaryTint = new Color(0.35f, 0.55f, 1f);
        public Color secondaryTint = new Color(0.9f, 0.55f, 0.3f);
        public int starsPerLayer = 42;
        public float verticalSpan = 16f;
        public float horizontalSpan = 24f;
        public int nebulaCount = 4;

        private class Star
        {
            public Transform transform;
            public float speed;
            public float driftSpeed;
        }

        private readonly List<Star> _stars = new List<Star>();
        private static Sprite _dotSprite;
        private static Sprite _blobSprite;

        private void Start()
        {
            BuildStarfield();
        }

        private void BuildStarfield()
        {
            _dotSprite = _dotSprite != null ? _dotSprite : CreateDotSprite(32, 0.95f);
            _blobSprite = _blobSprite != null ? _blobSprite : CreateDotSprite(64, 0.35f);

            EnsureSortingLayer("Background");

            var root = new GameObject("StarLayers").transform;
            root.SetParent(transform, false);

            // Nebula clouds first (far background).
            for (int i = 0; i < nebulaCount; i++)
            {
                var nebula = new GameObject("Nebula_" + i);
                nebula.transform.SetParent(root, false);
                nebula.transform.localPosition = new Vector3(
                    Random.Range(-horizontalSpan * 0.45f, horizontalSpan * 0.45f),
                    Random.Range(-verticalSpan * 0.5f, verticalSpan * 0.5f), 0f);
                float size = Random.Range(5f, 9f);
                nebula.transform.localScale = new Vector3(size, size, 1f);

                var renderer = nebula.AddComponent<SpriteRenderer>();
                renderer.sprite = _blobSprite;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -60;
                var tint = i % 2 == 0 ? primaryTint : secondaryTint;
                tint.a = 0.10f;
                renderer.color = tint;

                var star = new Star
                {
                    transform = nebula.transform,
                    speed = Random.Range(0.05f, 0.12f),
                    driftSpeed = Random.Range(-0.05f, 0.05f)
                };
                _stars.Add(star);
            }

            for (int layer = 0; layer < 3; layer++)
            {
                var layerRoot = new GameObject("StarLayer_" + layer).transform;
                layerRoot.SetParent(root, false);

                float depth = 1f - layer * 0.32f;
                float speed = Mathf.Lerp(2.4f, 0.5f, layer / 2f);
                int count = Mathf.RoundToInt(starsPerLayer * (1f + layer * 0.35f));

                for (int i = 0; i < count; i++)
                {
                    var starGo = new GameObject("Star");
                    starGo.transform.SetParent(layerRoot, false);
                    starGo.transform.localPosition = new Vector3(
                        Random.Range(-horizontalSpan * 0.5f, horizontalSpan * 0.5f),
                        Random.Range(-verticalSpan * 0.5f, verticalSpan * 0.5f), 0f);

                    float size = Random.Range(0.06f, 0.16f) * depth + layer * 0.02f;
                    starGo.transform.localScale = new Vector3(size, size, 1f);

                    var renderer = starGo.AddComponent<SpriteRenderer>();
                    renderer.sprite = _dotSprite;
                    renderer.sortingLayerName = "Background";
                    renderer.sortingOrder = -50 + layer * 5;

                    Color tint = Color.Lerp(primaryTint, Color.white, 0.55f);
                    if (Random.value > 0.72f) tint = Color.Lerp(secondaryTint, Color.white, 0.35f);
                    tint.a = Mathf.Lerp(0.35f, 1f, depth);
                    renderer.color = tint;

                    _stars.Add(new Star
                    {
                        transform = starGo.transform,
                        speed = speed * Random.Range(0.75f, 1.3f),
                        driftSpeed = Random.Range(-0.08f, 0.08f)
                    });
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float halfSpan = verticalSpan * 0.5f;
            float halfWidth = horizontalSpan * 0.5f;

            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                if (star.transform == null) continue;

                var position = star.transform.localPosition;
                position.y -= star.speed * dt;
                position.x += star.driftSpeed * dt;

                if (position.y < -halfSpan) position.y += verticalSpan;
                if (position.x < -halfWidth) position.x += horizontalSpan;
                else if (position.x > halfWidth) position.x -= horizontalSpan;

                star.transform.localPosition = position;
            }
        }

        private static void EnsureSortingLayer(string layerName)
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName) return;
            }

            Debug.LogWarning("[ParallaxStarfield] Sorting layer '" + layerName + "' missing - using Default.");
        }

        private static Sprite CreateDotSprite(int size, float hardness)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = Mathf.Pow(alpha, Mathf.Lerp(3.2f, 1.1f, hardness));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
