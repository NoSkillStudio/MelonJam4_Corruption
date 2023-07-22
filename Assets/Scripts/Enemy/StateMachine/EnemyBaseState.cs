using UnityEngine;

public abstract class EnemyBaseState : MonoBehaviour
{
    protected Animator animator;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
    }

    public abstract void EnterState(EnemyStateManager enemy);

    public abstract void UpdateState(EnemyStateManager enemy);

    public abstract void OnCollisionEnter(Collision collision);          
}