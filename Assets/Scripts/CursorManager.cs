using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D aimCursorTexture;
    [SerializeField] private Texture2D reloadCursorTexture;
    [SerializeField] private Texture2D jammedCursorTexture;

    private Player _player;
    private Vector2 _cursorHotspot;
    
    private void Awake()
    {
        _player = Player.Instance;
        Assert.IsNotNull(_player, $"[{name}] Player instance not found in scene");
    }

    void Start()
    {
        if (_player)
        {
            _cursorHotspot = new Vector2(aimCursorTexture.width / 2, aimCursorTexture.height / 2);
            Cursor.SetCursor(aimCursorTexture, _cursorHotspot, CursorMode.Auto);
        }
    }

    void Update()
    {
        HandleUpdateCursor();
    }

    private void HandleUpdateCursor()
    {
        var gun = _player.ActiveGun;

        if (!HUD.Instance.ShouldEnableCursorLabel)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        if (gun.IsReloading)
        {
            _cursorHotspot = new Vector2(reloadCursorTexture.width / 2, reloadCursorTexture.height / 2);
            Cursor.SetCursor(reloadCursorTexture, _cursorHotspot, CursorMode.Auto);
        }
        else if (gun.IsJammed)
        {
            _cursorHotspot = new Vector2(jammedCursorTexture.width / 2, jammedCursorTexture.height / 2);
            Cursor.SetCursor(jammedCursorTexture, _cursorHotspot, CursorMode.Auto);
        }
        else
        {
            _cursorHotspot = new Vector2(aimCursorTexture.width / 2, aimCursorTexture.height / 2);
            Cursor.SetCursor(aimCursorTexture, _cursorHotspot, CursorMode.Auto);
        }
    }
}
