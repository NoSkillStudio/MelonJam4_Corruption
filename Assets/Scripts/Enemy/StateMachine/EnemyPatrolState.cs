using UnityEngine;

public class EnemyPatrolState : EnemyBaseState
{
	[SerializeField] private Transform[] points;
	private int current;
	private int max;
	[SerializeField] private float speed;
	[SerializeField] private EnemyIdleState idleState;

	public override void EnterState(EnemyStateManager enemy)
	{
		current = 0;
		max = points.Length;
	}

	public override void OnCollisionEnter(Collision collision)
	{
		throw new System.NotImplementedException();
	}

	public override void UpdateState(EnemyStateManager enemy)
	{
		if (enemy.transform.position != points[current].position)
		{
			enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, points[current].position, speed * Time.deltaTime);
		}
		else
			current = (current + 1) % points.Length;

		if (enemy.transform.position == points[max - 1].position)
			enemy.SwitchState(idleState);
	}
}