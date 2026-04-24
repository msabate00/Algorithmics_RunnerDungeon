using UnityEngine;

public class EnemyDeadState : EnemyStateBase
{
    public EnemyDeadState(EnemyStateMachine enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SuspendAutoMovement = true;
        enemy.StopAllMotion();
    }
}
