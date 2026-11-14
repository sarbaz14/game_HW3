using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Small software rasteriser used to bake crisp placeholder sprites as PNG assets.</summary>
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color[] _pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width * height];
            Clear(new Color(0f, 0f, 0f, 0f));
        }

        public void Clear(Color color)
        {
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = color;
        }

        public void Over(int x, int y, Color color)
        {
            if (color.a <= 0.0005f) return;
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;

            int index = y * Width + x;
            Color destination = _pixels[index];
            float outAlpha = color.a + destination.a * (1f - color.a);
            if (outAlpha <= 0.0005f)
            {
                _pixels[index] = new Color(0f, 0f, 0f, 0f);
                return;
            }

            float r = (color.r * color.a + destination.r * destination.a * (1f - color.a)) / outAlpha;
            float g = (color.g * color.a + destination.g * destination.a * (1f - color.a)) / outAlpha;
            float b = (color.b * color.a + destination.b * destination.a * (1f - color.a)) / outAlpha;
            _pixels[index] = new Color(r, g, b, outAlpha);
        }

        public void Rect(float x0, float y0, float x1, float y1, Color color)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(x0, x1));
            int maxX = Mathf.CeilToInt(Mathf.Max(x0, x1));
            int minY = Mathf.FloorToInt(Mathf.Min(y0, y1));
            int maxY = Mathf.CeilToInt(Mathf.Max(y0, y1));

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                Over(x, y, color);
        }

        public void Disc(float cx, float cy, float radius, Color color, float softness = 0.8f)
        {
            Ellipse(cx, cy, radius, radius, color, softness);
        }

        public void Ellipse(float cx, float cy, float radiusX, float radiusY, Color color, float softness = 0.8f)
        {
            int minX = Mathf.FloorToInt(cx - radiusX - 1f);
            int maxX = Mathf.CeilToInt(cx + radiusX + 1f);
            int minY = Mathf.FloorToInt(cy - radiusY - 1f);
            int maxY = Mathf.CeilToInt(cy + radiusY + 1f);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = (x + 0.5f - cx) / Mathf.Max(0.0001f, radiusX);
                    float ny = (y + 0.5f - cy) / Mathf.Max(0.0001f, radiusY);
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);

                    float edge = Mathf.Clamp01((1f - distance) * Mathf.Max(radiusX, radiusY) / Mathf.Max(0.4f, softness));
                    if (edge <= 0f) continue;

                    Color tinted = color;
                    tinted.a *= edge;
                    Over(x, y, tinted);
                }
            }
        }

        public void Glow(float cx, float cy, float radius, Color color, float power = 2.2f)
        {
            int minX = Mathf.FloorToInt(cx - radius);
            int maxX = Mathf.CeilToInt(cx + radius);
            int minY = Mathf.FloorToInt(cy - radius);
            int maxY = Mathf.CeilToInt(cy + radius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) / radius;
                    if (distance >= 1f) continue;

                    Color tinted = color;
                    tinted.a *= Mathf.Pow(1f - distance, power);
                    Over(x, y, tinted);
                }
            }
        }

        public void Ring(float cx, float cy, float outerRadius, float innerRadius, Color color, float softness = 1.2f)
        {
            int minX = Mathf.FloorToInt(cx - outerRadius - 1f);
            int maxX = Mathf.CeilToInt(cx + outerRadius + 1f);
            int minY = Mathf.FloorToInt(cy - outerRadius - 1f);
            int maxY = Mathf.CeilToInt(cy + outerRadius + 1f);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float outer = Mathf.Clamp01((outerRadius - distance) / softness);
                    float inner = Mathf.Clamp01((distance - innerRadius) / softness);
                    float alpha = Mathf.Min(outer, inner);
                    if (alpha <= 0f) continue;

                    Color tinted = color;
                    tinted.a *= alpha;
                    Over(x, y, tinted);
                }
            }
        }

        public void Line(Vector2 a, Vector2 b, float thickness, Color color)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness - 1f);
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness + 1f);
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness - 1f);
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + thickness + 1f);

            Vector2 ab = b - a;
            float lengthSquared = Mathf.Max(0.0001f, ab.sqrMagnitude);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
                    float distance = Vector2.Distance(point, a + ab * t);
                    float alpha = Mathf.Clamp01((thickness - distance) / 0.8f);
                    if (alpha <= 0f) continue;

                    Color tinted = color;
                    tinted.a *= alpha;
                    Over(x, y, tinted);
                }
            }
        }

        public void Polygon(Vector2[] points, Color color, int supersample = 2)
        {
            if (points == null || points.Length < 3) return;

            float minX = points[0].x, maxX = points[0].x, minY = points[0].y, maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                maxX = Mathf.Max(maxX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            int step = Mathf.Max(1, supersample);
            float sampleCount = step * step;

            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
            {
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < step; sy++)
                    {
                        for (int sx = 0; sx < step; sx++)
                        {
                            Vector2 sample = new Vector2(x + (sx + 0.5f) / step, y + (sy + 0.5f) / step);
                            if (Contains(points, sample)) hits++;
                        }
                    }

                    if (hits == 0) continue;

                    Color tinted = color;
                    tinted.a *= hits / sampleCount;
                    Over(x, y, tinted);
                }
            }
        }

        public void PolygonOutline(Vector2[] points, float thickness, Color color)
        {
            if (points == null || points.Length < 2) return;
            for (int i = 0; i < points.Length; i++)
            {
                Line(points[i], points[(i + 1) % points.Length], thickness, color);
            }
        }

        private static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > point.y) == (polygon[j].y > point.y)) continue;

                float denominator = polygon[j].y - polygon[i].y;
                if (Mathf.Abs(denominator) < 0.00001f) continue;

                float xIntersection = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / denominator + polygon[i].x;
                if (point.x < xIntersection) inside = !inside;
            }

            return inside;
        }

        public void Speckle(int seed, int count, Color color, float maxSize = 2f)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float x = (float)random.NextDouble() * Width;
                float y = (float)random.NextDouble() * Height;
                float size = 0.6f + (float)random.NextDouble() * maxSize;
                Disc(x, y, size, color, 0.6f);
            }
        }

        public Texture2D ToTexture()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels(_pixels);
            texture.Apply();
            return texture;
        }
    }

    /// <summary>Draws every sprite used by the game.</summary>
    public static class ProceduralArt
    {
        public struct Palette
        {
            public Color Body;
            public Color BodyShade;
            public Color Wing;
            public Color Beak;
            public Color Comb;
            public Color Eye;
            public Color Outline;
            public Color Accent;
        }

        public static Palette BasicChickenPalette => new Palette
        {
            Body = new Color(0.94f, 0.93f, 0.98f),
            BodyShade = new Color(0.78f, 0.78f, 0.88f),
            Wing = new Color(0.87f, 0.86f, 0.94f),
            Beak = new Color(1f, 0.62f, 0.16f),
            Comb = new Color(0.93f, 0.22f, 0.28f),
            Eye = new Color(0.12f, 0.1f, 0.16f),
            Outline = new Color(0.2f, 0.2f, 0.28f),
            Accent = new Color(0.45f, 0.9f, 1f)
        };

        public static Palette MechaChickenPalette => new Palette
        {
            Body = new Color(0.66f, 0.72f, 0.8f),
            BodyShade = new Color(0.42f, 0.47f, 0.56f),
            Wing = new Color(0.56f, 0.62f, 0.7f),
            Beak = new Color(0.95f, 0.66f, 0.2f),
            Comb = new Color(0.85f, 0.25f, 0.2f),
            Eye = new Color(1f, 0.28f, 0.25f),
            Outline = new Color(0.13f, 0.16f, 0.22f),
            Accent = new Color(1f, 0.35f, 0.25f)
        };

        public static Palette CyberChickenPalette => new Palette
        {
            Body = new Color(0.42f, 0.24f, 0.6f),
            BodyShade = new Color(0.28f, 0.15f, 0.42f),
            Wing = new Color(0.36f, 0.19f, 0.52f),
            Beak = new Color(0.95f, 0.35f, 0.55f),
            Comb = new Color(0.85f, 0.12f, 0.4f),
            Eye = new Color(0.55f, 1f, 0.55f),
            Outline = new Color(0.12f, 0.05f, 0.2f),
            Accent = new Color(0.7f, 0.35f, 1f)
        };

        // ---------------------------------------------------------------- ships

        public static void DrawPlayerShip(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color hull = new Color(0.11f, 0.17f, 0.29f);
            Color hullLight = new Color(0.24f, 0.36f, 0.55f);
            Color cyan = new Color(0.42f, 0.92f, 1f);
            Color white = new Color(0.92f, 0.98f, 1f);
            Color orange = new Color(1f, 0.62f, 0.28f);

            canvas.Glow(w * 0.5f, h * 0.45f, w * 0.55f, new Color(cyan.r, cyan.g, cyan.b, 0.35f), 2.4f);

            Vector2[] hullPoints =
            {
                new Vector2(w * 0.5f, h * 0.97f),
                new Vector2(w * 0.36f, h * 0.72f),
                new Vector2(w * 0.13f, h * 0.55f),
                new Vector2(w * 0.08f, h * 0.35f),
                new Vector2(w * 0.33f, h * 0.38f),
                new Vector2(w * 0.5f, h * 0.26f),
                new Vector2(w * 0.67f, h * 0.38f),
                new Vector2(w * 0.92f, h * 0.35f),
                new Vector2(w * 0.87f, h * 0.55f),
                new Vector2(w * 0.64f, h * 0.72f)
            };

            canvas.Polygon(hullPoints, hull);
            canvas.PolygonOutline(hullPoints, Mathf.Max(1.4f, w * 0.022f), cyan);

            // Wing accents
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.14f, h * 0.5f), new Vector2(w * 0.3f, h * 0.44f),
                new Vector2(w * 0.14f, h * 0.38f)
            }, cyan);
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.86f, h * 0.5f), new Vector2(w * 0.7f, h * 0.44f),
                new Vector2(w * 0.86f, h * 0.38f)
            }, cyan);

            // Fuselage highlight
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * 0.9f), new Vector2(w * 0.42f, h * 0.55f),
                new Vector2(w * 0.58f, h * 0.55f)
            }, hullLight);

            // Cockpit
            canvas.Ellipse(w * 0.5f, h * 0.66f, w * 0.1f, h * 0.15f, new Color(0.72f, 0.95f, 1f));
            canvas.Ellipse(w * 0.5f, h * 0.68f, w * 0.06f, h * 0.1f, white);

            // Engines
            canvas.Rect(w * 0.34f, h * 0.2f, w * 0.42f, h * 0.3f, hullLight);
            canvas.Rect(w * 0.58f, h * 0.2f, w * 0.66f, h * 0.3f, hullLight);
            canvas.Glow(w * 0.38f, h * 0.12f, w * 0.12f, new Color(orange.r, orange.g, orange.b, 0.95f), 1.8f);
            canvas.Glow(w * 0.62f, h * 0.12f, w * 0.12f, new Color(orange.r, orange.g, orange.b, 0.95f), 1.8f);
        }

        // ---------------------------------------------------------------- chickens

        public static void DrawChicken(PixelCanvas canvas, Palette palette, bool mecha, bool cyber, bool boss)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            float bodyCenterY = boss ? 0.42f : 0.42f;
            float bodyRx = boss ? 0.31f : 0.30f;
            float bodyRy = boss ? 0.31f : 0.29f;
            float headY = boss ? 0.71f : 0.70f;
            float headR = boss ? 0.20f : 0.19f;

            if (boss)
            {
                canvas.Glow(w * 0.5f, h * 0.5f, w * 0.6f,
                    new Color(palette.Accent.r, palette.Accent.g, palette.Accent.b, 0.22f), 2.5f);
            }

            // Tail feathers
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * 0.2f), new Vector2(w * 0.66f, h * 0.02f),
                new Vector2(w * 0.72f, h * 0.22f)
            }, palette.Outline);
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * 0.2f), new Vector2(w * 0.34f, h * 0.02f),
                new Vector2(w * 0.28f, h * 0.22f)
            }, palette.Outline);

            // Feet
            canvas.Rect(w * 0.4f, h * 0.06f, w * 0.46f, h * 0.2f, palette.Beak);
            canvas.Rect(w * 0.54f, h * 0.06f, w * 0.6f, h * 0.2f, palette.Beak);

            // Body silhouette + body
            canvas.Ellipse(w * 0.5f, h * bodyCenterY, w * (bodyRx + 0.02f), h * (bodyRy + 0.02f), palette.Outline);
            canvas.Ellipse(w * 0.5f, h * bodyCenterY, w * bodyRx, h * bodyRy, palette.Body);
            canvas.Ellipse(w * 0.5f, h * (bodyCenterY - 0.09f), w * (bodyRx - 0.03f), h * (bodyRy - 0.12f), palette.BodyShade);

            // Wings
            canvas.Ellipse(w * 0.19f, h * 0.44f, w * 0.1f, h * 0.15f, palette.Wing);
            canvas.Ellipse(w * 0.81f, h * 0.44f, w * 0.1f, h * 0.15f, palette.Wing);

            // Head
            canvas.Disc(w * 0.5f, h * headY, w * (headR + 0.02f), palette.Outline);
            canvas.Disc(w * 0.5f, h * headY, w * headR, palette.Body);

            // Comb
            for (int i = 0; i < 3; i++)
            {
                float cx = w * (0.42f + i * 0.08f);
                float cy = h * (headY + headR - 0.02f + (i == 1 ? 0.035f : 0f));
                canvas.Disc(cx, cy, w * (boss ? 0.065f : 0.055f), palette.Comb);
            }

            if (boss)
            {
                // Crown for the stage bosses
                Color crown = new Color(1f, 0.83f, 0.32f);
                canvas.Polygon(new[]
                {
                    new Vector2(w * 0.33f, h * 0.86f), new Vector2(w * 0.4f, h * 0.99f),
                    new Vector2(w * 0.47f, h * 0.88f), new Vector2(w * 0.53f, h * 0.99f),
                    new Vector2(w * 0.6f, h * 0.88f), new Vector2(w * 0.67f, h * 0.99f),
                    new Vector2(w * 0.67f, h * 0.84f)
                }, crown);
            }

            // Beak
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * (headY - 0.05f)),
                new Vector2(w * 0.41f, h * (headY - 0.13f)),
                new Vector2(w * 0.59f, h * (headY - 0.13f))
            }, palette.Beak);

            if (mecha)
            {
                // Armoured plates and visor
                canvas.Rect(w * 0.3f, h * 0.5f, w * 0.7f, h * 0.545f, palette.BodyShade);
                canvas.Rect(w * 0.38f, h * (headY + 0.02f), w * 0.62f, h * (headY + 0.075f), new Color(0.1f, 0.12f, 0.16f));
                canvas.Glow(w * 0.5f, h * (headY + 0.047f), w * 0.16f, new Color(1f, 0.3f, 0.22f, 0.9f), 1.6f);
                canvas.Rect(w * 0.42f, h * (headY + 0.035f), w * 0.58f, h * (headY + 0.06f), palette.Eye);

                for (int i = 0; i < 4; i++)
                    canvas.Disc(w * (0.3f + i * 0.13f), h * 0.33f, w * 0.014f, new Color(0.85f, 0.9f, 1f));

                // Antenna
                canvas.Line(new Vector2(w * 0.5f, h * 0.86f), new Vector2(w * 0.56f, h * 0.99f), Mathf.Max(1.2f, w * 0.012f), palette.Outline);
                canvas.Disc(w * 0.56f, h * 0.99f, w * 0.025f, palette.Eye);
            }
            else if (cyber)
            {
                // Alien cyber-chicken: three eyes, glowing veins
                canvas.Disc(w * 0.43f, h * 0.74f, w * 0.042f, new Color(0.85f, 1f, 0.85f));
                canvas.Disc(w * 0.57f, h * 0.74f, w * 0.042f, new Color(0.85f, 1f, 0.85f));
                canvas.Disc(w * 0.5f, h * 0.66f, w * 0.032f, new Color(0.85f, 1f, 0.85f));
                canvas.Disc(w * 0.43f, h * 0.74f, w * 0.017f, palette.Outline);
                canvas.Disc(w * 0.57f, h * 0.74f, w * 0.017f, palette.Outline);

                canvas.Line(new Vector2(w * 0.22f, h * 0.46f), new Vector2(w * 0.38f, h * 0.56f), Mathf.Max(1.1f, w * 0.012f), palette.Accent);
                canvas.Line(new Vector2(w * 0.78f, h * 0.46f), new Vector2(w * 0.62f, h * 0.56f), Mathf.Max(1.1f, w * 0.012f), palette.Accent);
                canvas.Glow(w * 0.5f, h * 0.42f, w * 0.18f, new Color(palette.Accent.r, palette.Accent.g, palette.Accent.b, 0.28f), 2f);
            }
            else
            {
                // Classic chicken eyes
                canvas.Disc(w * 0.43f, h * 0.74f, w * 0.05f, new Color(1f, 1f, 1f));
                canvas.Disc(w * 0.57f, h * 0.74f, w * 0.05f, new Color(1f, 1f, 1f));
                canvas.Disc(w * 0.435f, h * 0.735f, w * 0.024f, palette.Eye);
                canvas.Disc(w * 0.565f, h * 0.735f, w * 0.024f, palette.Eye);
            }

            // Outline pass for readability
            canvas.Ring(w * 0.5f, h * bodyCenterY, w * bodyRx, w * (bodyRx - 0.022f), new Color(palette.Outline.r, palette.Outline.g, palette.Outline.b, 0.85f), 0.9f);
        }

        // ---------------------------------------------------------------- projectiles & props

        public static void DrawLaser(PixelCanvas canvas, Color core, Color glow)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            canvas.Glow(w * 0.5f, h * 0.5f, w * 0.48f, new Color(glow.r, glow.g, glow.b, 0.85f), 1.5f);
            canvas.Rect(w * 0.30f, h * 0.08f, w * 0.70f, h * 0.92f, glow);
            canvas.Rect(w * 0.40f, h * 0.12f, w * 0.60f, h * 0.95f, core);
            canvas.Rect(w * 0.46f, h * 0.2f, w * 0.54f, h * 1f, new Color(1f, 1f, 1f, 0.95f));
            canvas.Glow(w * 0.5f, h * 0.95f, w * 0.3f, new Color(core.r, core.g, core.b, 0.8f), 1.4f);
        }

        public static void DrawSuperMissile(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color steel = new Color(0.72f, 0.76f, 0.84f);
            Color dark = new Color(0.28f, 0.31f, 0.38f);
            Color hot = new Color(1f, 0.55f, 0.18f);

            canvas.Glow(w * 0.5f, h * 0.25f, w * 0.5f, new Color(hot.r, hot.g, hot.b, 0.5f), 1.8f);

            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * 0.98f), new Vector2(w * 0.68f, h * 0.62f),
                new Vector2(w * 0.66f, h * 0.2f), new Vector2(w * 0.34f, h * 0.2f),
                new Vector2(w * 0.32f, h * 0.62f)
            }, steel);

            canvas.PolygonOutline(new[]
            {
                new Vector2(w * 0.5f, h * 0.98f), new Vector2(w * 0.68f, h * 0.62f),
                new Vector2(w * 0.66f, h * 0.2f), new Vector2(w * 0.34f, h * 0.2f),
                new Vector2(w * 0.32f, h * 0.62f)
            }, Mathf.Max(1.2f, w * 0.03f), dark);

            canvas.Rect(w * 0.32f, h * 0.5f, w * 0.68f, h * 0.58f, new Color(0.95f, 0.35f, 0.2f));

            // Fins
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.32f, h * 0.36f), new Vector2(w * 0.06f, h * 0.16f),
                new Vector2(w * 0.32f, h * 0.2f)
            }, dark);
            canvas.Polygon(new[]
            {
                new Vector2(w * 0.68f, h * 0.36f), new Vector2(w * 0.94f, h * 0.16f),
                new Vector2(w * 0.68f, h * 0.2f)
            }, dark);

            canvas.Disc(w * 0.5f, h * 0.62f, w * 0.08f, new Color(1f, 0.92f, 0.7f));
            canvas.Glow(w * 0.5f, h * 0.1f, w * 0.24f, new Color(1f, 0.75f, 0.3f, 0.9f), 1.6f);
        }

        public static void DrawEgg(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color shell = new Color(0.98f, 0.97f, 0.93f);
            Color shade = new Color(0.8f, 0.8f, 0.82f);

            canvas.Ellipse(w * 0.5f, h * 0.5f, w * 0.38f, h * 0.46f, shade);
            canvas.Ellipse(w * 0.5f, h * 0.52f, w * 0.34f, h * 0.44f, shell);
            canvas.Ellipse(w * 0.4f, h * 0.62f, w * 0.09f, h * 0.14f, new Color(1f, 1f, 1f, 0.9f));
            canvas.Line(new Vector2(w * 0.44f, h * 0.3f), new Vector2(w * 0.56f, h * 0.42f),
                Mathf.Max(1f, w * 0.03f), new Color(0.75f, 0.7f, 0.72f, 0.8f));
        }

        public static void DrawEnergyFeather(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color glow = new Color(0.7f, 0.4f, 1f);
            Color core = new Color(0.95f, 0.9f, 1f);

            canvas.Glow(w * 0.5f, h * 0.55f, w * 0.5f, new Color(glow.r, glow.g, glow.b, 0.75f), 1.6f);

            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * 0.98f), new Vector2(w * 0.82f, h * 0.55f),
                new Vector2(w * 0.58f, h * 0.42f), new Vector2(w * 0.5f, h * 0.05f),
                new Vector2(w * 0.42f, h * 0.42f), new Vector2(w * 0.18f, h * 0.55f)
            }, new Color(glow.r, glow.g, glow.b, 0.9f));

            canvas.Line(new Vector2(w * 0.5f, h * 0.95f), new Vector2(w * 0.5f, h * 0.08f), Mathf.Max(1.2f, w * 0.045f), core);

            for (int i = 0; i < 4; i++)
            {
                float y = h * (0.75f - i * 0.13f);
                canvas.Line(new Vector2(w * 0.5f, y), new Vector2(w * 0.76f, y + h * 0.04f), Mathf.Max(1f, w * 0.03f),
                    new Color(core.r, core.g, core.b, 0.7f));
                canvas.Line(new Vector2(w * 0.5f, y), new Vector2(w * 0.24f, y + h * 0.04f), Mathf.Max(1f, w * 0.03f),
                    new Color(core.r, core.g, core.b, 0.7f));
            }
        }

        public static void DrawAsteroid(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color rock = new Color(0.45f, 0.38f, 0.34f);
            Color rockLight = new Color(0.62f, 0.54f, 0.47f);
            Color rockDark = new Color(0.28f, 0.24f, 0.23f);

            var random = new System.Random(7);
            var points = new Vector2[11];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i / (float)points.Length * Mathf.PI * 2f;
                float radius = 0.36f + (float)random.NextDouble() * 0.1f;
                points[i] = new Vector2(w * (0.5f + Mathf.Cos(angle) * radius), h * (0.5f + Mathf.Sin(angle) * radius));
            }

            canvas.Polygon(points, rock);
            canvas.PolygonOutline(points, Mathf.Max(1.2f, w * 0.02f), rockDark);

            canvas.Ellipse(w * 0.42f, h * 0.58f, w * 0.14f, h * 0.11f, rockLight);
            canvas.Disc(w * 0.6f, h * 0.42f, w * 0.09f, rockDark);
            canvas.Disc(w * 0.36f, h * 0.38f, w * 0.06f, rockDark);
            canvas.Disc(w * 0.55f, h * 0.66f, w * 0.045f, rockDark);
            canvas.Ellipse(w * 0.62f, h * 0.56f, w * 0.05f, h * 0.04f, rockLight);
            canvas.Speckle(11, 18, new Color(0.2f, 0.17f, 0.16f, 0.35f), 1.6f);
        }

        public static void DrawDrumstick(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color meat = new Color(0.75f, 0.42f, 0.18f);
            Color meatLight = new Color(0.9f, 0.58f, 0.28f);
            Color bone = new Color(0.97f, 0.95f, 0.88f);

            canvas.Glow(w * 0.5f, h * 0.5f, w * 0.5f, new Color(1f, 0.6f, 0.2f, 0.3f), 2f);
            canvas.Ellipse(w * 0.55f, h * 0.55f, w * 0.32f, h * 0.3f, meat);
            canvas.Ellipse(w * 0.56f, h * 0.6f, w * 0.2f, h * 0.18f, meatLight);

            canvas.Line(new Vector2(w * 0.3f, h * 0.35f), new Vector2(w * 0.16f, h * 0.18f), Mathf.Max(3f, w * 0.09f), bone);
            canvas.Disc(w * 0.15f, h * 0.16f, w * 0.07f, bone);
            canvas.Disc(w * 0.24f, h * 0.11f, w * 0.06f, bone);
        }

        public static void DrawBurger(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color bun = new Color(0.9f, 0.68f, 0.33f);
            Color bunLight = new Color(1f, 0.82f, 0.45f);
            Color patty = new Color(0.42f, 0.24f, 0.15f);
            Color cheese = new Color(1f, 0.82f, 0.2f);
            Color lettuce = new Color(0.5f, 0.85f, 0.35f);

            canvas.Glow(w * 0.5f, h * 0.5f, w * 0.5f, new Color(1f, 0.75f, 0.25f, 0.25f), 2f);

            canvas.Ellipse(w * 0.5f, h * 0.72f, w * 0.4f, h * 0.2f, bun);
            canvas.Ellipse(w * 0.5f, h * 0.76f, w * 0.34f, h * 0.14f, bunLight);
            canvas.Rect(w * 0.14f, h * 0.55f, w * 0.86f, h * 0.64f, lettuce);
            canvas.Rect(w * 0.16f, h * 0.46f, w * 0.84f, h * 0.58f, cheese);
            canvas.Rect(w * 0.16f, h * 0.32f, w * 0.84f, h * 0.5f, patty);
            canvas.Ellipse(w * 0.5f, h * 0.28f, w * 0.4f, h * 0.16f, bun);
            canvas.Ellipse(w * 0.5f, h * 0.26f, w * 0.34f, h * 0.11f, bunLight);

            canvas.Disc(w * 0.4f, h * 0.79f, w * 0.028f, new Color(1f, 0.97f, 0.85f));
            canvas.Disc(w * 0.57f, h * 0.82f, w * 0.026f, new Color(1f, 0.97f, 0.85f));
            canvas.Disc(w * 0.5f, h * 0.72f, w * 0.024f, new Color(1f, 0.97f, 0.85f));
        }

        public static void DrawGiftBox(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            Color box = new Color(0.9f, 0.22f, 0.32f);
            Color boxLight = new Color(1f, 0.35f, 0.45f);
            Color ribbon = new Color(1f, 0.85f, 0.3f);

            canvas.Glow(w * 0.5f, h * 0.5f, w * 0.52f, new Color(1f, 0.8f, 0.3f, 0.3f), 2f);

            canvas.Rect(w * 0.12f, h * 0.12f, w * 0.88f, h * 0.72f, box);
            canvas.Rect(w * 0.16f, h * 0.16f, w * 0.84f, h * 0.3f, boxLight);
            canvas.Rect(w * 0.44f, h * 0.12f, w * 0.56f, h * 0.72f, ribbon);
            canvas.Rect(w * 0.12f, h * 0.62f, w * 0.88f, h * 0.72f, ribbon);

            canvas.Disc(w * 0.4f, h * 0.82f, w * 0.12f, ribbon);
            canvas.Disc(w * 0.6f, h * 0.82f, w * 0.12f, ribbon);
            canvas.Disc(w * 0.5f, h * 0.78f, w * 0.065f, new Color(1f, 0.95f, 0.6f));
        }

        public static void DrawStarParticle(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;
            canvas.Glow(w * 0.5f, h * 0.5f, w * 0.5f, new Color(1f, 1f, 1f, 1f), 1.8f);
            canvas.Disc(w * 0.5f, h * 0.5f, w * 0.18f, new Color(1f, 1f, 1f, 1f));
        }

        public static void DrawFeatherParticle(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            canvas.Polygon(new[]
            {
                new Vector2(w * 0.5f, h * 0.95f), new Vector2(w * 0.78f, h * 0.5f),
                new Vector2(w * 0.52f, h * 0.1f), new Vector2(w * 0.22f, h * 0.5f)
            }, new Color(1f, 0.97f, 0.9f, 0.95f));

            canvas.Line(new Vector2(w * 0.5f, h * 0.9f), new Vector2(w * 0.5f, h * 0.12f), Mathf.Max(1f, w * 0.08f),
                new Color(0.85f, 0.8f, 0.72f, 0.9f));
        }

        public static void DrawShockwave(PixelCanvas canvas)
        {
            float w = canvas.Width;
            float h = canvas.Height;

            canvas.Ring(w * 0.5f, h * 0.5f, w * 0.48f, w * 0.36f, new Color(0.6f, 0.95f, 1f, 0.95f), 3.5f);
            canvas.Glow(w * 0.5f, h * 0.5f, w * 0.34f, new Color(0.5f, 0.9f, 1f, 0.35f), 2.2f);
            canvas.Ring(w * 0.5f, h * 0.5f, w * 0.3f, w * 0.27f, new Color(1f, 1f, 1f, 0.6f), 2f);
        }

        // ---------------------------------------------------------------- asset writing

        public static Sprite SaveSprite(PixelCanvas canvas, string assetPath, int pixelsPerUnit = 64)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");

            var texture = canvas.ToTexture();
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[ProceduralArt] Could not import sprite: " + assetPath);
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path);
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            string normalizedParent = parent.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(normalizedParent)) EnsureFolder(normalizedParent);
            AssetDatabase.CreateFolder(normalizedParent, leaf);
        }
    }
}
