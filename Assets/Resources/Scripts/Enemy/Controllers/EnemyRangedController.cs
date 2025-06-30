using UnityEngine;
using Random = UnityEngine.Random;

namespace Resources.Scripts.Enemy.Controllers
{
    public class EnemyRangedController : EnemyController
    {
        [Header("Ranged Attack Settings")]
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float rangedAttackCooldown = 2f;
        [SerializeField] private float projectileSpawnDelay = 1.8f;
        [SerializeField] private float bindingDuration = 0.5f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileLifeTime = 5f;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private float projectileSpreadAngle;
        [SerializeField] private Vector3 projectileScale = Vector3.one;

        protected override void PerformAttack()
        {
            base.PerformAttack();
            if (!Player) return;
            
            SpawnProjectile();
        }

        private void SpawnProjectile()
        {
            var origin = attackPoint ? attackPoint.position : transform.position;
            var dir = (Player.transform.position - origin).normalized;

            if (projectileSpreadAngle > 0f)
                dir = Quaternion.Euler(
                    0, 0, Random.Range(-projectileSpreadAngle, projectileSpreadAngle)
                ) * dir;

            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            p.transform.localScale = projectileScale;

            if (p.TryGetComponent<GoblinProjectile>(out var gp))
                gp.SetParameters(
                    dir,
                    projectileSpeed,
                    bindingDuration,
                    projectileLifeTime,
                    projectileDamage
                );
            else if (p.TryGetComponent<Rigidbody2D>(out var rb2))
                rb2.AddForce(dir * projectileSpeed, ForceMode2D.Impulse);
        }
    }
}
