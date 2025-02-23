using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using KBCore.Refs;

public class GunSelectionMenu : MenuPage
{
    #region Singleton
    private static GunSelectionMenu _instance;
    public static GunSelectionMenu Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<GunSelectionMenu>();
            return _instance;
        }
    }
    #endregion

    [Header("Gun Selection Menu")]
    [SerializeField, Self] private AudioSource _fxSelect;
    [SerializeField] private VisualTreeAsset _gunCardTemplate;

    [Header("Starter Guns")]
    [SerializeField] private Gun[] _starterGunPrefabs;
    [SerializeField, Multiline(4)] private string[] _starterGunDescriptions;

    private VisualElement _cardsContainer;

    protected override void Awake()
    {
        base.Awake();

        Assert.IsTrue(_starterGunPrefabs.Length > 0, $"[{name}] No starter gun prefabs assigned");
        Assert.IsTrue(_starterGunDescriptions.Length > 0, $"[{name}] No description assigned");
        Assert.IsTrue(_starterGunPrefabs.Length == _starterGunDescriptions.Length, $"[{name}] Number of starter gun prefabs and descriptions do not match");
        Assert.IsNotNull(_gunCardTemplate, $"[{name}] Gun Card Template not assigned");
        Assert.IsNotNull(Container.Q("CardsContainer"), $"[{name}] Element 'CardsContainer' not found");

        // If an instance already exists, destroy the new one
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(GunSelectionMenu)} found. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _cardsContainer = Container.Q("CardsContainer");
    }

    protected override void Start()
    {
        base.Start();
        MakeCards();
        Invoke(nameof(FreezeTime), 1f);
    }

    private void FreezeTime()
    {
        Time.timeScale = 0f;
    }

    private void MakeCards()
    {
        _cardsContainer.Clear();

        for (int i = 0; i < _starterGunPrefabs.Length; i++)
        {
            var card = _gunCardTemplate.Instantiate();
            var titleLabel = card.Q<Label>("CardLabel");
            var imgElement = card.Q("ImageElement");
            var descLabel = card.Q<Label>("DescriptionLabel");

            var gunPrefab = _starterGunPrefabs[i];
            titleLabel.text = gunPrefab.name;
            descLabel.text = _starterGunDescriptions[i];
            imgElement.style.backgroundImage = new(gunPrefab.SpriteRenderer.sprite);

            card.Q<Button>().clicked += () => OnGunCardClicked(gunPrefab);
            _cardsContainer.Add(card);
        }

        void OnGunCardClicked(Gun gunPrefab)
        {
            CancelInvoke(nameof(FreezeTime));
            Time.timeScale = 1f;
            _fxSelect.Play();

            var gun = Instantiate(gunPrefab);
            Player.Instance.EquipGun(gun);
            Hide();
        }
    }
}
