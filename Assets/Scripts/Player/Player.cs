using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using KBCore.Refs;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : ValidatedMonoBehaviour
{
    #region Singleton
    private static Player _instance;
    public static Player Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<Player>();
            return _instance;
        }
    }
    #endregion

    [SerializeField, Self] private Rigidbody2D _rb;
    [SerializeField] private Transform _weaponHolder;

    [Header("Ground Checking")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Bounds _groundCheckBounds = new(Vector3.zero, Vector3.one);
    [SerializeField] private float _voidHeight = -100f;

    [Header("Movement")]
    [SerializeField] private float _maxSpeed = 6f;
    [SerializeField] private float _acceleration = 2f;
    [SerializeField] private float _deceleration = 1f;
    [SerializeField] private float _jumpPower = 8.5f;

    [Header("Editor")]
    [SerializeField] private Color _gizmosColor = Color.magenta;
    [SerializeField] private bool _drawGroundCheck = true;

    public const string Tag = "Player";

    // Public properties
    public bool IsGrounded => _isGrounded;
    public Vector2 InputMove => _inputMove;
    public Vector2 Velocity => _rb.linearVelocity;
    public Gun ActiveGun => _activeGun;
    public int Score => _score;
    public bool IsNewHighScore => _isNewHighScore;
    public bool IsGameOver => _isGameOver;

    public event Action Jumped;
    public event Action Grounded;
    public event Action LeftGround;
    public event Action Fired;
    public event Action ScoreAdded;

    private Camera2D _camera;
    private Gun _activeGun;
    private bool _isGrounded;
    private bool _isNewHighScore;
    private bool _isGameOver;
    private bool _canReceiveMoveInput;
    private float _moveMotionX;
    private float _knockbackMotionX;
    private int _score;

    // Input
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _attackAction;
    private InputAction _reloadAction;
    private Vector2 _inputMove;

    private void Awake()
    {
        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(Player)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _camera = Camera2D.Current;
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _attackAction = InputSystem.actions.FindAction("Attack");
        _reloadAction = InputSystem.actions.FindAction("Reload");
        EquipGun(FindActiveGun());

        Assert.IsNotNull(_camera, $"[{name}] Camera instance not found in scene");
        Assert.IsNotNull(_weaponHolder, $"[{name}] WeaponHolder not assigned");
        Assert.IsNotNull(GameOverMenu.Instance, $"[{name}] GameOverMenu instance not found in scene");
    }

    private void Start()
    {
        SetupCamera();
    }

    private void Update()
    {
        GatherInput();
        HandleJumpInput();
        HandleWeaponRotation();
        HandleEndlessFalling();
    }

    private void FixedUpdate()
    {
        HandleCheckGround();
        HandleLateralMovement();
        HandleKnockbackMotionX();

        // Apply horizontal motion
        _rb.linearVelocityX = _moveMotionX + _knockbackMotionX;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _gizmosColor;
        if (_drawGroundCheck)
            Gizmos.DrawWireCube(transform.position + _groundCheckBounds.center, _groundCheckBounds.size);
    }

    private void GatherInput()
    {
        _canReceiveMoveInput =
            !PauseMenu.IsPaused() &&
            !GunRepairUI.Instance.IsActive &&
            GunSelectionMenu.Instance.IsHidden &&
            !_isGameOver;

        // Moving
        _inputMove = _canReceiveMoveInput
            ? _moveAction.ReadValue<Vector2>()
            : Vector2.zero;
        
        // Shooting
        var hasGun = _activeGun != null;
        if (hasGun && _canReceiveMoveInput && _attackAction.IsPressed())
        {
            if (_activeGun.CanFire())
            {
                _activeGun.Fire();
                Fired?.Invoke();
            }
            else if (_activeGun.Ammo == 0 && _activeGun.CanReload())
            {
                _activeGun.StartReload();
            }
        }

        // Reloading
        if (hasGun && _canReceiveMoveInput && _reloadAction.WasPressedThisFrame() && _activeGun.CanReload())
            _activeGun.StartReload();
    }

    private void HandleCheckGround()
    {
        var point = transform.position + _groundCheckBounds.center;
        var willLand = Physics2D.OverlapBox(point, _groundCheckBounds.size, 0f, _groundLayer);

        if (!_isGrounded && willLand)
            Grounded?.Invoke();
        else if (_isGrounded && !willLand)
            LeftGround?.Invoke();

        _isGrounded = willLand;
    }

    private void HandleLateralMovement()
    {
        if (Mathf.Abs(_inputMove.x) < .1f)
        {
            _moveMotionX = Mathf.MoveTowards(_moveMotionX, 0, _deceleration);
        }
        else
        {
            var dir = _inputMove.x > 0f ? 1f : -1f;
            _moveMotionX = Mathf.MoveTowards(_moveMotionX, dir * _maxSpeed, _acceleration);
        }
    }

    private void HandleKnockbackMotionX()
    {
        const float dissipation = 1f;
        _knockbackMotionX = Mathf.MoveTowards(_knockbackMotionX, 0, dissipation);
    }

    private void HandleJumpInput()
    {
        if (!_canReceiveMoveInput || !_jumpAction.enabled)
            return;

        bool jumpPressed = _jumpAction.WasPressedThisFrame();
        if (jumpPressed && _canReceiveMoveInput)
        {
            if (_isGrounded)
            {
                Jump();
            }
        }
    }

    private void HandleWeaponRotation()
    {
        Transform holder = _weaponHolder.transform;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = _camera.ScreenToWorldPoint(mousePos);
        Vector2 dir = (worldPos - (Vector2)holder.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        holder.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void HandleEndlessFalling()
    {
        if (transform.position.y < _voidHeight)
        {
            Debug.Log($"Player fell into the void");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void Jump()
    {
        var vel = _rb.linearVelocity;
        vel.y = _jumpPower;
        _rb.linearVelocity = vel;
        Jumped?.Invoke();
    }
    
    private void SetupCamera()
    {
        _camera.transform.SetParent(null);
        _camera.FinishMotionsImmediately();
    }

    public void AddScore(int amount)
    {
        _score += amount;
        
        if (_score > SavedGame.Instance.HighScore)
        {
            SavedGame.Instance.HighScore = _score;
            SavedGame.Instance.Save();
            _isNewHighScore = true;
        }

        ScoreAdded?.Invoke();
    }

    public void EquipGun(Gun gun)
    {
        if (gun == _activeGun)
            return;
        
        // If we already have a gun, disable & destroy it
        if (_activeGun)
        {
            Debug.Log($"[{name}] Unequipping {_activeGun.name}..");
            _activeGun.gameObject.SetActive(false);
            Destroy(_activeGun.gameObject, .1f);
        }

        var gunTransform = gun.transform;
        gunTransform.SetParent(_weaponHolder);
        gunTransform.localPosition = Vector3.zero;
        gunTransform.localEulerAngles = Vector3.zero;
        _activeGun = gun;

        Debug.Log($"[{name}] Equipped {gun.name}");
    }

    private Gun FindActiveGun()
        => _weaponHolder.transform.GetComponentInChildren<Gun>();
    
    public void GameOver()
    {
        _isGameOver = true;
        GunRepairUI.Instance.Hide();
        GameOverMenu.Instance.Show();
    }
}
