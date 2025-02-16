using UnityEngine;
using UnityEngine.Assertions;
using KBCore.Refs;
using DG.Tweening;

public class Gun : ValidatedMonoBehaviour
{
    [Header("References")]
    [SerializeField, Child] private SpriteRenderer _spriteRenderer;
    [SerializeField, Child(Flag.Editable)] private Transform _muzzleTransform;
    [SerializeField, Child(Flag.Editable)] private ParticleSystem _fxFire;
    [SerializeField] private Bullet _bulletPrefab;

    [Header("Gun")]
    [SerializeField] private float _fireDelay = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _jamChance = 0.058f;
    [SerializeField] private int _maxAmmo = 10;
    [SerializeField] private float _reloadTime = 2f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private int _repairsNeeded = 3;
    [SerializeField] private float _cameraRecoil = .1f;
    [SerializeField] private bool _shouldSpinOnReload = true;
    
    [Header("Gun Type")]
    [SerializeField] private bool _isRevolver = false;
    [SerializeField] private bool _isBurstFire = false; // New: Toggle for burst mode

    [Header("Bullet")]
    [SerializeField] private float _bulletSpeed = 50f;
    [SerializeField] private float _bulletDeviation = .1f;
    [SerializeField] private int _bulletsPerShot = 1;
    [SerializeField] private float _bulletLifetime = 1f;
    [SerializeField] private LayerMask _hittableLayers;

    [Header("Burst Fire")]
    [SerializeField] private int _burstCount = 3;       // New: Shots per burst
    [SerializeField] private float _burstDelay = 0.05f; // New: Delay between burst shots

    [Header("SFX")]
    [SerializeField] private AudioSource _sfxFire;
    [SerializeField] private AudioSource _sfxSpin;
    [SerializeField] private AudioSource _sfxReload;
    [SerializeField] private AudioSource _sfxJam;
    [SerializeField] private bool _canPlayMultipleFireSFX;

    // Upgrades
    [HideInInspector] public float ReloadTimeUpgradeMultiplier = 1f;
    [HideInInspector] public float DamageUpgradeMultiplier = 1f;
    [HideInInspector] public float JamChanceUpgradeMultiplier = 1f;

    public int Ammo => _ammo;
    public int MaxAmmo => _maxAmmo;
    public int RepairsNeeded => _repairsNeeded;
    public bool IsReloading => _isReloading;
    public bool IsJammed => _isJammed;
    public SpriteRenderer SpriteRenderer => _spriteRenderer;

    private Player _player;
    private float _lastFireTime = -float.MaxValue;
    private float _lastUnJamTime;
    private bool _isJammed;
    private bool _isReloading;
    private int _ammo;

    private void Awake()
    {
        _ammo = _maxAmmo;
        _player = Player.Instance;

        if (!_sfxFire) Debug.LogWarning($"[{name}] Fire SFX not assigned");
        if (!_sfxReload) Debug.LogWarning($"[{name}] Reload SFX not assigned");
        if (!_sfxReload && _shouldSpinOnReload) Debug.LogWarning($"[{name}] Spin SFX not assigned");

        Assert.IsNotNull(_player, $"[{name}] Player instance not found in scene");
    }

    private void Update()
    {
        HandleSpin();
        HandleSpriteFlipping();
    }

    private void HandleSpin()
    {
        if (_isReloading && _shouldSpinOnReload)
        {
            var angleDeg = _spriteRenderer.transform.localEulerAngles.z;
            _spriteRenderer.transform.localEulerAngles = new Vector3(0, 0, angleDeg + 1600f * Time.deltaTime);
        }
        else
        {
            _spriteRenderer.transform.localEulerAngles = Vector3.zero;
        }
    }

    private void HandleSpriteFlipping()
    {
        if (_player.InputMove.x < 0f)
        {
            _spriteRenderer.flipY = true;

            var localPos = _muzzleTransform.localPosition;
            _muzzleTransform.localPosition = new Vector3(localPos.x, -Mathf.Abs(localPos.y), localPos.z);
        }
        else if (_player.InputMove.x > 0f)
        {
            _spriteRenderer.flipY = false;

            var localPos = _muzzleTransform.localPosition;
            _muzzleTransform.localPosition = new Vector3(localPos.x, Mathf.Abs(localPos.y), localPos.z);
        }
    }

    public void Fire()
    {
        if (_isBurstFire)
        {
            StartCoroutine(FireBurst());
        }
        else
        {
            FireSingleShot();
        }
    }

    private void FireSingleShot()
    {
        for (int i = 0; i < _bulletsPerShot; i++)
        {
            Bullet bullet = Instantiate(_bulletPrefab, _muzzleTransform.position, _muzzleTransform.rotation);
            bullet.Fire(this, _bulletSpeed, _bulletDeviation, _bulletLifetime, _hittableLayers);
        }

        PlayFireSFX();
        _fxFire.Play();
        if (SavedSettings.Instance.CameraShakeEnabled)
            Camera2D.Current.AddRecoilShake(_cameraRecoil, -transform.right * _cameraRecoil);

        _ammo--;
        _lastFireTime = Time.time;

        // Play the appropriate shooting sound
        if (_sfxFire)
            _sfxFire.Play();

        // After firing, there is a chance the gun jams.
        if (Random.value < _jamChance * JamChanceUpgradeMultiplier && TimeSince(_lastUnJamTime) > 4f)
            Jam();
    }


    // Add a helper function to determine if the gun is a revolver
    private bool IsRevolver()
    {
        return name.ToLower().Contains("revolver");
    }

    private System.Collections.IEnumerator FireBurst()
    {
        for (int i = 0; i < _burstCount && _ammo > 0; i++)
        {
            FireSingleShot();
            yield return new WaitForSeconds(_burstDelay);
        }
    }

    public bool CanFire()
    {
        return !_isJammed &&
            _ammo > 0 &&
            !_isReloading &&
            TimeSince(_lastFireTime) > _fireDelay;
    }

    public void StartReload()
    {
        _isReloading = true;

        if (_sfxSpin)
            _sfxSpin.Play();

        CancelInvoke(nameof(FinishReload));
        Invoke(nameof(FinishReload), _reloadTime * ReloadTimeUpgradeMultiplier);
    }

    public bool CanReload()
    {
        return !_isJammed &&
            _ammo < _maxAmmo &&
            !_isReloading;
    }

    private void PlayFireSFX()
    {
        if (_canPlayMultipleFireSFX)
        {
            var audioInstance = Instantiate(_sfxFire, _muzzleTransform.position, _muzzleTransform.rotation);
            audioInstance.Play();

            DOVirtual.DelayedCall(_sfxFire.clip.length, () => Destroy(audioInstance.gameObject))
                .SetLink(audioInstance.gameObject);
        }
        else
        {
            _sfxFire.Play();
        }
    }

    private void FinishReload()
    {
        _isReloading = false;
        _ammo = _maxAmmo;

        if (_sfxSpin)
            _sfxSpin.Stop();

        if (_sfxReload)
            _sfxReload.Play();
    }

    public void Jam()
    {
        _isJammed = true;
        if (_sfxJam)
            _sfxJam.Play();
    }

    public void UnJam()
    {
        _isJammed = false;
        _lastUnJamTime = Time.time;
    }

    private float TimeSince(float since)
        => Time.time - since;

    public void OnBulletHit(Bullet bullet, RaycastHit2D hit)
    {
        if (!hit.transform.TryGetComponent(out IDamageable damageable))
            return;

        damageable.TakeDamage(_damage * DamageUpgradeMultiplier);
    }
}
