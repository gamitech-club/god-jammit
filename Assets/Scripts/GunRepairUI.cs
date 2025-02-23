using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using KBCore.Refs;
using DG.Tweening;

using Random = UnityEngine.Random;
using TMPro;

public class GunRepairUI : ValidatedMonoBehaviour
{
    #region Singleton
    private static GunRepairUI _instance;
    public static GunRepairUI Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<GunRepairUI>();
            return _instance;
        }
    }
    #endregion

    [SerializeField, Anywhere] private CanvasGroup _container;
    [SerializeField, Anywhere] private Slider _currentSlider;
    [SerializeField, Anywhere] private Slider _correctSlider;
    [SerializeField, Anywhere] private Image _currentSliderHandle;
    [SerializeField, Anywhere] private TextMeshProUGUI _repairsNeededLabel;
    [SerializeField] private float _diffNeeded = .032f;
    [SerializeField] private float _rollSpeed = 1f;

    [Header("SFX")]
    [SerializeField, Anywhere] private AudioSource _sfxRepairCorrectly;
    [SerializeField, Anywhere] private AudioSource _sfxRepairIncorrectly;

    public event Action RepairedCorrectly;
    public event Action RepairedIncorrectly;

    public bool IsActive => _isActive;

    private bool _isActive;
    private bool _isRolling;
    private int _repairsNeeded;
    private int _currentRepairs;
    private float _lastActivatedTime;
    private InputAction _repairAction;
    private Color _defaultHandleColor;
    private string _repairsNeededTextPlaceholder;
    private Sequence _handleFeedbackSequence;

    private void Awake()
    {
        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(GunRepairUI)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _repairAction = InputSystem.actions.FindAction("Repair");
        _defaultHandleColor = _currentSliderHandle.color;
        _repairsNeededTextPlaceholder = _repairsNeededLabel.text;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Hide();
    }

    // Update is called once per frame
    void Update()
    {
        GatherInput();
        HandleRolling();
    }

    private void GatherInput()
    {
        if (_isActive && _repairAction.WasPressedThisFrame())
        {
            float diff = Mathf.Abs(_currentSlider.value - _correctSlider.value);
            if (diff <= _diffNeeded)
                OnRepairCorrectly();
            else
                OnRepairIncorrectly();
        }
    }

    private void HandleRolling()
    {
        if (_isActive)
        {
            var t = TimeSince(_lastActivatedTime);
            _currentSlider.value = Mathf.PingPong(t * _rollSpeed, 1f);
        }
    }

    public void Activate(int repairsNeeded = 3)
    {
        _isActive = true;
        _repairsNeeded = repairsNeeded;
        _currentRepairs = 0;
        _lastActivatedTime = Time.time;
        UpdateRepairsNeededLabel();
        _container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _isActive = false;
        _container.gameObject.SetActive(false);
    }

    public void Randomize()
    {
        _correctSlider.value = Random.Range(.2f, .8f);
    }

    private void UpdateRepairsNeededLabel()
        => _repairsNeededLabel.text = string.Format(_repairsNeededTextPlaceholder, _currentRepairs, _repairsNeeded);
    
    private void UpdateRepairCorrectlySFX()
    {
        bool willSuccess = _currentRepairs + 1 >= _repairsNeeded;
        if (willSuccess)
        {
            _sfxRepairCorrectly.pitch = 1f;
        }
        else
        {
            var pitch = .8f;
            pitch += .1f * _currentRepairs;
            _sfxRepairCorrectly.pitch = Mathf.Clamp(pitch, .8f, 1.1f);
        }

    }

    private void StartHandleFeedbackSequence(Color color)
    {
        var img = _currentSliderHandle;
        _handleFeedbackSequence?.Kill(true);
        _handleFeedbackSequence = DOTween.Sequence()
            .AppendCallback(() => img.color = color)
            .Append(img.DOColor(_defaultHandleColor, .4f))
            .Join(img.transform.DOPunchScale(new Vector3(4f, .3f), .15f))
            .SetLink(img.gameObject);
    }

    private float TimeSince(float since)
        => Time.time - since;

    private void OnRepairCorrectly()
    {
        Randomize();
        StartHandleFeedbackSequence(Color.green);
        UpdateRepairCorrectlySFX();
        _sfxRepairCorrectly.Play();

        _currentRepairs++;
        if (_currentRepairs >= _repairsNeeded)
        {
            Hide();

            var gun = Player.Instance.ActiveGun;
            if (gun && gun.IsJammed)
                gun.UnJam();
        }

        UpdateRepairsNeededLabel();
        RepairedCorrectly?.Invoke();
    }

    private void OnRepairIncorrectly()
    {
        Randomize();
        StartHandleFeedbackSequence(Color.red);
        _sfxRepairIncorrectly.Play();

        _currentRepairs = Mathf.Max(0, _currentRepairs - 1);
        UpdateRepairsNeededLabel();

        RepairedIncorrectly?.Invoke();
    }
}
