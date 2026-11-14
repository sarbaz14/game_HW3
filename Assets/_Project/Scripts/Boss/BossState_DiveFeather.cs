using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Phase 2: telegraph flashes, rapid dive to the player's Y level while firing a fan of
    /// energy feathers, then returns to the top anchor.
    /// </summary>
    public class BossState_DiveFeather : IBossState
    {
        private enum Phase
        {
            Telegraph,
            Dive,
            Return
        }

        private Phase _phase;
        private float _timer;
        private float _featherTimer;
        private Vector3 _diveTarget;

        public void Enter(BossController boss)
        {
            _phase = Phase.Telegraph;
            _timer = 0f;
            _featherTimer = 0.15f;
            boss.SetTint(boss.telegraphTint);
        }

        public void Tick(BossController boss, float deltaTime)
        {
            _timer += deltaTime;

            switch (_phase)
            {
                case Phase.Telegraph:
                    boss.MoveTarget = boss.Anchor + new Vector3(0f, 0.35f, 0f);
                    float pulse = Mathf.PingPong(_timer * 9f, 1f);
                    boss.SetTint(Color.Lerp(boss.telegraphTint, Color.white, pulse));

                    if (_timer >= 0.9f)
                    {
                        _phase = Phase.Dive;
                        _timer = 0f;

                        Vector3 player = boss.PlayerPosition;
                        _diveTarget = new Vector3(
                            Mathf.Clamp(player.x, -5.6f, 5.6f),
                            Mathf.Clamp(player.y + 2.4f, -1.8f, 2.4f),
                            0f);

                        boss.SetTint(Color.white);
                    }
                    break;

                case Phase.Dive:
                    boss.MoveTarget = _diveTarget;

                    _featherTimer -= deltaTime;
                    if (_featherTimer <= 0f)
                    {
                        _featherTimer = 0.30f / boss.SpeedMultiplier;
                        boss.FireFeatherFan(3, 22f);
                    }

                    if (_timer >= 1.9f)
                    {
                        _phase = Phase.Return;
                        _timer = 0f;
                    }
                    break;

                case Phase.Return:
                    boss.MoveTarget = boss.Anchor;
                    if (Vector3.Distance(boss.transform.position, boss.Anchor) < 0.4f || _timer > 2.4f)
                    {
                        boss.FinishDive();
                    }
                    break;
            }
        }

        public void Exit(BossController boss)
        {
            boss.SetTint(Color.white);
        }
    }
}
