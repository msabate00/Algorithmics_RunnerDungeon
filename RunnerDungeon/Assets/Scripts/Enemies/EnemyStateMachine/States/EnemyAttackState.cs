using UnityEngine;

public class EnemyAttackState : EnemyStateBase
{
    public EnemyAttackState(EnemyStateMachine enemy) : base(enemy) { }

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

        if (enemy.player == null)
        {
            enemy.PrepareReturnToNearestPatrolPoint();
            enemy.ChangeState(enemy.ReturnState);
            return;
        }

        enemy.StopMoving();
        enemy.FaceTarget(enemy.player.position.x);

        float distance = enemy.DistanceToPlayer();
        float verticalDifference = Mathf.Abs(enemy.VerticalDifferenceToPlayer());

        if (distance > enemy.attackDistance + 0.15f || verticalDifference > 1f)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        if (enemy.AttackTimer <= 0f)
        {
            enemy.AttackTimer = enemy.attackCooldown;
            enemy.DoAttack();
        }
    }
}
