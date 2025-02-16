using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using EasyTransition;

public class GameOverMenu : MenuPage
{
    #region Singleton
    private static GameOverMenu _instance;
    public static GameOverMenu Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<GameOverMenu>();
            return _instance;
        }
    }
    #endregion

    [Header("GameOver Menu")]
    [SerializeField] private TransitionSettings _transitionSettings;

    private Spawner _spawner;
    private Label _statsLabel;
    private string _statsTextFormat;

    protected override void Awake()
    {
        base.Awake();

        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(PauseMenu)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _spawner = FindFirstObjectByType<Spawner>();
        _statsLabel = Container.Q<Label>("StatsLabel");
        _statsTextFormat = _statsLabel?.text;

        Assert.IsNotNull(_transitionSettings, $"[{name}] Transition Settings not assigned");
        Assert.IsNotNull(_spawner, $"[{name}] {nameof(Spawner)} instance not found in scene");
        Assert.IsNotNull(_statsLabel, $"[{name}] Stats label not found");
    }

    private void OnEnable()
    {
        Showed += UpdateStats;
        Container.Q<Button>("RestartButton").clicked += OnRestartButtonClicked;
        Container.Q<Button>("MainMenuButton").clicked += OnMainMenuButtonClicked;
    }

    private void OnDisable()
    {
        Showed -= UpdateStats;
        Container.Q<Button>("RestartButton").clicked -= OnRestartButtonClicked;
        Container.Q<Button>("MainMenuButton").clicked -= OnMainMenuButtonClicked;
    }

    private void UpdateStats()
    {
        _statsLabel.text = string.Format(
            _statsTextFormat,
            _spawner.Round,
            Player.Instance.Score,
            SavedGame.Instance.HighScore
        );
    }

    private void OnRestartButtonClicked()
    {
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;
        TransitionManager.Instance().Transition(sceneIndex, _transitionSettings, 0f);
    }

    private void OnMainMenuButtonClicked()
    {
        TransitionManager.Instance().Transition(0, _transitionSettings, 0f);
    }
}
