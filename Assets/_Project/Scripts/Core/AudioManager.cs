using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Persistent audio service: one looping BGM source and one SFX source.</summary>
    public class AudioManager : MonoBehaviour
    {
        public const string MasterKey = "cs_volume_master";
        public const string BgmKey = "cs_volume_bgm";
        public const string SfxKey = "cs_volume_sfx";

        public static AudioManager Instance { get; private set; }

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private AudioLibrary _library;

        public float MasterVolume { get; private set; } = 1f;
        public float BgmVolume { get; private set; } = 0.7f;
        public float SfxVolume { get; private set; } = 0.9f;

        public static AudioManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[AudioManager]");
            go.AddComponent<AudioManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;

            _library = ConfigProvider.Config != null ? ConfigProvider.Config.audio : null;

            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            BgmVolume = PlayerPrefs.GetFloat(BgmKey, 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.9f);
            ApplyVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayBGM(AudioClip clip, bool restart = false)
        {
            if (_bgmSource == null) return;
            if (clip == null) return;
            if (!restart && _bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource == null) return;
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public void PlaySfx(AudioLibrary.Sfx id, float pitchVariance = 0.05f)
        {
            if (_library == null) return;
            PlaySfx(_library.GetSfx(id), pitchVariance);
        }

        public void PlaySfx(AudioClip clip, float pitchVariance = 0.05f)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            _sfxSource.PlayOneShot(clip, 1f);
        }

        public void SetMasterVolume(float value) => MasterVolume = SaveVolume(MasterKey, value, MasterVolume);
        public void SetBGMVolume(float value) => BgmVolume = SaveVolume(BgmKey, value, BgmVolume);
        public void SetSFXVolume(float value) => SfxVolume = SaveVolume(SfxKey, value, SfxVolume);

        private float SaveVolume(string key, float value, float current)
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(key, clamped);
            PlayerPrefs.Save();
            ApplyVolumes();
            return clamped;
        }

        public void RefreshVolumes()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            BgmVolume = PlayerPrefs.GetFloat(BgmKey, 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.9f);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (_bgmSource != null) _bgmSource.volume = Mathf.Clamp01(MasterVolume * BgmVolume) * 0.7f;
            if (_sfxSource != null) _sfxSource.volume = Mathf.Clamp01(MasterVolume * SfxVolume);
        }
    }
}
