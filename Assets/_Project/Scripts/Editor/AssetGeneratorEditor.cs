using UnityEditor;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Bakes all placeholder sprites (PNG) and audio clips (WAV) procedurally.</summary>
    public static class AssetGeneratorEditor
    {
        [MenuItem("ChickenShooter/1. Generate Art & Audio", false, 10)]
        public static void GenerateAll()
        {
            GenerateSprites();
            GenerateAudio();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AssetGenerator] Sprites and audio generated.");
        }

        public static void GenerateSprites()
        {
            ProceduralArt.EnsureFolder(EditorAssets.SpriteDir);

            // Player ship
            var ship = new PixelCanvas(96, 96);
            ProceduralArt.DrawPlayerShip(ship);
            ProceduralArt.SaveSprite(ship, EditorAssets.SpriteDir + "/PlayerShip.png", 64);

            // Chickens
            SaveChicken("Chicken_Lv1", 64, ProceduralArt.BasicChickenPalette, false, false, false);
            SaveChicken("Chicken_Lv2", 64, ProceduralArt.MechaChickenPalette, true, false, false);
            SaveChicken("Chicken_Lv3", 64, ProceduralArt.CyberChickenPalette, false, true, false);

            // Bosses
            SaveChicken("Boss_Lv1", 160, ProceduralArt.BasicChickenPalette, false, false, true);
            SaveChicken("Boss_Lv2", 160, ProceduralArt.MechaChickenPalette, true, false, true);
            SaveChicken("Boss_Lv3", 160, ProceduralArt.CyberChickenPalette, false, true, true);

            // Lasers
            SaveLaser("Laser_Lv1", new Color(0.85f, 1f, 1f), new Color(0.35f, 0.95f, 1f));
            SaveLaser("Laser_Lv2", new Color(0.9f, 1f, 0.85f), new Color(0.45f, 1f, 0.55f));
            SaveLaser("Laser_Lv3", new Color(1f, 0.9f, 1f), new Color(0.85f, 0.45f, 1f));

            var missile = new PixelCanvas(40, 64);
            ProceduralArt.DrawSuperMissile(missile);
            ProceduralArt.SaveSprite(missile, EditorAssets.SpriteDir + "/SuperMissile.png", 64);

            var egg = new PixelCanvas(32, 44);
            ProceduralArt.DrawEgg(egg);
            ProceduralArt.SaveSprite(egg, EditorAssets.SpriteDir + "/EnemyEgg.png", 64);

            var feather = new PixelCanvas(24, 44);
            ProceduralArt.DrawEnergyFeather(feather);
            ProceduralArt.SaveSprite(feather, EditorAssets.SpriteDir + "/EnergyFeather.png", 64);

            var asteroid = new PixelCanvas(80, 80);
            ProceduralArt.DrawAsteroid(asteroid);
            ProceduralArt.SaveSprite(asteroid, EditorAssets.SpriteDir + "/Asteroid.png", 64);

            var drumstick = new PixelCanvas(40, 40);
            ProceduralArt.DrawDrumstick(drumstick);
            ProceduralArt.SaveSprite(drumstick, EditorAssets.SpriteDir + "/Food_Drumstick.png", 64);

            var burger = new PixelCanvas(40, 40);
            ProceduralArt.DrawBurger(burger);
            ProceduralArt.SaveSprite(burger, EditorAssets.SpriteDir + "/Food_Burger.png", 64);

            var gift = new PixelCanvas(40, 40);
            ProceduralArt.DrawGiftBox(gift);
            ProceduralArt.SaveSprite(gift, EditorAssets.SpriteDir + "/GiftBox.png", 64);

            var star = new PixelCanvas(24, 24);
            ProceduralArt.DrawStarParticle(star);
            ProceduralArt.SaveSprite(star, EditorAssets.SpriteDir + "/StarParticle.png", 64);

            var featherParticle = new PixelCanvas(20, 24);
            ProceduralArt.DrawFeatherParticle(featherParticle);
            ProceduralArt.SaveSprite(featherParticle, EditorAssets.SpriteDir + "/FeatherParticle.png", 64);

            var shockwave = new PixelCanvas(128, 128);
            ProceduralArt.DrawShockwave(shockwave);
            ProceduralArt.SaveSprite(shockwave, EditorAssets.SpriteDir + "/RingShockwave.png", 64);
        }

        private static void SaveChicken(string name, int size, ProceduralArt.Palette palette, bool mecha, bool cyber, bool boss)
        {
            var canvas = new PixelCanvas(size, size);
            ProceduralArt.DrawChicken(canvas, palette, mecha, cyber, boss);
            ProceduralArt.SaveSprite(canvas, EditorAssets.SpriteDir + "/" + name + ".png", 64);
        }

        private static void SaveLaser(string name, Color core, Color glow)
        {
            var canvas = new PixelCanvas(14, 44);
            ProceduralArt.DrawLaser(canvas, core, glow);
            ProceduralArt.SaveSprite(canvas, EditorAssets.SpriteDir + "/" + name + ".png", 64);
        }

        public static void GenerateAudio()
        {
            ProceduralArt.EnsureFolder(EditorAssets.BgmDir);
            ProceduralArt.EnsureFolder(EditorAssets.SfxDir);

            // BGM: menu (calm), level 1 (heroic), level 2 (industrial), level 3 (darker, faster)
            var minorArp = new[] { 0, 7, 12, 7, 15, 12, 7, 3 };
            var majorArp = new[] { 0, 4, 7, 12, 7, 4, 12, 7 };

            ProceduralAudio.WriteWav(EditorAssets.BgmDir + "/BGM_MainMenu.wav",
                ProceduralAudio.BuildBgm(new ProceduralAudio.BgmSpec
                {
                    Bpm = 96,
                    Bars = 8,
                    ChordRoots = new[] { 0, -3, -5, -1 },
                    ArpPattern = minorArp,
                    LeadKind = 0,
                    Brightness = 0.4f,
                    Seed = 101
                }));

            ProceduralAudio.WriteWav(EditorAssets.BgmDir + "/BGM_Level1.wav",
                ProceduralAudio.BuildBgm(new ProceduralAudio.BgmSpec
                {
                    Bpm = 132,
                    Bars = 8,
                    ChordRoots = new[] { 0, 5, 7, 5 },
                    ArpPattern = majorArp,
                    LeadKind = 1,
                    Brightness = 0.8f,
                    Seed = 202
                }));

            ProceduralAudio.WriteWav(EditorAssets.BgmDir + "/BGM_Level2.wav",
                ProceduralAudio.BuildBgm(new ProceduralAudio.BgmSpec
                {
                    Bpm = 144,
                    Bars = 8,
                    ChordRoots = new[] { 0, -2, -4, 3 },
                    ArpPattern = minorArp,
                    LeadKind = 2,
                    Brightness = 0.7f,
                    Seed = 303
                }));

            ProceduralAudio.WriteWav(EditorAssets.BgmDir + "/BGM_Level3.wav",
                ProceduralAudio.BuildBgm(new ProceduralAudio.BgmSpec
                {
                    Bpm = 158,
                    Bars = 8,
                    ChordRoots = new[] { 0, 1, -2, 0 },
                    ArpPattern = minorArp,
                    LeadKind = 1,
                    Brightness = 0.9f,
                    Seed = 404
                }));

            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_Laser.wav", ProceduralAudio.Laser());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_MissileExplosion.wav", ProceduralAudio.Explosion());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_ChickenCluck.wav", ProceduralAudio.Cluck());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_ChickenDeath.wav", ProceduralAudio.ChickenDeath());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_EggCrack.wav", ProceduralAudio.EggCrack());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_PickupFood.wav", ProceduralAudio.PickupFood());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_PickupGift.wav", ProceduralAudio.PickupGift());
            ProceduralAudio.WriteWav(EditorAssets.SfxDir + "/SFX_PlayerHit.wav", ProceduralAudio.PlayerHit());
        }
    }
}
