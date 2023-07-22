using UnityEngine;

public class Pistol : MonoBehaviour
{
    [SerializeField] private KeyCode _shootKey;
    [SerializeField] private float _colldown;
    [SerializeField] private float _maxDistance;
    [SerializeField] private Camera _camera;
    private float _time;
    private Transform _bulletSpawnPoint;
    private ParticleSystem _particle;
    private AudioSource _audio;

    private void Start()
    {

        GameObject bulletSpawnPoint = GameObject.FindWithTag("BulletSpawnPoint");
        _audio = GetComponent<AudioSource>();
        _particle = bulletSpawnPoint.GetComponent<ParticleSystem>();
        _bulletSpawnPoint = bulletSpawnPoint.transform;
        _time = _colldown;
    }

    private void Update()
    {
        _time -= Time.deltaTime;
        if (Input.GetKeyDown(_shootKey) && _time <= 0)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector3 origin = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));
        _time = 0f;
        _particle.Play();
        _audio.Play();
        if (Physics.Raycast(origin, _camera.transform.forward, out RaycastHit hit, _maxDistance))
        {
            HandleHit(hit);
        }
    }

    private void HandleHit(RaycastHit hit)
    {
        print("Hit");
    }
}
