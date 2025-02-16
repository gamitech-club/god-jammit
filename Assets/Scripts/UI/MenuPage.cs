using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using KBCore.Refs;
using DG.Tweening;

public class MenuPage : ValidatedMonoBehaviour
{
    public enum AnimationStyle
    {
        Instant,
        PushLeft,
        PushRight,
        PushUp,
        PushDown,
        ScaleDown
    }

    [Serializable]
    public struct ButtonMenuPair
    {
        public string ButtonName;
        public MenuPage MenuPage;

        public ButtonMenuPair(string buttonName, MenuPage menuPage)
        {
            ButtonName = buttonName;
            MenuPage = menuPage;
        }
    }

    [Header("Menu Page")]
    [SerializeField, Self(Flag.EditableAnywhere)] private UIDocument _uiDocument;
    [SerializeField] private string _containerName = "Container";
    [SerializeField] private AnimationStyle _animation;
    [SerializeField] private float _transitionDuration = 0.3f;
    [SerializeField] private bool _dissolveAnimation;
    [SerializeField] private bool _shouldStartHidden;
    [SerializeField] private ButtonMenuPair[] _buttonMenuPairs;

    public bool IsHidden => _isHidden;
    protected bool ShouldStartHidden => _shouldStartHidden;
    public VisualElement Container => _container;
    public AnimationStyle Animation { get => _animation; set => _animation = value; }
    public ButtonMenuPair[] ButtonMenuPairs {  get => _buttonMenuPairs; set => _buttonMenuPairs = value; }

    public event Action Showed;
    public event Action Hid;
    
    private bool _isHidden;
    private VisualElement _container;
    private VisualElement _lastFocusedElement;
    private Tween _delayedDisableTween;

    protected virtual void Awake()
    {
        _uiDocument.enabled = true;
        _container = _uiDocument.rootVisualElement.Q(_containerName);
        Assert.IsNotNull(_container, $"[{name}] Container element named '{_containerName}' not found");
    }

    protected virtual void Start()
    {
        RegisterButtonMenuPairs();
        if (_shouldStartHidden)
            Hide();
    }

    private void RegisterButtonMenuPairs()
    {
        foreach (var pair in _buttonMenuPairs)
        {
            Button button = _container.Q<Button>(pair.ButtonName);
            if (button != null)
                button.clicked += () => {
                    _lastFocusedElement = button;
                    OpenMenu(pair.MenuPage);
                };
            else
                Debug.LogWarning($"[{name}] Button element named '{pair.ButtonName}' not found", this);
        }
    }

    protected void OpenMenu(MenuPage menu, bool hideSelf = true)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        foreach (var pair in _buttonMenuPairs)
        {
            if (pair.MenuPage != menu)
                pair.MenuPage.Hide();
        }

        if (hideSelf)
            Hide();

        menu.Show();
        menu.TryFocus();
    }

    public void Hide()
    {
        _delayedDisableTween?.Kill();
        _delayedDisableTween = DOVirtual.DelayedCall(_transitionDuration, () => _container.enabledSelf = false)
            .SetLink(_uiDocument.gameObject);

        switch (_animation)
        {
            case AnimationStyle.Instant:
                _container.visible = false;
                break;
            case AnimationStyle.PushLeft:
                _container.style.translate = new(new Translate(new Length(-100f, LengthUnit.Percent), 0));
                break;
            case AnimationStyle.PushRight:
                _container.style.translate = new(new Translate(new Length(100f, LengthUnit.Percent), 0));
                break;
            case AnimationStyle.PushUp:
                _container.style.translate = new(new Translate(0, new Length(-100f, LengthUnit.Percent)));
                break;
            case AnimationStyle.PushDown:
                _container.style.translate = new(new Translate(0, new Length(100f, LengthUnit.Percent)));
                break;
            case AnimationStyle.ScaleDown:
                _container.style.scale = new(Vector2.zero);
                break;
        }

        if (_dissolveAnimation)
            _container.style.opacity = 0f;

        _isHidden = true;
        Hid?.Invoke();
    }

    public void Show()
    {
        _delayedDisableTween?.Kill();
        _container.enabledSelf = true;

        switch (_animation)
        {
            case AnimationStyle.Instant:
                _container.visible = true;
                break;
            case AnimationStyle.PushLeft:
            case AnimationStyle.PushRight:
            case AnimationStyle.PushUp:
            case AnimationStyle.PushDown:
                _container.style.translate = StyleKeyword.Initial;
                break;
            case AnimationStyle.ScaleDown:
                _container.style.scale = StyleKeyword.Initial;
                break;
        }

        if (_dissolveAnimation)
            _container.style.opacity = 1f;

        _isHidden = false;
        Showed?.Invoke();
    }

    public void TryFocus()
    {
        if (_lastFocusedElement != null)
            _lastFocusedElement.Focus();
        else
            _lastFocusedElement = _container.Query().Where(x => x.focusable).First();
        
        if (_lastFocusedElement == null)
            Debug.Log($"[{name}] No focusable elements found");
    }

    public void HideInstantly()
    {
        var defaultDuration = _transitionDuration;
        _transitionDuration = 0f;
        Hide();
        _transitionDuration = defaultDuration;
    }
}
