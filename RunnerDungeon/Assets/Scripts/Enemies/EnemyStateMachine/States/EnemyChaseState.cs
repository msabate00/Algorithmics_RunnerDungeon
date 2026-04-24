using UnityEngine;

public class EnemyChaseState : EnemyStateBase
{
    public EnemyChaseState(EnemyStateMachine enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SuspendAutoMovement = false;
    }

    public override void LogicUpdate()
    {
        if (enemy.IsDead)
        {
            enemy.ChangeState(enemy.DeadState);
            return;
        }

        if (enemy.player == null)
        {
            enemy.PrepareReturnToNearestPatrolPoint();
            enemy.ChangeState(enemy.ReturnState);
            return;
        }

        float distance = enemy.DistanceToPlayer();
        float verticalDifference = enemy.VerticalDifferenceToPlayer();
        float horizontalDifference = enemy.player.position.x - enemy.transform.position.x;

        if (distance > enemy.loseDistance)
        {
            enemy.PrepareReturnToNearestPatrolPoint();
            enemy.ChangeState(enemy.ReturnState);
            return;
        }

        if (distance <= enemy.attackDistance && Mathf.Abs(verticalDifference) < 1f)
        {
            enemy.ChangeState(enemy.AttackState);
            return;
        }

        enemy.SetHorizontalMovement(Mathf.Sign(horizontalDifference), enemy.chaseSpeed);

        if (enemy.WallAhead() && enemy.IsGrounded() && enemy.CanJumpNow())
        {
            enemy.Jump();
            return;
        }

        bool playerIsHigher = verticalDifference > enemy.playerHigherJumpThreshold;
        bool playerIsCloseHorizontally = enemy.HorizontalDistanceToPlayer() < 2f;

        if (playerIsHigher && playerIsCloseHorizontally && enemy.CanJumpNow())
        {
            enemy.Jump();
            return;
        }

        if (!enemy.allowDropFromPlatforms && !enemy.GroundAhead() && enemy.IsGrounded() && verticalDifference < -0.25f)
        {
            enemy.StopMoving();
        }
    }
}
