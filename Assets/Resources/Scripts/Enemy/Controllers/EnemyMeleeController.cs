using UnityEngine;
using System.Collections;
using Resources.Scripts.Enemy.Enum;

namespace Resources.Scripts.Enemy.Controllers
{
    public class EnemyMeleeController : EnemyController
    {
        [Header("Melee Attack Settings")]
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField, Tooltip("Толкать игрока при ударе")]
        private bool pushPlayer = true;

        public override bool PushPlayer => pushPlayer;
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

            var since = Time.time - lastAttackTime;
            if (since >= attackCooldown)
                StartCoroutine(PerformMeleeAttack());
        }

        private IEnumerator PerformMeleeAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            PlayAnimation(EnemyAnimationName.Attack, false);

            var hitTime = attackCooldown * 0.4f;
            yield return new WaitForSeconds(hitTime);

            player.TakeDamage(this, stats);

            var tail = attackCooldown - hitTime;
            if (tail > 0f)
                yield return new WaitForSeconds(tail);

            skeletonAnimation.state.ClearTrack(0);
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
        }
        
    }
}