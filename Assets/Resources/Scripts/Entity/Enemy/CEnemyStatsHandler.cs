using Resources.Scripts.Entity;
using UnityEngine;

namespace Resources.Scripts.Enemy
{
    public class CEnemyStatsHandler : EntityStats
    {
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
