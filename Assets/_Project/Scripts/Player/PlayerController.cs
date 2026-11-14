using UnityEngine;
using UnityEngine.InputSystem;

namespace ChickenShooter
{
    /// <summary>
    /// Reads the New Input System "Gameplay" map exclusively (WASD/arrows, Space, B, Esc/P).
    /// Movement is clamped to the camera viewport and fire is hold-to-shoot.
    /// </summary>
    [RequireComponent(typeof(PlayerWeaponController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 8f;
        public float horizontalMin = 0.05f;
        public float horizontalMax = 0.95f;
        public float verticalMin = 0.06f;
        public float verticalMax = 0.90f;
        public float bankAngle = 14f;
        public ParticleSystem exhaust;
        public ParticleSystem exhaustLeft;
        public ParticleSystem exhaustRight;

        private PlayerWeaponController _weapons;
        private Camera _camera;
        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _fireAction;
        private InputAction _missileAction;
        private bool _fireHeld;
        private Vector2 _moveInput;
        private float _bank;

        private void Awake()
        {
            _weapons = GetComponent<PlayerWeaponController>();
            _camera = Camera.main;

            var config = ConfigProvider.Config;
            if (config != null && config.inputActions != null)
            {
                _gameplayMap = config.inputActions.FindActionMap("Gameplay", false);
                if (_gameplayMap != null)
                {
                    _moveAction = _gameplayMap.FindAction("Move", false);
                    _fireAction = _gameplayMap.FindAction("Fire", false);
                    _missileAction = _gameplayMap.FindAction("SuperMissile", false);
                }
            }

            if (_moveAction == null)
            {
                Debug.LogWarning("[PlayerController] Gameplay input actions are unavailable. " +
                                 "Ship will stay idle until Resources/GameConfig has the InputActions asset assigned.");
            }
        }

        private void OnEnable()
        {
            PlayerLocator.Register(this);

            if (_gameplayMap != null) _gameplayMap.Enable();
            if (_moveAction != null) _moveAction.Enable();
            if (_fireAction != null)
            {
                _fireAction.Enable();
                _fireAction.performed += HandleFirePerformed;
                _fireAction.canceled += HandleFireCanceled;
            }

            if (_missileAction != null)
            {
                _missileAction.Enable();
                _missileAction.performed += HandleMissilePerformed;
            }
        }

        private void OnDisable()
        {
            if (_fireAction != null)
            {
                _fireAction.performed -= HandleFirePerformed;
                _fireAction.canceled -= HandleFireCanceled;
            }

            if (_missileAction != null) _missileAction.performed -= HandleMissilePerformed;

            _fireHeld = false;
            if (_gameplayMap != null) _gameplayMap.Disable();

            PlayerLocator.Unregister(this);
        }

        private void HandleFirePerformed(InputAction.CallbackContext context) => _fireHeld = true;
        private void HandleFireCanceled(InputAction.CallbackContext context) => _fireHeld = false;

        private void HandleMissilePerformed(InputAction.CallbackContext context)
        {
            if (IsGameplayBlocked()) return;
            if (_weapons != null) _weapons.FireSuperMissile();
        }

        private void Update()
        {
            var gameManager = GameManager.Instance;
            bool blocked = gameManager != null && (gameManager.IsGameOver || gameManager.IsVictory || gameManager.IsPaused);

            if (blocked)
            {
                UpdateExhaust(0f);
                return;
            }

            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

            transform.position += new Vector3(_moveInput.x, _moveInput.y, 0f) * (moveSpeed * Time.deltaTime);
            ClampToViewport();

            if (_fireHeld && _weapons != null) _weapons.TryFire();

            float targetBank = -_moveInput.x * bankAngle;
            _bank = Mathf.Lerp(_bank, targetBank, Time.deltaTime * 8f);
            transform.rotation = Quaternion.Euler(0f, 0f, _bank);

            UpdateExhaust(_moveInput.sqrMagnitude);
        }

        private bool IsGameplayBlocked()
        {
            var gameManager = GameManager.Instance;
            return gameManager != null && (gameManager.IsGameOver || gameManager.IsVictory || gameManager.IsPaused);
        }

        private void ClampToViewport()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            float distance = Mathf.Abs(_camera.transform.position.z);
            Vector3 min = _camera.ViewportToWorldPoint(new Vector3(horizontalMin, verticalMin, distance));
            Vector3 max = _camera.ViewportToWorldPoint(new Vector3(horizontalMax, verticalMax, distance));

            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            transform.position = position;
        }

        private void UpdateExhaust(float intensity)
        {
            float rate = Mathf.Lerp(20f, 70f, Mathf.Clamp01(intensity));
            SetEmission(exhaust, rate);
            SetEmission(exhaustLeft, rate * 0.7f);
            SetEmission(exhaustRight, rate * 0.7f);
        }

        private static void SetEmission(ParticleSystem system, float rate)
        {
            if (system == null) return;
            var emission = system.emission;
            emission.rateOverTime = rate;
        }

        public void SetMoveSpeed(float speed) => moveSpeed = Mathf.Max(0.1f, speed);
    }
}
