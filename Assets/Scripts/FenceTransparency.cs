using UnityEngine;
using KBCore.Refs;
using DG.Tweening;

public class FenceTransparency : ValidatedMonoBehaviour
{
    [SerializeField, Child] private SpriteRenderer _spriteRenderer;

    [SerializeField] private float transparentAlpha = 0.3f; // Transparency level when an object is behind
    [SerializeField] private GameObject[] targetObjects;    // Array of GameObjects to check (e.g., Player, Anvil)

    private Tween _fadeTween;
    private int _triggeredObjectsCount; // Track how many target objects are in the trigger
    private float _defaultAlpha;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _defaultAlpha = _spriteRenderer.color.a;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        foreach (GameObject target in targetObjects)
        {
            if (other.gameObject == target)
            {
                _triggeredObjectsCount++;
                SetAlpha(transparentAlpha);
                break;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        foreach (GameObject target in targetObjects)
        {
            if (other.gameObject == target)
            {
                _triggeredObjectsCount = Mathf.Max(0, _triggeredObjectsCount - 1); // Prevent negative count
                if (_triggeredObjectsCount == 0)
                {
                    SetAlpha(_defaultAlpha);
                }
                break;
            }
        }
    }

    private void SetAlpha(float alpha)
    {
        _fadeTween?.Kill();
        _fadeTween = _spriteRenderer.DOFade(alpha, 0.2f).SetLink(_spriteRenderer.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the trigger area for debugging
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size);
        }
    }
}
