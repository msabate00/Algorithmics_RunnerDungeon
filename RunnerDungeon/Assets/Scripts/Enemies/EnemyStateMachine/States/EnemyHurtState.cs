using UnityEngine;

public class EnemyHurtState : EnemyStateBase
{
    public EnemyHurtState(EnemyStateMachine enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SuspendAutoMovement = true;
        enemy.StopMoving();
        enemy.StateTimer = enemy.hurtTime;
        enemy.ApplyHurtKnockback();
    }

    public override void LogicUpdate()
    {
        if (enemy.IsDead)
        {
            enemy.ChangeState(enemy.DeadState);
            return;
        }

        enemy.StateTimer -= Time.deltaTime;

        if (enemy.StateTimer > 0f)
            return;

        if (enemy.CanSeePlayer())
        {
            enemy.ChangeState(enemy.ChaseState);
        }
        else
        {
            enemy.PrepareReturnToNearestPatrolPoint();
            enemy.ChangeState(enemy.ReturnState);
        }
    }

    public override void Exit()
    {
        enemy.SuspendAutoMovement = false;
    }
}
