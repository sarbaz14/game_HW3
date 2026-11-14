using System.Collections.Generic;
using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Owns the formation anchor: horizontal sway, slow descent and the dive-bomb timer.
    /// Chickens in formation continuously converge on their slot position.
    /// </summary>
    public class EnemyFormationController : MonoBehaviour
    {
        public float swayAmplitude = 1.8f;
        public float swaySpeed = 0.8f;
        public float descentSpeed = 0.12f;
        public float minAnchorY = 1.1f;
        public float diveInterval = 5f;

        private readonly List<Vector2> _slotOffsets = new List<Vector2>();
        private readonly List<EnemyController> _members = new List<EnemyController>();
        private float _minOffsetY;
        private float _phase;
        private float _diveTimer;
        private Vector3 _anchor = new Vector3(0f, 2.2f, 0f);

        public Vector3 Anchor => _anchor;
        public bool Active { get; set; } = true;

        public void SetSlotOffsets(List<Vector2> offsets)
        {
            _slotOffsets.Clear();
            _minOffsetY = 0f;
            if (offsets != null)
            {
                _slotOffsets.AddRange(offsets);
                for (int i = 0; i < offsets.Count; i++)
                {
                    if (offsets[i].y < _minOffsetY) _minOffsetY = offsets[i].y;
                }
            }
        }

        public Vector3 GetBaseSlotPosition(int slotIndex)
        {
            Vector2 offset = slotIndex >= 0 && slotIndex < _slotOffsets.Count ? _slotOffsets[slotIndex] : Vector2.zero;
            return _anchor + new Vector3(offset.x, offset.y, 0f);
        }

        public Vector3 GetSlotPosition(int slotIndex)
        {
            Vector2 offset = slotIndex >= 0 && slotIndex < _slotOffsets.Count ? _slotOffsets[slotIndex] : Vector2.zero;
            float bob = Mathf.Sin(_phase * 1.7f + slotIndex * 0.55f) * 0.07f;
            return _anchor + new Vector3(offset.x, offset.y + bob, 0f);
        }

        public void Register(EnemyController enemy)
        {
            if (enemy == null || _members.Contains(enemy)) return;
            _members.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            if (enemy == null) return;
            _members.Remove(enemy);
        }

        private void Update()
        {
            if (!Active) return;

            float dt = Time.deltaTime;
            _phase += dt * swaySpeed;

            _anchor.x = Mathf.Sin(_phase) * swayAmplitude;
            float effectiveMinAnchorY = Mathf.Max(minAnchorY, 1.0f - _minOffsetY);
            if (_anchor.y > effectiveMinAnchorY) _anchor.y -= descentSpeed * dt;
            else _anchor.y = effectiveMinAnchorY;

            _diveTimer += dt;
            if (_diveTimer >= Mathf.Max(1f, diveInterval))
            {
                _diveTimer = 0f;
                TriggerRandomDive();
            }
        }

        private void TriggerRandomDive()
        {
            if (_members.Count == 0) return;

            int start = Random.Range(0, _members.Count);
            for (int i = 0; i < _members.Count; i++)
            {
                var member = _members[(start + i) % _members.Count];
                if (member == null) continue;
                if (!member.IsInFormation) continue;

                member.StartDive();
                return;
            }
        }
    }
}
