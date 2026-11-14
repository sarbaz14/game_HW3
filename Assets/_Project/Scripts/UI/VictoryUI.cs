using UnityEngine;
using UnityEngine.UI;

namespace ChickenShooter
{
    /// <summary>Final victory screen (Level 3 boss defeated, save file reset).</summary>
    public class VictoryUI : MonoBehaviour
    {
        public GameObject panel;
        public Text scoreText;
        public Button mainMenuButton;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void OnEnable()
        {
            GameEvents.OnGameVictory += Show;
        }

        private void OnDisable()
        {
            GameEvents.OnGameVictory -= Show;
        }

        private void Show()
        {
            if (panel != null) panel.SetActive(true);

            var gameManager = GameManager.Instance;
            if (scoreText != null)
            {
                int score = gameManager != null ? gameManager.TotalScore : 0;
                scoreText.text = "THE GALAXY IS SAFE\n\nFINAL SCORE\n" + score.ToString("N0");
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.QuitToMainMenu();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.MenuSceneName);
        }
    }
}
