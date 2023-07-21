using UnityEngine;

public class EnemyHealth : UnitHealth
{
    private PlayerHealth playerHealth;
    public override void ApplyDamage(int damage)
	{
		base.ApplyDamage(damage);
	}


    protected override void Start()
    {
        base.Start();
        playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        playerHealth.OnDie += Die;
    }

    private void OnDisable()
    {
        playerHealth.OnDie -= Die;
    }

    public override void Die()
	{        
        transform.rotation = Quaternion.identity;
        base.Die();
    }
}