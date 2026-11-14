using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Null-safe audio facade so gameplay code never throws when audio is unavailable.</summary>
    public static class AudioCue
    {
        public static void Play(AudioLibrary.Sfx id, float pitchVariance = 0.05f)
        {
            var manager = AudioManager.Instance;
            if (manager != null) manager.PlaySfx(id, pitchVariance);
        }

        public static void PlayBgm(AudioClip clip)
        {
            var manager = AudioManager.Instance;
            if (manager != null) manager.PlayBGM(clip);
        }

        public static void PlayBgm(int levelIndex)
        {
            var config = ConfigProvider.Config;
            if (config == null || config.audio == null) return;
            PlayBgm(config.audio.GetLevelBgm(levelIndex));
        }
    }

    /// <summary>Cached player lookup for dive-bomb aiming.</summary>
    public static class PlayerLocator
    {
        private static PlayerController _cached;

        public static void Register(PlayerController player) => _cached = player;

        public static void Unregister(PlayerController player)
        {
            if (_cached == player) _cached = null;
        }

        public static Transform Player
        {
            get
            {
                if (_cached != null && _cached.gameObject != null) return _cached.transform;
                _cached = Object.FindAnyObjectByType<PlayerController>();
                return _cached != null ? _cached.transform : null;
            }
        }

        public static Vector3 PlayerPositionOr(Vector3 fallback)
        {
            var player = Player;
            return player != null ? player.position : fallback;
        }

        public static void Clear() => _cached = null;
    }
}
