using UnityEngine;
using KBCore.Refs;

public class Bullet : ValidatedMonoBehaviour
{
    [SerializeField, Child] private TrailRenderer _trail;
    [SerializeField, Child(Flag.Editable)] private ParticleSystem _fxImpact;
    
    private Gun _gun;
    private LayerMask _hittableLayers;
    private float _speed;
    private float _lifetime;
    private bool _isFired;

    private float _firedTime;

    private void Update()
    {
        HandleLifetime();
    }

    private void FixedUpdate()
    {
        HandleMovementAndCollision();
    }

    private void HandleLifetime()
    {
        if (_isFired && TimeSince(_firedTime) > _lifetime)
            Destroy(gameObject);
    }

    private void HandleMovementAndCollision()
    {
        var nextPosition = transform.position +  _speed * Time.fixedDeltaTime * transform.right;
        var hit = Physics2D.Linecast(transform.position, nextPosition, _hittableLayers);

        if (hit)
        {
            OnHit(hit);
            return;
        }

        transform.position = nextPosition;
    }

    public void Fire(Gun gun,float speed, float deviation, float lifetime, LayerMask hittableLayers)
    {
        _gun = gun;
        _speed = speed;
        _lifetime = lifetime;
        _firedTime = Time.time;
        _hittableLayers = hittableLayers;
        _isFired = true;

        transform.Rotate(0, 0, Random.Range(-deviation, deviation));
    }

    private float TimeSince(float since)
        => Time.time - since;
    
    private void OnHit(RaycastHit2D hit)
    {
        _fxImpact.transform.SetParent(null);
        _fxImpact.transform.position = hit.point;
        _fxImpact.Play();

        if (_gun)
            _gun.OnBulletHit(this, hit);

        Destroy(gameObject);
    }
}
