using UnityEngine;

namespace Resources.Scripts.Entity
{
	public class EntityStats: MonoBehaviour
	{
        [Header("Health Settings")]
        [SerializeField, Range(5, 200)] private int health = 100;
        [SerializeField] private int maxHealth = 100;
        public int Health { get => health; set => health = Mathf.Clamp(value, 0, maxHealth); }
	}
}