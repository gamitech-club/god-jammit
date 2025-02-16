using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using DG.Tweening;

public class SettingsMenu : MenuPage
{
    [Header("Settings Menu")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private bool _isInPauseMenu;

    private Tween _delayedSaveTween;
    private bool _isEventsRegistered;

    protected override void Start()
    {
        base.Start();
        Assert.IsNotNull(_audioMixer, $"[{name}] AudioMixer not assigned");

        if (_isInPauseMenu)
            Container.AddToClassList("in-pause-menu");

        SetupSettings();
    }

    private void SetupSettings()
    {
        var settings = SavedSettings.Instance;

        // Setup sliders
        SetupSlider("MasterVolumeSlider", settings.MasterVolume, OnMasterVolumeSliderChanged);
        SetupSlider("MusicVolumeSlider", settings.MusicVolume, OnMusicVolumeSliderChanged);
        SetupSlider("SFXVolumeSlider", settings.SFXVolume, OnSFXVolumeSliderChanged);
        SetupToggle("CameraShakeToggle", settings.CameraShakeEnabled, OnCameraShakeToggleChanged);

        // "Reset" button
        if (!_isEventsRegistered)
            Container.Q<Button>("ResetButton").clicked += OnResetButtonClicked;

        _isEventsRegistered = true;

        // Apply settings
        SetAudioVolume("MasterVolume", settings.MasterVolume);
        SetAudioVolume("MusicVolume", settings.MusicVolume);
        SetAudioVolume("SFXVolume", settings.SFXVolume);
    }

    private Slider SetupSlider(string sliderName, float initialValue, Action<float> onValueChanged)
    {
        Slider slider = Container.Q<Slider>(sliderName);
        Assert.IsNotNull(slider, $"[{name}] Slider named '{sliderName}' not found.");

        slider.SetValueWithoutNotify(initialValue);
        if (!_isEventsRegistered)
            slider.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));

        return slider;
    }

    private Toggle SetupToggle(string toggleName, bool initialValue, Action<bool> onValueChanged)
    {
        Toggle toggle = Container.Q<Toggle>(toggleName);
        Assert.IsNotNull(toggle, $"[{name}] Toggle named '{toggleName}' not found.");

        toggle.SetValueWithoutNotify(initialValue);
        if (!_isEventsRegistered)
            toggle.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));

        return toggle;
    }

    private void SetAudioVolume(string audio, float normalizedVolume)
    {
        if (!_audioMixer.SetFloat(audio, Mathf.Lerp(-80f, 20f, normalizedVolume)))
            Debug.LogError($"Failed to set audio '{audio}'", this);
    }

    private void DelayedSave(float delay = .2f)
    {
        _delayedSaveTween?.Kill();
        _delayedSaveTween = DOVirtual.DelayedCall(delay, Save).SetLink(gameObject);
    }

    private void Save()
        => SavedSettings.Instance.Save();

    private void OnMasterVolumeSliderChanged(float value)
    {
        SetAudioVolume("MasterVolume", value);
        SavedSettings.Instance.MasterVolume = value;
        DelayedSave();
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        SetAudioVolume("MusicVolume", value);
        SavedSettings.Instance.MusicVolume = value;
        DelayedSave();
    }

    private void OnSFXVolumeSliderChanged(float value)
    {
        SetAudioVolume("SFXVolume", value);
        SavedSettings.Instance.SFXVolume = value;
        DelayedSave();
    }

    private void OnCameraShakeToggleChanged(bool value)
    {
        SavedSettings.Instance.CameraShakeEnabled = value;
        DelayedSave();
    }

    private void OnResetButtonClicked()
    {
        SavedSettings.DeleteFile();
        SavedSettings.ResetInstance();
        SetupSettings();
    }
}
