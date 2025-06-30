using UnityEngine;

namespace Resources.Scripts.Entity
{
	public class EntityStats: MonoBehaviour
	{
        [Header("Health Settings")]
        [SerializeField, Range(5, 200)] private int health = 100;
        [SerializeField] private int maxHealth = 100;

        public int Health
        {
	        get => health;
	        set => health = Mathf.Clamp(value, 0, maxHealth);
        }
        
        [Header("Movement Stats")]
        [SerializeField, Range(1, 15)] private int movementSpeed = 1;
        [SerializeField, Range(0.1f, 5f)] private float slowMultiplier = 1f;
        public int MovementSpeed => movementSpeed;
        public float SlowMultiplier
        {
	        get => slowMultiplier;
	        set => slowMultiplier = value;
        }

        [Header("Combat Stats")]
        [SerializeField, Range(1, 50)] private int damage = 3;
        [SerializeField, Range(0.1f, 5f)] private float attackSpeedMultiplier = 1f;
        [SerializeField, Range(1, 10)] private int pushDistance;
        
        public int PushDistance => pushDistance;
        public int Damage => damage;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
	}
}