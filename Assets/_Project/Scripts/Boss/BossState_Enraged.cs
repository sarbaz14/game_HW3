using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Phase 3 (entered whenever HP drops below 30%): crimson tint, 1.5x speed, radial energy
    /// feathers, fast egg volleys and periodic minion summons (which Super Missiles can wipe).
    /// </summary>
    public class BossState_Enraged : IBossState
    {
        private float _eggTimer;
        private float _featherTimer;
        private float _summonTimer;
        private float _sweepPhase;

        public void Enter(BossController boss)
        {
            _eggTimer = 0.6f;
            _featherTimer = 0.9f;
            _summonTimer = 1.4f;
            _sweepPhase = 0f;

            boss.SetTint(boss.enragedTint);
            AudioCue.Play(AudioLibrary.Sfx.ChickenDeath, 0.02f);

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.35f, 0.3f);
        }

        public void Tick(BossController boss, float deltaTime)
        {
            _sweepPhase += deltaTime * 1.5f;
            boss.MoveTarget = new Vector3(
                Mathf.Sin(_sweepPhase) * 4.2f,
                boss.Anchor.y + 0.2f + Mathf.Sin(_sweepPhase * 3f) * 0.3f,
                0f);

            _eggTimer -= deltaTime;
            if (_eggTimer <= 0f)
            {
                _eggTimer = 1.05f;
                boss.FireEggVolley(Random.Range(4, 7), 34f);
            }

            _featherTimer -= deltaTime;
            if (_featherTimer <= 0f)
            {
                _featherTimer = 2.1f;
                boss.FireFeatherRadial(8);
            }

            _summonTimer -= deltaTime;
            if (_summonTimer <= 0f)
            {
                _summonTimer = 6.5f;
                boss.SummonMinions(Random.Range(2, 4));
            }
        }

        public void Exit(BossController boss)
        {
        }
    }
}
