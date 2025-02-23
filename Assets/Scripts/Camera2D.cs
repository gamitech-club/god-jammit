using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Camera2D : MonoBehaviour
{
    #region Singleton
    private static Camera2D _current;
    public static Camera2D Current {
        get {
            if (_current == null)
                _current = FindFirstObjectByType<Camera2D>();
            return _current;
        }
    }
    #endregion

    [SerializeField] private Camera _mainCamera;

    [Header("General")]
    [SerializeField] private bool _useGeneralSmooth;
    [SerializeField] private Vector3 _generalOffset = new(0f, 0f, -10f);
    [SerializeField] private float _generalSmoothSpeed = 5f;

    [Header("Target Tracking")]
    [SerializeField] private bool _enableTracking = true;
    [SerializeField] private bool _useSmoothTracking = true;
    [SerializeField] private bool _drawTracking = true;
    [SerializeField] private Transform _trackedTransform;
    [SerializeField] private float _trackSmoothSpeed = 5f;
    [SerializeField] private Vector3 _trackingOffset;

    [Header("Bounds")]
    [SerializeField] private bool _enableBounds;
    [SerializeField] private bool _useSmoothBounds = true;
    [SerializeField] private bool _drawRestrictedBounds = true;
    [ContextMenuItem("Match Camera Bounds", nameof(MatchRestrictedBoundsToCameraBounds))]
    [SerializeField] private Bounds _restrictedBounds;
    [SerializeField] private float _boundsSmoothSpeed = 5f;

    [Header("Shaking")]
    [SerializeField] private bool _enableShaking = true;
    [SerializeField, Range(0f, 1f)] private float _shakeMultiplier = 1f;
    [SerializeField] private AnimationCurve _shakeProgressCurve = AnimationCurve.Constant(0f, 1f, 1f);

    public Vector3 GlobalOffset { get => _generalOffset; set => _generalOffset = value; }
    public Transform TrackedTransform { get => _trackedTransform; set => _trackedTransform = value; }
    public Vector3 TrackingOffset { get => _trackingOffset; set => _trackingOffset = value; }
    public Bounds RestrictedBounds { get => _restrictedBounds; set => _restrictedBounds = value; }
    public bool EnableBounds { get => _enableBounds; set => _enableBounds = value; }
    public float DefaultOrthoSize { get; private set; }

    private Tween _zoomTween;
    private List<ShakeInstance> _activeShakes = new();
    private float _shakeSeed;

    private Vector3 _trackingMotion;
    private Vector3 _boundsMotion;
    private Vector3 _shakeMotion;

    private void Reset()
    {
        _mainCamera = Camera.main;

        if (_mainCamera)
        {
            _generalOffset = new(0f, 0f, _mainCamera.transform.position.z);
            _restrictedBounds.extents = GetCameraExtents();
        }
    }

    private void Awake()
    {
        if (_current != null && _current != this)
        {
            Debug.LogWarning($"Multiple instances of {nameof(Camera2D)} found" +
                " in scene. Destroying the new one ({this}).");

            Destroy(this);
            return;
        }
        else
        {
            _current = this;
        }

        DefaultOrthoSize = _mainCamera.orthographicSize;
        _shakeSeed = Random.value;
    }

    private void LateUpdate()
    {
        HandleTrackingMotion();
        HandleBoundsMotion();
        HandleCameraShake();
        ApplyMotion(_useGeneralSmooth);
    }

    private void OnDrawGizmos()
    {
        if (_enableTracking && _drawTracking)
        {
            Gizmos.color = Color.white;
            if (_trackedTransform)
                Gizmos.DrawLine(transform.position, _trackedTransform.position);
        }

        if (_enableBounds && _drawRestrictedBounds)
        {
            Gizmos.color = IsExtentsSmaller(_restrictedBounds.extents, GetCameraExtents())
                ? Color.red
                : new Color(1f, .5f, .5f);

            Gizmos.DrawWireCube(_restrictedBounds.center, _restrictedBounds.size);
        }
    }

    private void ApplyMotion(bool smooth = false)
    {
        var applied = _generalOffset + _trackingMotion + _boundsMotion;
        var finalPos = smooth
            ? Vector3.Lerp(transform.position, applied, _generalSmoothSpeed * Time.deltaTime) + _shakeMotion
            : applied + _shakeMotion;

        transform.position = finalPos;
    }

    private void HandleTrackingMotion()
    {
        if (!_enableTracking || !_trackedTransform)
            return;

        var trackPos = _trackedTransform.position + _trackingOffset;
        if (_useSmoothTracking)
            _trackingMotion = Vector3.Lerp(_trackingMotion, trackPos, _trackSmoothSpeed * Time.deltaTime);
        else
            _trackingMotion = trackPos;
    }

    private void HandleBoundsMotion()
    {
        if (!_enableBounds)
        {
            _boundsMotion = Vector3.zero;
            return;
        }

        var camExtents = GetCameraExtents();
        var restrictedExtents = _restrictedBounds.extents;

        // If restricted extents are smaller than camera extents, adjust restricted extents
        if (IsExtentsSmaller(restrictedExtents, camExtents))
        {
            Debug.LogWarning($"[{name}] Restricted bounds extents ({restrictedExtents}) is " +
                $"smaller than camera extents ({camExtents}).", this);

            restrictedExtents = new(
                Mathf.Max(camExtents.x, restrictedExtents.x),
                Mathf.Max(camExtents.y, restrictedExtents.y),
                restrictedExtents.z
            );
        }

        // Takes global offset and tracking motion into account
        var currentCamPos = _generalOffset + _trackingMotion;

        var camMin = currentCamPos - camExtents;
        var camMax = currentCamPos + camExtents;
        var restrictedMin = _restrictedBounds.center - restrictedExtents;
        var restrictedMax = _restrictedBounds.center + restrictedExtents;
        
        if (!_useSmoothBounds)
        {
            if (camMin.x < restrictedMin.x)
                _boundsMotion.x = restrictedMin.x - camMin.x;
            else if (camMax.x > restrictedMax.x)
                _boundsMotion.x = restrictedMax.x - camMax.x;
            else
                _boundsMotion.x = 0;
            
            if (camMin.y < restrictedMin.y)
                _boundsMotion.y = restrictedMin.y - camMin.y;
            else if (camMax.y > restrictedMax.y)
                _boundsMotion.y = restrictedMax.y - camMax.y;
            else
                _boundsMotion.y = 0;
        }
        else
        {
            var speed = Time.deltaTime * _boundsSmoothSpeed;
            if (camMin.x < restrictedMin.x)
                _boundsMotion.x = Mathf.Lerp(_boundsMotion.x, restrictedMin.x - camMin.x, speed);
            else if (camMax.x > restrictedMax.x)
                _boundsMotion.x = Mathf.Lerp(_boundsMotion.x, restrictedMax.x - camMax.x, speed);
            else
                _boundsMotion.x = Mathf.Lerp(_boundsMotion.x, 0, speed);
            
            if (camMin.y < restrictedMin.y)
                _boundsMotion.y = Mathf.Lerp(_boundsMotion.y, restrictedMin.y - camMin.y, speed);
            else if (camMax.y > restrictedMax.y)
                _boundsMotion.y = Mathf.Lerp(_boundsMotion.y, restrictedMax.y - camMax.y, speed);
            else
                _boundsMotion.y = Mathf.Lerp(_boundsMotion.y, 0, speed);
        }
        
    }

    private void HandleCameraShake()
    {
        if (!_enableShaking)
        {
            _shakeMotion = Vector3.zero;
            return;
        }

        float time = Time.time + _shakeSeed;
        _shakeMotion = Vector3.zero;

        // Reverse for-loop
        for (int i = _activeShakes.Count - 1; i >= 0 ; i--)
        {
            var shake = _activeShakes[i];
            if (shake.IsFinished())
            {
                _activeShakes.RemoveAt(i);
                continue;
            }

            float multiplier = _shakeMultiplier;
            if (!shake.IsPersistent)
                multiplier *= _shakeProgressCurve.Evaluate(shake.GetProgress());

            Vector3 motion;
            if (shake.IsRecoilShake)
            {
                motion = multiplier * shake.Amplitude * shake.Recoil;
            }
            else
            {
                motion = new Vector3(
                    (Mathf.PerlinNoise(time * shake.Frequency, 0) * 2 - 1) * shake.Amplitude * multiplier,
                    (Mathf.PerlinNoise(time * shake.Frequency, 1) * 2 - 1) * shake.Amplitude * multiplier
                );
            }

            _shakeMotion += motion;
        }
    }

    /// <summary>
    /// Returns the camera's visible extents, resulting in half of height and width.
    /// </summary>
    public Vector3 GetCameraExtents()
    {
        float height = _mainCamera.orthographicSize;
        float width = height * _mainCamera.aspect;
        return new(width, height, 0);
    }

    public void Zoom(float orthoSize, float duration = .3f)
    {
        _zoomTween?.Kill();
        _zoomTween = _mainCamera.DOOrthoSize(orthoSize, duration).SetLink(gameObject);
    }

    public ShakeInstance AddShake(float duration, float frequency, float amplitude = 1f)
    {
        var instance = new ShakeInstance(Mathf.Max(.001f, duration), frequency, amplitude);
        _activeShakes.Add(instance);
        return instance;
    }

    public ShakeInstance AddPersistentShake(float frequency, float amplitude = 1f)
    {
        var instance = new ShakeInstance(-1f, frequency, amplitude);
        _activeShakes.Add(instance);
        return instance;
    }

    public ShakeInstance AddRecoilShake(float duration, Vector2 dir)
    {
        var instance = ShakeInstance.NewRecoilShake(duration, dir);
        _activeShakes.Add(instance);
        return instance;
    }

    public void RemoveShake(ShakeInstance instance)
        => _activeShakes.Remove(instance);
    
    public void ClearShake()
        => _activeShakes.Clear();

    private bool IsExtentsSmaller(Vector2 lhs, Vector2 rhs)
        => lhs.x < rhs.x || lhs.y < rhs.y;

    /// <summary>
    /// Teleports the camera to the supposed position immediately.
    /// </summary>
    public void FinishMotionsImmediately()
    {
        // Just in case the motion is not updated, update it now.
        HandleTrackingMotion();
        HandleBoundsMotion();

        // Apply without smooth
        ApplyMotion(false);
    }

    public Vector2 ScreenToWorldPoint(Vector2 screenPos)
        => _mainCamera.ScreenToWorldPoint(new(screenPos.x, screenPos.y, -_mainCamera.transform.position.z));

    // Editor context menu helper
    private void MatchRestrictedBoundsToCameraBounds()
    {
        if (!_mainCamera)
        {
            Debug.LogWarning("Main camera not assigned.");
            return;
        }

        #if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Match Restricted Bounds to Camera Bounds");
        #endif

        _restrictedBounds.center = new(transform.position.x, transform.position.y, 0);
        _restrictedBounds.extents = GetCameraExtents();
    }

    public class ShakeInstance
    {
        public float Frequency = 1f;
        public float Amplitude = 1f;
        public readonly bool IsPersistent;

        public Vector2 Recoil { get; private set; }
        public bool IsRecoilShake { get; private set; }

        private readonly float _duration;
        private readonly float _endTIme;

        public ShakeInstance(float duration, float frequency, float amplitude)
        {
            Frequency = frequency;
            Amplitude = amplitude;
            IsPersistent = duration < 0;
            _duration = duration;
            _endTIme = Time.time + duration;
        }

        public static ShakeInstance NewRecoilShake(float duration, Vector2 dir)
        {
            var instance = new ShakeInstance(duration, 1f, 1f)
            {
                IsRecoilShake = true,
                Recoil = dir
            };

            return instance;
        }

        /// <summary>
        /// Get progress of the shake (0.0 - 1.0).
        /// </summary>
        public float GetProgress()
            => 1f - (_endTIme - Time.time) / _duration;

        public bool IsFinished()
            => Time.time > _endTIme && !IsPersistent;
    }
}
