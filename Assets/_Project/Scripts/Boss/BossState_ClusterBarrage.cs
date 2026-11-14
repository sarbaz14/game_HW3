using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Phase 1: sweeps across the top of the arena while firing clustered egg volleys.</summary>
    public class BossState_ClusterBarrage : IBossState
    {
        private const float Duration = 6f;

        private float _elapsed;
        private float _fireTimer;
        private float _sweepPhase;

        public void Enter(BossController boss)
        {
            _elapsed = 0f;
            _fireTimer = 0.6f;
            _sweepPhase = boss.transform.position.x / 4f;
            boss.SetTint(Color.white);
        }

        public void Tick(BossController boss, float deltaTime)
        {
            _sweepPhase += deltaTime * 0.85f;

            boss.MoveTarget = new Vector3(
                Mathf.Sin(_sweepPhase) * 3.8f,
                boss.Anchor.y + Mathf.Sin(_sweepPhase * 2.1f) * 0.25f,
                0f);

            _fireTimer -= deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = 1.25f / boss.SpeedMultiplier;
                boss.FireEggVolley(Random.Range(3, 6), 26f);
            }

            _elapsed += deltaTime;
            if (_elapsed >= Duration / boss.SpeedMultiplier)
            {
                boss.ChangeState(boss.DiveState);
            }
        }

        public void Exit(BossController boss)
        {
        }
    }
}
