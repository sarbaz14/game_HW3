using System;

namespace ChickenShooter
{
    /// <summary>
    /// Central event hub. Gameplay systems raise events here, UI and audio listen.
    /// No gameplay script may hold a direct reference to a UI widget.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnLivesChanged;
        public static event Action<int> OnWeaponPowerChanged;
        public static event Action<int> OnMissilesChanged;
        public static event Action<float> OnFoodProgressChanged;   // normalized 0..1
        public static event Action<int, int> OnRoundChanged;        // levelNumber, roundNumber
        public static event Action<string, float, float> OnBossSpawned; // bossName, currentHp, maxHp
        public static event Action<float, float> OnBossHealthChanged;   // currentHp, maxHp
        public static event Action OnBossDefeated;
        public static event Action<string> OnStageCompleted;
        public static event Action OnPlayerDamaged;
        public static event Action OnGameOver;
        public static event Action OnGameVictory;
        public static event Action<bool> OnPauseStateChanged;
        public static event Action OnScreenClearTriggered;

        public static void RaiseScoreChanged(int score) => OnScoreChanged?.Invoke(score);
        public static void RaiseLivesChanged(int lives) => OnLivesChanged?.Invoke(lives);
        public static void RaiseWeaponPowerChanged(int power) => OnWeaponPowerChanged?.Invoke(power);
        public static void RaiseMissilesChanged(int missiles) => OnMissilesChanged?.Invoke(missiles);
        public static void RaiseFoodProgressChanged(float normalized) => OnFoodProgressChanged?.Invoke(normalized);
        public static void RaiseRoundChanged(int level, int round) => OnRoundChanged?.Invoke(level, round);
        public static void RaiseBossSpawned(string bossName, float currentHp, float maxHp) => OnBossSpawned?.Invoke(bossName, currentHp, maxHp);
        public static void RaiseBossHealthChanged(float currentHp, float maxHp) => OnBossHealthChanged?.Invoke(currentHp, maxHp);
        public static void RaiseBossDefeated() => OnBossDefeated?.Invoke();
        public static void RaiseStageCompleted(string message) => OnStageCompleted?.Invoke(message);
        public static void RaisePlayerDamaged() => OnPlayerDamaged?.Invoke();
        public static void RaiseGameOver() => OnGameOver?.Invoke();
        public static void RaiseGameVictory() => OnGameVictory?.Invoke();
        public static void RaisePauseStateChanged(bool paused) => OnPauseStateChanged?.Invoke(paused);
        public static void RaiseScreenClearTriggered() => OnScreenClearTriggered?.Invoke();
    }
}
