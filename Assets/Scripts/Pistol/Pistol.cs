using UnityEngine;

public class Pistol : MonoBehaviour
{
    [SerializeField] private KeyCode _shootKey;
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private float _colldown;
    private float _time = 0f;
    private Transform _bulletSpawnPoint;
    private ParticleSystem _particle;


    private void Start()
    {
        GameObject bulletSpawnPoint = GameObject.FindWithTag("BulletSpawnPoint");
        _particle = bulletSpawnPoint.GetComponent<ParticleSystem>();
        _bulletSpawnPoint = bulletSpawnPoint.transform;
    }

    private void Update()
    {
        _time += Time.deltaTime;
        if (Input.GetKeyDown(_shootKey) && _time >= _colldown)
        {
            _time = 0f;
            _particle.Play();
            Bullet bullet = Instantiate(
                _bulletPrefab.gameObject,
                _bulletSpawnPoint.position,
                Quaternion.identity
            ).GetComponent<Bullet>();
            Vector3 direction = _bulletSpawnPoint.forward;
            bullet.Fly(direction);
        }
    }
}
