using UnityEngine;
using Resources.Scripts.Player;

namespace Resources.Scripts.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GoblinProjectile : MonoBehaviour
    {
        private Vector2 moveDirection;
        private float speed;
        private float bindingDuration;
        private Rigidbody2D rb;

        [Tooltip("Lifetime of the projectile in seconds.")]
        [SerializeField] private float lifeTime = 5f;
        
        /// <summary>
        /// Устанавливает параметры полёта и связывания.
        /// </summary>
        public void SetParameters(Vector2 direction, float projectileSpeed, float bindDuration, float projectileLifeTime, float projectileDamage)
        {
            moveDirection = direction;
            speed = projectileSpeed;
            bindingDuration = bindDuration;
            lifeTime = projectileLifeTime;
            // projectileDamage передаётся, но тут не используется, т.к. урон снаряд не наносит

            Destroy(gameObject, lifeTime);
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = moveDirection * speed;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                var player = collision.GetComponent<PlayerController>();
                if (player != null && !player.IsDead)
                {
                    // Проигрываем анимацию удара и связываем
                    player.ApplyBinding(bindingDuration);
                }

                Destroy(gameObject);
            }
            else if (!collision.CompareTag("Enemy"))
            {
                Destroy(gameObject);
            }
        }
    }
}