using System.Diagnostics;
using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
    private EnemyBaseState currentState;
    [SerializeField] private EnemyPatrolState patrolState;
    [SerializeField] private EnemyIdleState idleState;
    [SerializeField] private EnemyAgroState agroState;

   private void Start()
    {
        currentState = agroState;
        currentState.EnterState(this);
    }

    private void Update()
    {
        currentState.UpdateState(this);
    }

    public void SwitchState(EnemyBaseState state)
    {
        currentState = state;
        state.EnterState(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentState.OnCollisionEnter(collision);
    }

    private int GetRandomNumber(int maxNumber)
    {
        return Random.Range(0, maxNumber);
    }

}