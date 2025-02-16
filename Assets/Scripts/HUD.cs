using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using KBCore.Refs;
using DG.Tweening;
using TMPro;

public class HUD : ValidatedMonoBehaviour
{
    #region Singleton
    private static HUD _instance;
    public static HUD Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<HUD>();
            return _instance;
        }
    }
    #endregion

    [SerializeField] private TextMeshPro _cursorLabel;
    [SerializeField, Anywhere] private TextMeshProUGUI _scoreLabel;
    [SerializeField, Anywhere] private TextMeshProUGUI _roundLabel;
    [SerializeField, Multiline(4)] private string _newHighscoreTextFormat = "NEW HIGHSCORE\n{0}";

    public bool ShouldEnableCursorLabel => _shouldEnableCursorLabel;

    private Player _player;
    private Camera2D _camera;
    private Spawner _spawner;
    private Sequence _scoreSequence;
    private Sequence _roundLabelSequence;
    private string _scoreTextFormat;
    private bool _shouldEnableCursorLabel;

    private void Awake()
    {
        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(HUD)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _player = Player.Instance;
        _camera = Camera2D.Current;
        _spawner = FindFirstObjectByType<Spawner>();
        _scoreTextFormat = _scoreLabel.text;

        Assert.IsNotNull(_cursorLabel, $"[{name}] Cursor label not assigned");
        Assert.IsNotNull(_player, $"[{name}] Player instance not found in scene");
        Assert.IsNotNull(_camera, $"[{name}] Camera instance not found in scene");
        Assert.IsNotNull(_spawner, $"[{name}] Spawner instance not found in scene");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _cursorLabel.transform.SetParent(null);
        _roundLabel.DOFade(0f, 0f);

        UpdateScore();
        UpdateRoundLabel(1);
    }

    private void Update()
    {
        HandleCursorLabel();
        HandleScoreLabelState();
    }

    private void OnEnable()
    {
        _player.Fired += OnGunFired;
        _player.ScoreAdded += OnScoreAdded;
        _spawner.RoundCompleted += OnRoundCompleted;
    }

    private void OnDisable()
    {
        _player.Fired -= OnGunFired;
        _player.ScoreAdded -= OnScoreAdded;
        _spawner.RoundCompleted -= OnRoundCompleted;
    }

    private void HandleCursorLabel()
    {
        _shouldEnableCursorLabel =
            !GunRepairUI.Instance.IsActive &&
            !PauseMenu.IsPaused() &&
            _player.ActiveGun &&
            GunUpgradeMenu.Instance.IsHidden &&
            !_player.IsGameOver;
        
        _cursorLabel.enabled = _shouldEnableCursorLabel;
        if (!_shouldEnableCursorLabel)
            return;

        // Update cursor label position
        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = _camera.ScreenToWorldPoint(mousePos);
        _cursorLabel.transform.position = worldPos;

        // Handle text
        var gun = _player.ActiveGun;
        var color = Color.white;
        string text;

        if (GunRepairUI.Instance.IsActive) {
            text = string.Empty;
        } else if (gun.IsJammed) {
            text = "JAMMED!";
            color = new(Mathf.PingPong(Time.time * 10f, 1f), 0.36f, 0.44f);
        } else if (gun.IsReloading) {
            text = "Reloading";
            color = Color.yellow;
        } else {
            text = $"{gun.Ammo}/{gun.MaxAmmo}";
        }
        
        _cursorLabel.text = text;
        _cursorLabel.color = color;
    }

    private void HandleScoreLabelState()
    {
        _scoreLabel.enabled =
            !GunRepairUI.Instance.IsActive &&
            GunSelectionMenu.Instance.IsHidden &&
            GunUpgradeMenu.Instance.IsHidden &&
            !_player.IsGameOver;
    }

    void UpdateScore()
    {
        var score = _player.Score;
        _scoreLabel.text = _player.IsNewHighScore
            ? string.Format(_newHighscoreTextFormat, _player.Score)
            : string.Format(_scoreTextFormat, _player.Score);

        _scoreSequence?.Kill();
        _scoreSequence = DOTween.Sequence()
            .Append(_scoreLabel.transform.DOPunchScale(Vector3.one * .4f, .2f))
            .AppendCallback(() => _scoreLabel.transform.localScale = Vector3.one)
            .SetLink(_scoreLabel.gameObject);
    }

    private void UpdateRoundLabel(int round)
    {
        var roundTransform = _roundLabel.transform;
        _roundLabel.text = $"Round {round}";

        _roundLabelSequence?.Kill();
        _roundLabelSequence = DOTween.Sequence()
            .Append(roundTransform.DOScale(1f, 0f))
            .Join(_roundLabel.DOFade(0f, 0f))
            .Append(_roundLabel.DOFade(1f, 4f))
            .Join(roundTransform.DOScale(1.4f, 4f))
            .Append(_roundLabel.DOFade(0f, .5f))
            .SetLink(_roundLabel.gameObject);
    }

    private void OnGunFired()
        => _cursorLabel.transform.DOPunchScale(Vector3.one * .35f, .1f);

    private void OnScoreAdded()
        => UpdateScore();
    
    private void OnRoundCompleted()
    {
        UpdateRoundLabel(_spawner.Round);
    }
}
