using System.Diagnostics;
using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
    private EnemyBaseState currentState;
    [SerializeField] private EnemyPatrolState patrolState;
    [SerializeField] private EnemyIdleState idleState;

   private void Start()
    {
        currentState = patrolState;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        currentState.OnCollisionEnter2D(collision);
    }

    private int GetRandomNumber(int maxNumber)
    {
        return Random.Range(0, maxNumber);
    }

}