using UnityEngine;
using System.Collections;
using Resources.Scripts.Enemy.Enum;

namespace Resources.Scripts.Enemy.Controllers
{
    public class EnemyMeleeController : EnemyController
    {
        [Header("Melee Attack Settings")]
        public float attackRange = 1f;
        public float attackCooldown = 1f;
        public bool pushPlayer = true;
        public float pushForceMultiplier = 1f;

        protected override void OnAdjustAttackCooldown(float animationDuration)
        {
            attackCooldown = Mathf.Max(attackCooldown, animationDuration);
        }

        protected override bool CanAttack(float distanceToPlayer)
        {
            return distanceToPlayer <= attackRange;
        }

        protected override void AttemptAttack()
        {
            if (isAttacking) return;

            float since = Time.time - lastAttackTime;
            if (since >= attackCooldown)
                StartCoroutine(PerformMeleeAttack());
        }

        private IEnumerator PerformMeleeAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            PlayAnimation(EnemyAnimationName.Attack, false);

            float hitTime = attackCooldown * 0.4f;
            yield return new WaitForSeconds(hitTime);

            player.TakeDamage(this, stats);

            if (pushPlayer)
                player.ApplyPush(
                    (player.transform.position - transform.position).normalized
                    * pushForceMultiplier
                );

            float tail = attackCooldown - hitTime;
            if (tail > 0f)
                yield return new WaitForSeconds(tail);

            skeletonAnimation.state.ClearTrack(0);
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
        }

        public override bool PushesPlayer => pushPlayer;
    }
}