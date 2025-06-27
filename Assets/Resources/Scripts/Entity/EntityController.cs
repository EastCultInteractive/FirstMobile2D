using System;
using UnityEngine;

namespace Resources.Scripts.Entity
{
	public class EntityController : MonoBehaviour
	{
		private Rigidbody2D rb;

		private void Awake()
		{
			rb = GetComponent<Rigidbody2D>();
		}

		public void ApplyPush(Vector2 force)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
		
        public void MakeDash(Vector3 direction)
        {
            // Translate the transform in the normalized direction.
            transform.Translate(direction.normalized);
        }
        
        public void TakeDamage(EntityController from, EntityController target)
        {
            // if (isImmortal || isRolling || IsDead || drawingManager.IsDrawing || playerStats.TryEvade(transform.position))
            //     return;

            var damage = stats.Damage;
            playerStats.Health -= damage;
            StartCoroutine(DamageFlash());

            if (playerStats.Health <= 0)
            {
                Die();
                return;
            }

            if (push)
                MakeDash(transform.position - enemy.transform.position);
        }
	}
}