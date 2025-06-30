using UnityEngine;
using Resources.Scripts.Enemy.Controllers;

namespace Resources.Scripts.SpellMode.Skills
{
    /// <summary>
    /// ----------------------------------------------------------------------------
    /// Skill that pushes enemies away within a specified radius.
    /// ----------------------------------------------------------------------------
    /// </summary>
    public class PushEnemiesSkill : SkillBase
    {
        [Header("Push Enemies Settings")]
        public float effectRadius = 5f;
        public float pushForce = 10f;

        private Collider2D[] resultsBuffer = new Collider2D[50];

        protected override void ActivateSkill()
        {
            var filter = new ContactFilter2D { useTriggers = false };

            Physics2D.OverlapCircle(transform.position, effectRadius, filter, resultsBuffer);
            foreach (var hit in resultsBuffer)
            {
                if (!hit.CompareTag("Enemy"))
                    return;
                
                var enemy = hit.GetComponent<EnemyController>();
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                enemy.ApplyPush(direction * pushForce);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, effectRadius);
        }
    }
}
