using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChickenShooter
{
    /// <summary>
    /// Pause menu (Esc / P). Intentionally contains ONLY Resume and Quit to Main Menu -
    /// no restart and no manual save, so the game-over penalty cannot be bypassed.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject panel;
        public Button resumeButton;
        public Button mainMenuButton;

        private InputAction _pauseAction;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(QuitToMainMenu);

            var config = ConfigProvider.Config;
            if (config != null && config.inputActions != null)
            {
                var map = config.inputActions.FindActionMap("Gameplay", false);
                if (map != null) _pauseAction = map.FindAction("Pause", false);
            }

            if (_pauseAction == null)
                Debug.LogWarning("[PauseMenuUI] Pause action not found in the InputActions asset.");
        }

        private void OnEnable()
        {
            if (_pauseAction == null) return;
            _pauseAction.Enable();
            _pauseAction.performed += HandlePausePerformed;
        }

        private void OnDisable()
        {
            if (_pauseAction != null) _pauseAction.performed -= HandlePausePerformed;
            SetPaused(false);
        }

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.IsGameOver || gameManager.IsVictory) return;
            SetPaused(!gameManager.IsPaused);
        }

        public void SetPaused(bool paused)
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null) gameManager.SetPaused(paused);

            if (panel != null) panel.SetActive(paused);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Resume()
        {
            SetPaused(false);
        }

        public void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.QuitToMainMenu();
        }
    }
}
