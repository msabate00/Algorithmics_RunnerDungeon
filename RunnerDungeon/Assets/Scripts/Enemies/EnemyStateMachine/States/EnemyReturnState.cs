using UnityEngine;

public class EnemyReturnState : EnemyStateBase
{
    public EnemyReturnState(EnemyStateMachine enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SuspendAutoMovement = false;
        enemy.PrepareReturnToNearestPatrolPoint();
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
            enemy.ChangeState(enemy.PatrolState);
            return;
        }

        Transform target = enemy.GetReturnPoint();
        if (target == null)
        {
            enemy.ChangeState(enemy.PatrolState);
            return;
        }

        float difference = target.position.x - enemy.transform.position.x;

        if (Mathf.Abs(difference) <= enemy.pointReachedDistance)
        {
            enemy.PatrolIndex = enemy.ReturnIndex;
            enemy.ChangeState(enemy.PatrolState);
            return;
        }

        enemy.SetHorizontalMovement(Mathf.Sign(difference), enemy.returnSpeed);

        if (enemy.WallAhead() && enemy.IsGrounded() && enemy.CanJumpNow())
            enemy.Jump();

        if (!enemy.GroundAhead() && enemy.IsGrounded())
            enemy.StopMoving();
    }
}
