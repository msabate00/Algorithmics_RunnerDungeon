using UnityEngine;

public class EnemyPatrolState : EnemyStateBase
{
    public EnemyPatrolState(EnemyStateMachine enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SuspendAutoMovement = false;
        enemy.StopMoving();
    }

    public override void LogicUpdate()
    {
        if (enemy.IsDead)
        {
            enemy.ChangeState(enemy.DeadState);
            return;
        }

        if (enemy.CanSeePlayer())
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        if (!enemy.HasPatrolPoints)
        {
            enemy.StopMoving();
            return;
        }

        Transform target = enemy.GetCurrentPatrolPoint();
        if (target == null)
        {
            enemy.StopMoving();
            return;
        }

        float difference = target.position.x - enemy.transform.position.x;

        if (Mathf.Abs(difference) <= enemy.pointReachedDistance)
        {
            enemy.AdvancePatrolIndex();
            enemy.StopMoving();
            return;
        }

        enemy.SetHorizontalMovement(Mathf.Sign(difference), enemy.patrolSpeed);

        if (enemy.WallAhead() && enemy.IsGrounded())
        {
            if (enemy.CanJumpNow())
                enemy.Jump();
            else
            {
                enemy.AdvancePatrolIndex();
                enemy.StopMoving();
            }
        }

        if (!enemy.GroundAhead() && enemy.IsGrounded())
        {
            enemy.AdvancePatrolIndex();
            enemy.StopMoving();
        }
    }
}
