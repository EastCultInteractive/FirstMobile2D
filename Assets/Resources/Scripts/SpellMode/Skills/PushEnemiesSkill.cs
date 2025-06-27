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
        [Tooltip("Radius within which enemies will be pushed.")]
        public float effectRadius = 5f;
        [Tooltip("Force with which enemies are pushed.")]
        public float pushForce = 10f;

        // Pre-allocated array for collider results to avoid runtime allocations.
        private Collider2D[] resultsBuffer = new Collider2D[50];

        /// <summary>
        /// Activates the push effect on all enemies within the effect radius.
        /// </summary>
        protected override void ActivateSkill()
        {
            // Set up a default contact filter; triggers are not included.
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };

            // Use the non-allocating OverlapCircle to get colliders.
            int count = Physics2D.OverlapCircle(transform.position, effectRadius, filter, resultsBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = resultsBuffer[i];
                if (hit.CompareTag("Enemy"))
                {
                    EnemyController enemy = hit.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        Vector2 direction = (hit.transform.position - transform.position).normalized;
                        enemy.ApplyPush(direction * pushForce);
                    }
                }
            }
            Debug.Log("PushEnemiesSkill activated.");
        }

        /// <summary>
        /// Draws the effect radius in the editor for visualization.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, effectRadius);
        }
    }
}
