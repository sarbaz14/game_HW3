using UnityEngine;

namespace ChickenShooter
{
    [CreateAssetMenu(menuName = "ChickenShooter/AudioLibrary", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        public enum Sfx
        {
            Laser = 0,
            MissileExplosion = 1,
            ChickenCluck = 2,
            ChickenDeath = 3,
            EggCrack = 4,
            PickupFood = 5,
            PickupGift = 6,
            PlayerHit = 7
        }

        public AudioClip[] bgm = new AudioClip[4]; // 0 main menu, 1..3 levels
        public AudioClip[] sfx = new AudioClip[8];

        public AudioClip GetSfx(Sfx id)
        {
            int index = (int)id;
            if (sfx == null || index < 0 || index >= sfx.Length) return null;
            return sfx[index];
        }

        public AudioClip GetLevelBgm(int levelNumber)
        {
            if (bgm == null) return null;
            int index = Mathf.Clamp(levelNumber, 0, bgm.Length - 1);
            return index < bgm.Length ? bgm[index] : null;
        }

        public AudioClip MenuBgm => GetLevelBgm(0);
    }
}
