using System.Threading.Tasks;
using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    [SerializeField] private EnemyPatrolState patrolState;
    public override void EnterState(EnemyStateManager enemy)
    {
        SwitchToRandomState(enemy);
    }

    public override void OnCollisionEnter2D(Collision2D enemy)
    {

    }

    public override void UpdateState(EnemyStateManager enemy)
    {

    }

    private async void SwitchToRandomState(EnemyStateManager enemy)
    {
        await Task.Delay(Random.Range(100, 1000));
        enemy.SwitchState(patrolState);
        Debug.Log("enemy.SwitchState(patrolState);");
    }

}