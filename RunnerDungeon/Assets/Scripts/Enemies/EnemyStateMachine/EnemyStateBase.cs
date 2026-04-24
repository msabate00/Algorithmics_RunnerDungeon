using UnityEngine;

public abstract class EnemyStateBase
{
    protected readonly EnemyStateMachine enemy;

    protected EnemyStateBase(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void LogicUpdate() { }
    public virtual void PhysicsUpdate() { }
}
