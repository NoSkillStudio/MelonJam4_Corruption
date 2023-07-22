using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;
    private Vector3 _direction;
    private Rigidbody _rb;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        _rb.velocity = _direction * speed;
    }

    public void Fly(Vector3 direction)
    {
        _direction = direction;
    }
}
