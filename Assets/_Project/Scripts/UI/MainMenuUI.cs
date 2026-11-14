using UnityEngine;
using UnityEngine.UI;

namespace ChickenShooter
{
    /// <summary>Main menu: New Game, Continue (only with a save file), volume sliders and Quit.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public Button newGameButton;
        public Button continueButton;
        public Button quitButton;
        public Slider masterSlider;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Text continueInfoText;
        public Text saveStatusText;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(StartNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBgmVolume);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        private void Start()
        {
            Time.timeScale = 1f;

            AudioCue.PlayBgm(0);

            var audioManager = AudioManager.EnsureInstance();
            audioManager.RefreshVolumes();

            if (masterSlider != null) masterSlider.SetValueWithoutNotify(audioManager.MasterVolume);
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(audioManager.BgmVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);

            RefreshContinueState();
        }

        private void RefreshContinueState()
        {
            bool hasSave = SaveSystem.HasSaveFile();
            if (continueButton != null) continueButton.interactable = hasSave;

            var data = hasSave ? SaveSystem.Load() : null;

            if (saveStatusText != null)
            {
                saveStatusText.text = hasSave
                    ? "SAVE FOUND"
                    : "NO SAVE DATA";
                saveStatusText.color = hasSave ? UIStyle.Accent : new Color(0.65f, 0.68f, 0.78f);
            }

            if (continueInfoText != null)
            {
                continueInfoText.text = hasSave && data != null
                    ? "LEVEL " + data.levelIndex + "  -  ROUND " + data.roundIndex +
                      "     SCORE " + data.totalScore.ToString("N0")
                    : "Start a run to create a save file";
            }
        }

        public void StartNewGame()
        {
            var gameManager = GameManager.EnsureInstance();
            gameManager.StartNewGame();
        }

        public void ContinueGame()
        {
            var gameManager = GameManager.EnsureInstance();
            gameManager.ContinueGame();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetMasterVolume(float value)
        {
            var audioManager = AudioManager.Instance;
            if (audioManager != null) audioManager.SetMasterVolume(value);
        }

        private void SetBgmVolume(float value)
        {
            var audioManager = AudioManager.Instance;
            if (audioManager != null) audioManager.SetBGMVolume(value);
        }

        private void SetSfxVolume(float value)
        {
            var audioManager = AudioManager.Instance;
            if (audioManager != null) audioManager.SetSFXVolume(value);
        }
    }
}
