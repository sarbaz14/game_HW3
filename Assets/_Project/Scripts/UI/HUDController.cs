using UnityEngine;
using UnityEngine.UI;

namespace ChickenShooter
{
    /// <summary>In-game HUD. Updates exclusively through GameEvents (no gameplay references).</summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Text")]
        public Text stageText;
        public Text scoreText;
        public Text missileText;
        public Text weaponText;
        public Text foodLabel;

        [Header("Bars")]
        public Slider foodSlider;
        public Slider bossSlider;

        [Header("Lives")]
        public Image[] lifeIcons = new Image[3];

        [Header("Boss bar")]
        public GameObject bossPanel;
        public Text bossNameText;

        [Header("Banner")]
        public GameObject bannerPanel;
        public Text bannerText;

        private void OnEnable()
        {
            GameEvents.OnScoreChanged += HandleScore;
            GameEvents.OnLivesChanged += HandleLives;
            GameEvents.OnWeaponPowerChanged += HandleWeapon;
            GameEvents.OnMissilesChanged += HandleMissiles;
            GameEvents.OnFoodProgressChanged += HandleFood;
            GameEvents.OnRoundChanged += HandleRound;
            GameEvents.OnBossSpawned += HandleBossSpawned;
            GameEvents.OnBossHealthChanged += HandleBossHealth;
            GameEvents.OnBossDefeated += HandleBossDefeated;
            GameEvents.OnStageCompleted += HandleStageCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged -= HandleScore;
            GameEvents.OnLivesChanged -= HandleLives;
            GameEvents.OnWeaponPowerChanged -= HandleWeapon;
            GameEvents.OnMissilesChanged -= HandleMissiles;
            GameEvents.OnFoodProgressChanged -= HandleFood;
            GameEvents.OnRoundChanged -= HandleRound;
            GameEvents.OnBossSpawned -= HandleBossSpawned;
            GameEvents.OnBossHealthChanged -= HandleBossHealth;
            GameEvents.OnBossDefeated -= HandleBossDefeated;
            GameEvents.OnStageCompleted -= HandleStageCompleted;
        }

        private void Start()
        {
            if (bossPanel != null) bossPanel.SetActive(false);
            if (bannerPanel != null) bannerPanel.SetActive(false);
            RefreshFromGameManager();
        }

        private void RefreshFromGameManager()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            HandleScore(gameManager.TotalScore);
            HandleLives(gameManager.RemainingLives);
            HandleWeapon(gameManager.WeaponPowerLevel);
            HandleMissiles(gameManager.SecondaryMissiles);
            HandleFood(gameManager.FoodProgress / 100f);
            HandleRound(gameManager.LevelIndex, gameManager.RoundIndex);
        }

        private void HandleScore(int score)
        {
            if (scoreText != null) scoreText.text = "SCORE\n" + score.ToString("N0");
        }

        private void HandleLives(int lives)
        {
            if (lifeIcons == null) return;
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null) lifeIcons[i].enabled = i < lives;
            }
        }

        private void HandleWeapon(int power)
        {
            if (weaponText != null) weaponText.text = "POWER " + new string('I', Mathf.Clamp(power, 1, 3));
        }

        private void HandleMissiles(int missiles)
        {
            if (missileText != null) missileText.text = "x" + Mathf.Max(0, missiles);
        }

        private void HandleFood(float normalized)
        {
            if (foodSlider != null) foodSlider.value = Mathf.Clamp01(normalized);
            if (foodLabel != null) foodLabel.text = "MISSILE CHARGE " + Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f) + "%";
        }

        private void HandleRound(int level, int round)
        {
            if (stageText != null) stageText.text = "LEVEL " + level + "  -  ROUND " + round;
        }

        private void HandleBossSpawned(string bossName, float currentHp, float maxHp)
        {
            if (bossPanel != null) bossPanel.SetActive(true);
            if (bossNameText != null) bossNameText.text = bossName.ToUpperInvariant();
            if (bossSlider != null) bossSlider.value = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 1f;
        }

        private void HandleBossHealth(float currentHp, float maxHp)
        {
            if (bossSlider != null) bossSlider.value = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        }

        private void HandleBossDefeated()
        {
            if (bossPanel != null) bossPanel.SetActive(false);
        }

        private void HandleStageCompleted(string message)
        {
            ShowBanner(message);
        }

        private void ShowBanner(string message)
        {
            if (bannerPanel == null) return;

            bannerPanel.SetActive(true);
            if (bannerText != null) bannerText.text = message;
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), 2.4f);
        }

        private void HideBanner()
        {
            if (bannerPanel != null) bannerPanel.SetActive(false);
        }
    }
}
