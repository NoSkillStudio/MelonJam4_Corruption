using UnityEngine;
using UnityEngine.AI;

public class EnemyAgroState : EnemyBaseState
{
	private NavMeshAgent navMeshAgent;
	private PlayerHealth player;

	[SerializeField] int damage;

	protected override void Start()
	{
		base.Start();
		navMeshAgent = GetComponent<NavMeshAgent>();
	}
	public override void EnterState(EnemyStateManager enemy)
	{
		player = FindObjectOfType<PlayerHealth>();
		navMeshAgent.SetDestination(player.transform.position);
	}
	public override void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
		{
			playerHealth.ApplyDamage(damage);
		}
	}

	public override void UpdateState(EnemyStateManager enemy)
	{
		
	}

}