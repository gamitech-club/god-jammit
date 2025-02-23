using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using EasyTransition;

public class PauseMenu : MenuPage
{
    #region Singleton
    private static PauseMenu _instance;
    public static PauseMenu Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<PauseMenu>();
            return _instance;
        }
    }
    #endregion
    
    [Header("Pause Menu")]
    [SerializeField] private MenuPage _settingsMenu;
    [SerializeField] private TransitionSettings _transitionSettings;

    public event Action PauseChanged;

    private InputAction _pauseAction;
    private bool _isPaused;

    protected override void Awake()
    {
        base.Awake();
        Assert.IsNotNull(_settingsMenu, $"[{name}] Settings Menu not assigned");
        Assert.IsNotNull(_transitionSettings, $"[{name}] Transition Settings not assigned");
        Assert.IsNotNull(TransitionManager.Instance(), $"[{name}] Transition Manager instance not found in scene");

        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(PauseMenu)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _pauseAction = InputSystem.actions.FindAction("Cancel");
    }

    private void OnEnable()
    {
        Container.Q<Button>("ResumeButton").clicked += OnResumeButtonClicked;;
        Container.Q<Button>("RestartButton").clicked += OnRestartButtonClicked;
        Container.Q<Button>("MainMenuButton").clicked += OnMainMenuButtonClicked;
        Container.Q<Button>("QuitButton").clicked += OnQuitButtonClicked;
        _pauseAction.performed += OnPauseActionPerformed;
    }

    private void OnDisable()
    {
        Container.Q<Button>("ResumeButton").clicked -= OnResumeButtonClicked;;
        Container.Q<Button>("RestartButton").clicked -= OnRestartButtonClicked;
        Container.Q<Button>("MainMenuButton").clicked -= OnMainMenuButtonClicked;
        Container.Q<Button>("QuitButton").clicked -= OnQuitButtonClicked;
        _pauseAction.performed -= OnPauseActionPerformed;
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        Show();
        TryFocus();
        PauseChanged?.Invoke();
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        Hide();
        PauseChanged?.Invoke();
    }

    private void OnPauseActionPerformed(InputAction.CallbackContext ctx)
    {
        bool canPause = 
            _settingsMenu.IsHidden &&
            !TransitionManager.Instance().IsTransitioning &&
            GunSelectionMenu.Instance.IsHidden &&
            GunUpgradeMenu.Instance.IsHidden &&
            !Player.Instance.IsGameOver;
        
        if (!canPause)
            return;

        if (_isPaused) Resume();
        else Pause();
    }

    private void OnResumeButtonClicked()
    {
        if (!_isPaused)
            return;

        Resume();
    }

    private void OnRestartButtonClicked()
    {
        if (!_isPaused)
            return;

        Resume();
        TransitionManager.Instance().Transition(SceneManager.GetActiveScene().buildIndex, _transitionSettings, 0f);
    }

    private void OnMainMenuButtonClicked()
    {
        if (!_isPaused)
            return;

        Resume();
        TransitionManager.Instance().Transition(0, _transitionSettings, 0f);
    }

    private void OnQuitButtonClicked()
    {
        if (!_isPaused)
            return;
        
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public static bool IsPaused()
    {
        if (Instance == null)
        {
            Debug.LogWarning("PauseMenu not found in the scene.");
            return false;
        }

        return Instance._isPaused;
    }
}
