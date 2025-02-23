using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using KBCore.Refs;

public class GunUpgradeMenu : MenuPage
{
    #region Singleton
    private static GunUpgradeMenu _instance;
    public static GunUpgradeMenu Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<GunUpgradeMenu>();
            return _instance;
        }
    }
    #endregion

    [Header("Gun Upgrade Menu")]
    [SerializeField, Child] private AudioSource _sfxUpgrade;
    [SerializeField] private Sprite _reloadTimeSprite;
    [SerializeField] private Sprite _damageSprite;
    [SerializeField] private Sprite _jamChanceSprite;
    [SerializeField] private int _pointsPerUpgrade = 100;

    [Header("Gun Upgrades")]
    [SerializeField, Range(0f, 1f)] private float _reloadTimeReduce = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _damageIncrease = .15f;
    [SerializeField, Range(0f, 1f)] private float _jamChanceReduce = .1f;

    [Header("Max Upgrades")]
    [SerializeField] private float _reloadTimeMaxMult = 0.25f;
    [SerializeField] private float _damageMaxMult = 2f;
    [SerializeField] private float _jamChanceMaxMult = 0.1f;

    private Player _player;
    private bool _canUpgrade;

    // Upgrade buttons
    private Button _reloadTimeButton;
    private Button _damageButton;
    private Button _jamChanceButton;

    protected override void Awake()
    {
        base.Awake();
        Assert.IsNotNull(Player.Instance, $"[{name}] Player instance not found in scene");
        Assert.IsNotNull(_reloadTimeSprite, $"[{name}] Reload Time sprite not assigned");
        Assert.IsNotNull(_damageSprite, $"[{name}] Damage sprite not assigned");
        Assert.IsNotNull(_jamChanceSprite, $"[{name}] Jam Chance sprite not assigned");

        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(GunUpgradeMenu)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _player = Player.Instance;
        _reloadTimeButton = Container.Q("ReloadTimeCard").Q<Button>();
        _damageButton = Container.Q("DamageCard").Q<Button>();
        _jamChanceButton = Container.Q("JamChanceCard").Q<Button>();
        MakeCards();
    }

    private void OnEnable()
    {
        _player.ScoreAdded += OnScoreAdded;
        _reloadTimeButton.clicked += OnUpgradeReloadTimeButtonClicked;
        _damageButton.clicked += OnUpgradeDamageButtonClicked;
        _jamChanceButton.clicked += OnUpgradeJamChanceButtonClicked;
    }

    private void OnDisable()
    {
        _player.ScoreAdded -= OnScoreAdded;
        _reloadTimeButton.clicked -= OnUpgradeReloadTimeButtonClicked;
        _damageButton.clicked -= OnUpgradeDamageButtonClicked;
        _jamChanceButton.clicked -= OnUpgradeJamChanceButtonClicked;
    }

    private void MakeCards()
    {
        var green = "#3e6958";
        _reloadTimeButton.Q<Label>("CardLabel").text = $"Reload";
        _reloadTimeButton.Q("ImageElement").style.backgroundImage = new(_reloadTimeSprite);
        _reloadTimeButton.Q<Label>("DescriptionLabel").text = $"Reduce reload time by <color={green}>{_reloadTimeReduce * 100}%</color>";

        _damageButton.Q<Label>("CardLabel").text = $"Damage";
        _damageButton.Q("ImageElement").style.backgroundImage = new(_damageSprite);
        _damageButton.Q<Label>("DescriptionLabel").text = $"Increase damage by <color={green}>{_damageIncrease * 100}%</color>";

        _jamChanceButton.Q<Label>("CardLabel").text = $"Jam Chance";
        _jamChanceButton.Q("ImageElement").style.backgroundImage = new(_jamChanceSprite);
        _jamChanceButton.Q<Label>("DescriptionLabel").text = $"Reduce jam chance by <color={green}>{_jamChanceReduce * 100}%</color>";
    }

    public void TryShowUpgrade()
    {
        var gun = _player.ActiveGun;
        if (!gun)
        {
            Debug.LogError($"[{nameof(GunUpgradeMenu)}] Gun upgrade is needed but there is no active gun");
            return;
        }

        bool canUpgradeReloadTime = gun.ReloadTimeUpgradeMultiplier - _reloadTimeReduce >= _reloadTimeMaxMult;
        bool canUpgradeDamage = gun.DamageUpgradeMultiplier + _damageIncrease <= _damageMaxMult;
        bool canUpgradeJamChance = gun.JamChanceUpgradeMultiplier - _jamChanceReduce >= _jamChanceMaxMult;

        if (!canUpgradeReloadTime && !canUpgradeDamage && !canUpgradeJamChance)
        {
            Debug.Log($"[{name}] All upgrades are maxed out!");
            return;
        }

        Debug.Log($"[{name}] Gun upgrade available!");

        // If a stat is already at max, disable it
        _reloadTimeButton.SetEnabled(canUpgradeReloadTime);
        _damageButton.SetEnabled(canUpgradeDamage);
        _jamChanceButton.SetEnabled(canUpgradeJamChance);

        Time.timeScale = 0f;
        _canUpgrade = true;
        Show();
    }

    private void OnScoreAdded()
    {
        if (_player.Score % _pointsPerUpgrade != 0 || _player.IsGameOver)
            return;

        TryShowUpgrade();
    }

    private void OnUpgradeReloadTimeButtonClicked()
    {
        if (!_canUpgrade)
            return;

        var prevValue = _player.ActiveGun.ReloadTimeUpgradeMultiplier;
        _player.ActiveGun.ReloadTimeUpgradeMultiplier -= _reloadTimeReduce;
        Debug.Log($"Upgraded reload time from {prevValue:f2} to {_player.ActiveGun.ReloadTimeUpgradeMultiplier:f2}");

        OnAnyUpgradeButtonClicked();
    }

    private void OnUpgradeDamageButtonClicked()
    {
        if (!_canUpgrade)
            return;

        var prevValue = _player.ActiveGun.DamageUpgradeMultiplier;
        _player.ActiveGun.DamageUpgradeMultiplier += _damageIncrease;
        Debug.Log($"Upgraded damage from {prevValue:f2} to {_player.ActiveGun.DamageUpgradeMultiplier:f2}");

        OnAnyUpgradeButtonClicked();
    }

    private void OnUpgradeJamChanceButtonClicked()
    {
        if (!_canUpgrade)
            return;

        var prevValue = _player.ActiveGun.JamChanceUpgradeMultiplier;
        _player.ActiveGun.JamChanceUpgradeMultiplier -= _jamChanceReduce;
        Debug.Log($"Upgraded jam chance from {prevValue:f2} to {_player.ActiveGun.JamChanceUpgradeMultiplier:f2}");

        OnAnyUpgradeButtonClicked();
    }

    private void OnAnyUpgradeButtonClicked()
    {
        Time.timeScale = 1f;
        _canUpgrade = false;
        _sfxUpgrade.Play();
        Hide();
    }
}
