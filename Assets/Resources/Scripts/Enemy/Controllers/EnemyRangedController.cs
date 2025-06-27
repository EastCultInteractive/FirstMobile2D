using UnityEngine;
using System.Collections;
using Resources.Scripts.Enemy.Enum;

namespace Resources.Scripts.Enemy.Controllers
{
    public class EnemyRangedController : EnemyController
    {
        [Header("Ranged Attack Settings")]
        public float attackRange = 5f;
        public float rangedAttackCooldown = 2f;
        public float projectileSpawnDelay = 1.8f;
        public float bindingDuration = 0.5f;
        public GameObject projectilePrefab;
        public Transform attackPoint;
        public float projectileSpeed = 5f;
        public float projectileLifeTime = 5f;
        public float projectileDamage = 1f;
        public float projectileSpreadAngle;
        public Vector3 projectileScale = Vector3.one;

        protected override void OnAdjustAttackCooldown(float animationDuration)
        {
            rangedAttackCooldown = Mathf.Max(rangedAttackCooldown, animationDuration);
        }

        protected override bool CanAttack(float distanceToPlayer)
        {
            return distanceToPlayer <= attackRange && HasLineOfSight();
        }

        protected override void AttemptAttack()
        {
            if (isAttacking) return;

            float since = Time.time - lastAttackTime;
            if (since >= rangedAttackCooldown)
                StartCoroutine(PerformRangedAttack());
        }

        private IEnumerator PerformRangedAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            var track = PlayAnimation(EnemyAnimationName.Attack, false);
            yield return new WaitForSeconds(projectileSpawnDelay);

            if (player != null && !player.IsDead && HasLineOfSight())
                SpawnProjectile();

            float remaining = Mathf.Max(0f, track.Animation.Duration - projectileSpawnDelay);
            yield return new WaitForSeconds(remaining);

            isAttacking = false;
        }

        private void SpawnProjectile()
        {
            var origin = attackPoint != null ? attackPoint.position : transform.position;
            var dir = (player.transform.position - origin).normalized;

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
