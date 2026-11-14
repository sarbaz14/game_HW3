namespace ChickenShooter
{
    /// <summary>State Pattern contract used by the boss finite state machine.</summary>
    public interface IBossState
    {
        void Enter(BossController boss);
        void Tick(BossController boss, float deltaTime);
        void Exit(BossController boss);
    }
}
