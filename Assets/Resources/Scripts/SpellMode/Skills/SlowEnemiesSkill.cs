using Resources.Scripts.Enemy.Controllers;
using UnityEngine;

namespace Resources.Scripts.SpellMode.Skills
{
    /// <summary>
    /// Не работает, позже обновим.
    /// </summary>
    public class SlowEnemiesSkill : SkillBase
    {
        [Header("Slow Enemies Settings")]
        [Tooltip("Radius within which enemies will be slowed.")]
        public float effectRadius = 5f;
        [Tooltip("Slow multiplier (e.g., 0.5 reduces enemy speed to 50%).")]
        public float slowMultiplier = 0.5f;
        [Tooltip("Duration of the slow effect in seconds.")]
        public float slowDuration = 3f;

        // Pre-allocated array to store collider results.
        private readonly Collider2D[] _resultsBuffer = new Collider2D[50];

        /// <summary>
        /// Activates the slow effect on all enemies within the effect radius.
        /// </summary>
        protected override void ActivateSkill()
        {
            // Configure a ContactFilter2D that ignores triggers and uses no layer mask.
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = false
            };

            // Perform a non-allocating physics query.
            int count = Physics2D.OverlapCircle(transform.position, effectRadius, filter, _resultsBuffer);
            for (int i = 0; i < count; i++)
            {
                var hit = _resultsBuffer[i];
                if (!hit.CompareTag("Enemy"))
                    continue;

                var enemy = hit.GetComponent<EnemyController>();
                if (enemy == null)
                    continue;

                enemy.ApplySlow(slowMultiplier, slowDuration);
            }

            Debug.Log($"SlowEnemiesSkill activated: radius={effectRadius}, multiplier={slowMultiplier}, duration={slowDuration}");
        }

        /// <summary>
        /// Draws the effect radius in the editor for visualization.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, effectRadius);
        }
    }
}
