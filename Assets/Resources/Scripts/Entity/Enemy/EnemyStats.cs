using UnityEngine;

namespace Resources.Scripts.Entity.Enemy
{
    public class EnemyStats : EntityStats
    {
        [Header("Combat")]
        [SerializeField, Range(1f, 15f)] private float attackRange = 3f;

        public float AttackRange => attackRange;
        
        [Header("Arena Roam Stats")]
        [SerializeField] private float detectionRange = 5f;

        public float DetectionRange => detectionRange;
        
        [Header("Labyrinth Patrol Stats")] 
        [SerializeField] private float patrolRadius = 3f;
        [SerializeField] private float patrolSpeedMultiplier = 0.5f;
        
        public float PatrolSpeedMultiplier => patrolSpeedMultiplier;
        public float PatrolRadius => patrolRadius;
    }
}
