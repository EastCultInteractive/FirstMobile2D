using UnityEngine;

namespace Resources.Scripts.Enemy
{
    public class EnemyStatsHandler : MonoBehaviour
    {
        [Header("Combat Stats")]
        [SerializeField, Range(1, 50), Tooltip("Damage dealt by the enemy.")]
        private int damage = 3;
        [SerializeField, Range(1, 100), Tooltip("Health points of the enemy.")]
        private int health = 10;
        [SerializeField, Range(0.1f, 5f), Tooltip("Multiplier for the enemy's attack speed.")]
        private float attackSpeedMultiplier = 1f;

        [Header("Movement Stats")]
        [SerializeField, Range(1, 15), Tooltip("Movement speed of the enemy.")]
        private int movementSpeed = 1;
        [SerializeField, Range(0.1f, 5f), Tooltip("Multiplier for slowing effects.")]
        private float slowMultiplier = 1f;
        [SerializeField, Tooltip("Range within which the enemy can detect the player.")]
        private float detectionRange = 5f;

        /// <summary>
        /// Gets the enemy's damage value.
        /// </summary>
        public int Damage => damage;

        /// <summary>
        /// Gets the enemy's health points.
        /// </summary>
        public int Health => health;

        /// <summary>
        /// Gets the enemy's attack speed multiplier.
        /// </summary>
        public float AttackSpeedMultiplier => attackSpeedMultiplier;

        /// <summary>
        /// Gets the enemy's movement speed.
        /// </summary>
        public int MovementSpeed => movementSpeed;

        /// <summary>
        /// Gets the current slow multiplier.
        /// </summary>
        public float SlowMultiplier => slowMultiplier;

        /// <summary>
        /// Gets the detection range.
        /// </summary>
        public float DetectionRange => detectionRange;

        /// <summary>
        /// Applies a slow effect by setting the multiplier.
        /// </summary>
        public void SetSlowMultiplier(float factor)
        {
            slowMultiplier = factor;
        }

        /// <summary>
        /// Resets slow multiplier back to 1.
        /// </summary>
        public void ResetSlowMultiplier()
        {
            slowMultiplier = 1f;
        }
    }
}
