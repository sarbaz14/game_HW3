using UnityEngine;
using UnityEngine.UI;

namespace ChickenShooter
{
    /// <summary>Game over screen: Retry (Round 1 of the current level) or Main Menu.</summary>
    public class GameOverUI : MonoBehaviour
    {
        public GameObject panel;
        public Text scoreText;
        public Text messageText;
        public Button retryButton;
        public Button mainMenuButton;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += Show;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= Show;
        }

        private void Show()
        {
            if (panel != null) panel.SetActive(true);

            var gameManager = GameManager.Instance;
            if (scoreText != null)
            {
                int score = gameManager != null ? gameManager.TotalScore : 0;
                scoreText.text = "FINAL SCORE\n" + score.ToString("N0");
            }

            if (messageText != null && gameManager != null)
            {
                messageText.text = "Progress reset to Level " + gameManager.LevelIndex + " - Round 1";
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.RetryCurrentLevel();
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.QuitToMainMenu();
        }
    }
}
