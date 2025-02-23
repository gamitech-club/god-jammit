using UnityEngine;
using KBCore.Refs;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : ValidatedMonoBehaviour, IDamageable
{
    [SerializeField, Self] private Rigidbody2D _rb;

    [Header("Enemy Settings")]
    [SerializeField] private float _speed = .4f;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private int _scoreValue = 10;
    [SerializeField] private bool _isSpawnedFromLeft;
    [SerializeField] private bool _isFlying; 

    [Header("FX")]
    [SerializeField, Child] private SpriteRenderer _spriteRenderer;
    [SerializeField, Child] private ParticleSystem _fxDeath;

    private Tween _rotateTween;
    private Tween _colorTween;

    private Color _defaultSpriteColor;
    private float _health;
    private bool _isDead;
    private Transform _target; 

    public bool IsSpawnedFromLeft
{
    get => _isSpawnedFromLeft;
    set
    {
        _isSpawnedFromLeft = value;
        if (!_isFlying) 
        {
            _spriteRenderer.flipX = !_isSpawnedFromLeft;
        }
    }
}

    private void Awake()
    {
        _health = _maxHealth;
        _defaultSpriteColor = _spriteRenderer.color;
        if (!_isFlying && Player.Instance != null)
        {
            _target = Player.Instance.transform;
        }
    }

    private void Start()
    {
        // Start the coroutine to adjust rotation every 0.5 seconds
        _target = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(AdjustRotationPeriodically());
    }

    
    private void Update()
    {
        if (!_isFlying)
        {
            // Flip sprite based on spawn direction for grounded enemies
            _spriteRenderer.flipX = !_isSpawnedFromLeft;
        }
    }

    private void FixedUpdate()
    {
        if (_isDead || (_isFlying && !_target))
            return;

        if (_isFlying)
        {
            FlyTowardsPlayer();
        }
        else
        {
            MoveOnGround();
        }
    }

    private void MoveOnGround()
    {
        float dir = _isSpawnedFromLeft ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * _speed, _rb.linearVelocity.y);
    }

    private void FlyTowardsPlayer()
    {
        if (!_target) return;

        Vector2 direction = (_target.position - transform.position).normalized;
        Vector2 newPosition = _rb.position + direction * _speed * Time.fixedDeltaTime;

        _rb.MovePosition(newPosition);

        _spriteRenderer.flipX = direction.x < 0;
    }

    public void TakeDamage(float damage)
    {
        if (_isDead)
            return;

        _health -= damage;

        // Effects
        _spriteRenderer.color = Color.red;
        _rotateTween?.Kill();
        _colorTween?.Kill();
        _rotateTween = _spriteRenderer.transform.DOPunchRotation(new(0, 0, Random.Range(-45f, 45f)), .1f).SetLink(gameObject);
        _colorTween = _spriteRenderer.DOColor(_defaultSpriteColor, .2f).SetLink(gameObject);

        if (_health <= 0f)
            Die();
    }

    private void Die()
    {
        if (_scoreValue >= 100)
            GunUpgradeMenu.Instance.TryShowUpgrade();
        Player.Instance.AddScore(_scoreValue);

        _fxDeath.transform.SetParent(null);
        _fxDeath.Play();
        Destroy(gameObject);
    }   

    public void ToggleFlying(bool enableFlying)
    {
        _isFlying = enableFlying;
        _rb.gravityScale = enableFlying ? 0 : 1; // Disable gravity when flying
    }

    private IEnumerator AdjustRotationPeriodically()
    {
        while (true)
        {
            // Reset the rotation to keep the sprite upright
            transform.GetChild(0).transform.rotation = Quaternion.Euler(0, 0, 0);

            // Wait for 0.5 seconds before the next adjustment
            yield return new WaitForSeconds(0.5f);
        }
    }
    }
