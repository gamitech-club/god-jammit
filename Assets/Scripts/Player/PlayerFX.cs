using UnityEngine;
using UnityEngine.Assertions;
using KBCore.Refs;
using DG.Tweening;

public class PlayerFX : ValidatedMonoBehaviour
{
    [SerializeField, Self] private Player _plr;
    [SerializeField, Self] private Animator _animator;
    [SerializeField, Child] private SpriteRenderer _spriteRenderer;
    [SerializeField, Anywhere] private Transform _weaponHolder;

    [Header("SFX")]
    [SerializeField, Anywhere] private AudioSource _sfxJump;

    private readonly int _animIdleHash = Animator.StringToHash("PlayerIdle");
    private readonly int _animWalkHash = Animator.StringToHash("PlayerWalk");
    private readonly int _animJumpIdleHash = Animator.StringToHash("PlayerJumpIdle");
    private readonly int _animFallIdleHash = Animator.StringToHash("PlayerFallIdle");
    
    private void Awake()
    {
        Assert.IsNotNull(_sfxJump, $"[{name}] Jump SFX not assigned");
    }

    private void OnEnable()
    {
        _plr.Jumped += OnJumped;
    }

    private void OnDisable()
    {
        _plr.Jumped -= OnJumped;
    }

    private void Update()
    {
        HandleSpriteFlipping();
        HandleAnimation();
    }

    private void HandleSpriteFlipping()
    {
        if (_plr.InputMove.x < 0f)
        {
            _spriteRenderer.flipX = true;

            var localPos = _weaponHolder.localPosition;
            _weaponHolder.localPosition = new Vector3(Mathf.Abs(localPos.x), localPos.y, localPos.z);
        }
        else if (_plr.InputMove.x > 0f)
        {
            _spriteRenderer.flipX = false;

            var localPos = _weaponHolder.localPosition;
            _weaponHolder.localPosition = new Vector3(-Mathf.Abs(localPos.x), localPos.y, localPos.z);
        }
    }

    private void HandleAnimation()
    {
        if (_plr.IsGrounded)
        {
            if (Mathf.Abs(_plr.InputMove.x) > .1f)
                _animator.Play(_animWalkHash);
            else
                _animator.Play(_animIdleHash);
        }
        else
        {
            if (_plr.Velocity.y > 0f)
                _animator.Play(_animJumpIdleHash);
            else if (_plr.Velocity.y < 0f)
                _animator.Play(_animFallIdleHash);
        }
    }

    private void OnJumped()
    {
        _sfxJump.Play();
    }
}
