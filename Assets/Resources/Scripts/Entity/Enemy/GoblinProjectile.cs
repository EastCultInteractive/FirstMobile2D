using UnityEngine;

namespace Resources.Scripts.Entity.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GoblinProjectile : MonoBehaviour
    {
        private Vector2 _moveDirection;
        private float _speed;
        private float _bindingDuration;
        private Rigidbody2D _rb;

        [Tooltip("Lifetime of the projectile in seconds.")]
        [SerializeField]
        private float lifeTime = 5f;

        /// <summary>
        /// Устанавливает параметры полёта и связывания.
        /// </summary>
        public void SetParameters(Vector2 direction, float projectileSpeed, float bindDuration, float projectileLifeTime, float projectileDamage)
        {
            _moveDirection = direction;
            _speed = projectileSpeed;
            _bindingDuration = bindDuration;
            lifeTime = projectileLifeTime;

            Destroy(gameObject, lifeTime);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = _moveDirection * _speed;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            
            var player = collision.GetComponent<EntityController>();
            if (player != null && !player.IsDead)
                player.ApplyStun(_bindingDuration);
            Destroy(gameObject);
        }
    }
}