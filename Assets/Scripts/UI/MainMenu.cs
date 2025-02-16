using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using EasyTransition;
using KBCore.Refs;

public class MainMenu : MenuPage
{
    [Header("Main Menu")]
    [SerializeField, Scene] private Camera2D _camera;
    [SerializeField] private TransitionSettings _transitionSettings;
    [SerializeField, Multiline] private string _highscoreLabelFormat = "HIGHSCORE\n{0}";

    protected override void Start()
    {
        base.Start();
        Assert.IsNotNull(_transitionSettings, $"[{name}] Transition Settings not assigned");
        Assert.IsNotNull(TransitionManager.Instance(), $"[{name}] Transition Manager instance not found in scene");

        Container.Q<Button>("PlayButton").clicked += OnPlayButtonClicked;
        Container.Q<Button>("QuitButton").clicked += OnQuitButtonClicked;
        Container.Q<Label>("VersionLabel").text = $"v{Application.version}";
        Container.Q<Label>("HighscoreLabel").text = string.Format(_highscoreLabelFormat, SavedGame.Instance.HighScore);
        
        TryFocus();
        _camera.AddPersistentShake(.1f, 1.6f);
    }

    private void OnPlayButtonClicked()
    {
        var nextSceneindex = SceneManager.GetActiveScene().buildIndex + 1;
        TransitionManager.Instance().Transition(nextSceneindex, _transitionSettings, 0f);
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
