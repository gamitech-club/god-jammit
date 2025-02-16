using UnityEngine;
using DG.Tweening;
using KBCore.Refs;
using UnityEngine.Assertions;

public class Arrow : ValidatedMonoBehaviour
{
    [SerializeField, Self] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _moveDistance = .4f;  // The distance to move up and down
    [SerializeField] private float _moveDuration = .8f;  // Duration for one move cycle

    private Player _player;
    private float _targetY;

    private void Awake()
    {
        _player = Player.Instance;
        Assert.IsNotNull(_player, $"[{name}] Player instance not found in scene");
    }

    private void Start()
    {
        // Start levitating
        _targetY = transform.localPosition.y + _moveDistance;
        transform.DOLocalMoveY(_targetY, _moveDuration)
            .SetLink(gameObject)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    // Update is called once per frame
    void Update()
    {
        var gun = _player.ActiveGun;
        _spriteRenderer.enabled = gun && gun.IsJammed;
    }
}
