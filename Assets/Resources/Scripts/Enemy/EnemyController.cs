using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using Resources.Scripts.Player;
using Resources.Scripts.Labyrinth;

namespace Resources.Scripts.Enemy
{
    public enum EnemyType
    {
        Standard,
        Goblin,
        DarkSkull,
        Troll,
        Gremlin,
        ScullKnife
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SkeletonAnimation))]
    public class EnemyController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Common Settings")]
        public string enemyName = "Enemy";
        public EnemyType enemyType = EnemyType.Standard;

        [Header("Movement Settings")]
        public float speed = 1f;
        public float slowMultiplier = 1f;
        public float detectionRange = 5f;

        [Header("Patrol Settings (Labyrinth)")]
        public float patrolRadius = 3f;
        public float patrolSpeedMultiplier = 0.5f;

        [Header("Standard Attack Settings")]
        public float attackRange = 1f;
        [Tooltip("Минимальное время между ударами; скорректируется под длину анимации")]
        public float attackCooldown = 1f;
        public bool pushPlayer = true;
        public float pushForceMultiplier = 1f;

        [Header("Goblin Attack Settings")]
        public float goblinAttackCooldownTime = 2f;
        public float goblinAttackAnimationDuration = 1f;
        public float bindingDuration = 0.5f;
        public GameObject projectilePrefab;
        public Transform attackPoint;
        public float projectileSpeed = 5f;
        public float goblinProjectileLifeTime = 5f;
        public float goblinProjectileDamage = 1f;
        public float goblinProjectileSpreadAngle;
        public Vector3 goblinProjectileScale = Vector3.one;

        [Header("DarkSkull Attack Settings")]
        public float darkSkullAttackCooldownTime = 1f;
        public float darkSkullAttackAnimationDuration = 1f;
        public float darkSkullPushForce = 5f;

        [Header("Troll Attack Settings")]
        public float trollAttackCooldownTime = 1f;
        public float trollAttackAnimationDuration = 1f;
        public float trollPushForce = 5f;

        [Header("Spine Animation Settings")]
        public SkeletonAnimation skeletonAnimation;

        [Header("Detection & Obstacles")]
        public LayerMask obstacleMask;

        [Header("Debug Settings")]
        public bool debugLog;

        #endregion

        #region Private Fields

        private Rigidbody2D rb;
        private PlayerController player;
        private PlayerStatsHandler playerStats;
        private LabyrinthField labField;
        private List<Vector3> currentPath = new List<Vector3>();
        private int pathIndex;

        private bool isAttacking;
        private bool isChasing;
        private float lastAttackTime;

        private string idleAnim, walkAnim, attackAnim;
        private Vector2 roamDirection;
        private float roamTimeRemaining;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            var bc = GetComponent<BoxCollider2D>();
            bc.isTrigger = true;
            Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer);

            skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
                Debug.LogError($"[{enemyName}] Missing SkeletonAnimation");

            switch (enemyType)
            {
                case EnemyType.Gremlin:
                    idleAnim = "idle_01_002";
                    walkAnim = "Goes_01_002";
                    attackAnim = "Attack_01_002";
                    break;
                case EnemyType.ScullKnife:
                    idleAnim = "Idle_02_001";
                    walkAnim = "Goes_02_002";
                    attackAnim = "Attack_01_001";
                    break;
                case EnemyType.Troll:
                    idleAnim = "Idle_02_004";
                    walkAnim = "Goes_02_002";
                    attackAnim = "Attack_01_03";
                    break;
                case EnemyType.DarkSkull:
                    idleAnim = "idle_02_003";
                    walkAnim = "Походка_02_003";
                    attackAnim = "attack_02_002";
                    break;
                default:
                    idleAnim = "Idle";
                    walkAnim = "Walk";
                    attackAnim = "Attack";
                    break;
            }
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.GetComponent<PlayerController>();
                playerStats = go.GetComponent<PlayerStatsHandler>();
            }

            labField = LabyrinthGeneratorWithWalls.CurrentField;
            roamTimeRemaining = 0f;

            var sd = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(true);
            var anim = sd.FindAnimation(attackAnim);
            if (anim != null)
                attackCooldown = Mathf.Max(attackCooldown, anim.Duration);

            PlayIdleAnim();
        }

        private void Update()
        {
            if (player == null || player.isDead)
            {
                isAttacking = false;
                isChasing = false;
                if (labField != null) PatrolLabyrinth();
                else RoamArena();
                return;
            }

            float dist = Vector3.Distance(transform.position, player.transform.position);
            bool sees = dist <= detectionRange;

            if (sees) ChaseBehavior(dist);
            else
            {
                isChasing = false;
                if (labField != null) PatrolLabyrinth();
                else RoamArena();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (player == null || player.isDead) return;
            if (other.CompareTag("Player"))
                AttemptAttack();
        }

        #endregion

        #region Arena Roaming

        private void RoamArena()
        {
            if (isAttacking) return;

            if (roamTimeRemaining <= 0f)
            {
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                roamDirection = new Vector2(Mathf.Cos(a), Mathf.Sin(a)).normalized;
                roamTimeRemaining = Random.Range(2f, 5f);
                PlayWalkAnim();
            }

            roamTimeRemaining -= Time.deltaTime;
            Vector3 move = roamDirection * speed * Time.deltaTime;
            transform.position += move;
            TurnToTarget(move);
        }

        #endregion

        #region Labyrinth Patrol

        private void PatrolLabyrinth()
        {
            if (isAttacking) return;

            if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                var from = WorldToCell(transform.position);
                var to = new Vector2Int(Random.Range(0, labField.Rows), Random.Range(0, labField.Cols));
                BuildPath(from, to);
            }
            FollowPath();
        }

        private void BuildPath(Vector2Int from, Vector2Int to)
        {
            var cells = labField.FindPath(from, to);
            currentPath = labField.PathToWorld(cells);
            pathIndex = 0;
            PlayWalkAnim();
        }

        private void RecalculatePathToPlayer()
        {
            if (labField == null) return;
            BuildPath(WorldToCell(transform.position), WorldToCell(player.transform.position));
        }

        private void FollowPath()
        {
            if (pathIndex >= currentPath.Count)
            {
                currentPath.Clear();
                PlayIdleAnim();
                return;
            }

            Vector3 goal = currentPath[pathIndex];
            Vector3 dir = (goal - transform.position).normalized;
            TurnToTarget(dir);
            transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, goal) < 0.05f) pathIndex++;
        }

        private Vector2Int WorldToCell(Vector3 w) =>
            new Vector2Int(
                Mathf.RoundToInt(-w.y / labField.CellSizeY),
                Mathf.RoundToInt(w.x / labField.CellSizeX)
            );

        #endregion

        #region Chase & Attack

        private void ChaseBehavior(float dist)
        {
            bool inMelee = (enemyType != EnemyType.Goblin) && dist <= attackRange;
            bool canShoot = enemyType == EnemyType.Goblin && HasLineOfSight();

            if (inMelee || canShoot)
            {
                AttemptAttack();
                return;
            }

            if (labField != null)
            {
                if (!isChasing)
                {
                    isChasing = true;
                    RecalculatePathToPlayer();
                }
                if (pathIndex >= currentPath.Count) RecalculatePathToPlayer();
                FollowPath();
            }
            else
            {
                if (!isChasing)
                {
                    isChasing = true;
                    EnsureWalkAnim();
                }
                Vector3 dir = (player.transform.position - transform.position).normalized;
                TurnToTarget(dir);
                transform.position = Vector3.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
            }
        }

        private void AttemptAttack()
        {
            if (player == null || player.isDead) return;
            if (isAttacking) return;

            float since = Time.time - lastAttackTime;
            switch (enemyType)
            {
                case EnemyType.Goblin:
                    if (since >= goblinAttackCooldownTime) StartCoroutine(PerformGoblinAttack());
                    break;
                case EnemyType.DarkSkull:
                    if (since >= darkSkullAttackCooldownTime) StartCoroutine(PerformDarkSkullAttack());
                    break;
                case EnemyType.Troll:
                    if (since >= trollAttackCooldownTime) StartCoroutine(PerformTrollAttack());
                    break;
                default:
                    if (since >= attackCooldown) StartCoroutine(PerformStandardAttack());
                    break;
            }
        }

        private IEnumerator PerformStandardAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed; speed = 0f;
            PlayAttackAnim();

            float hitTime = attackCooldown * 0.4f;
            yield return new WaitForSeconds(hitTime);
            if (!player.isDead)
            {
                player.StartCoroutine(player.DamageFlash());
                player.TakeDamage(this);
                if (pushPlayer)
                    player.transform.position += (player.transform.position - transform.position).normalized * pushForceMultiplier;
            }
            yield return new WaitForSeconds(attackCooldown - hitTime);

            speed = oldSpeed;
            PlayIdleAnim();
            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
            EnsureWalkAnim();
        }

        private IEnumerator PerformGoblinAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed; speed = 0f;
            PlayAttackAnim();

            float hitTime = goblinAttackAnimationDuration * 0.4f;
            yield return new WaitForSeconds(hitTime);
            if (!player.isDead && HasLineOfSight())
                SpawnProjectileEvent();
            yield return new WaitForSeconds(goblinAttackAnimationDuration - hitTime);

            speed = oldSpeed;
            PlayIdleAnim();
            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
            EnsureWalkAnim();
        }

        private IEnumerator PerformDarkSkullAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed; speed = 0f;
            PlayAttackAnim();

            float hitTime = darkSkullAttackAnimationDuration * 0.4f;
            yield return new WaitForSeconds(hitTime);
            player.StartCoroutine(player.DamageFlash());
            player.ReceiveDarkSkullHit();
            if (pushPlayer)
                player.transform.position += (player.transform.position - transform.position).normalized * darkSkullPushForce;
            yield return new WaitForSeconds(darkSkullAttackAnimationDuration - hitTime);

            // Снимаем атаку с трека, чтобы точно переключиться в Idle
            skeletonAnimation.state.ClearTrack(0);

            speed = oldSpeed;
            PlayIdleAnim();
            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
            EnsureWalkAnim();
        }

        private IEnumerator PerformTrollAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed; speed = 0f;
            PlayAttackAnim();

            float hitTime = trollAttackAnimationDuration * 0.4f;
            yield return new WaitForSeconds(hitTime);
            player.StartCoroutine(player.DamageFlash());
            player.ReceiveTrollHit();
            if (pushPlayer)
                player.transform.position += (player.transform.position - transform.position).normalized * trollPushForce;
            yield return new WaitForSeconds(trollAttackAnimationDuration - hitTime);

            speed = oldSpeed;
            PlayIdleAnim();
            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
            EnsureWalkAnim();
        }

        #endregion

        #region Spine Animation Control

        private void PlayIdleAnim() => skeletonAnimation?.state.SetAnimation(0, idleAnim, true);
        private void PlayWalkAnim() => skeletonAnimation?.state.SetAnimation(0, walkAnim, true);
        private void EnsureWalkAnim()
        {
            var c = skeletonAnimation?.state.GetCurrent(0);
            if (c == null || c.Animation.Name != walkAnim) PlayWalkAnim();
        }
        private void PlayAttackAnim() => skeletonAnimation?.state.SetAnimation(0, attackAnim, false);

        #endregion

        #region Animation Events & Effects

        public void SpawnProjectileEvent()
        {
            if (player == null || player.isDead) return;
            var origin = attackPoint != null ? attackPoint.position : transform.position;
            var dir = (player.transform.position - origin).normalized;
            if (goblinProjectileSpreadAngle > 0f)
                dir = Quaternion.Euler(0, 0, Random.Range(-goblinProjectileSpreadAngle, goblinProjectileSpreadAngle)) * dir;
            if (projectilePrefab != null)
            {
                var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
                p.transform.localScale = goblinProjectileScale;
                if (p.TryGetComponent<GoblinProjectile>(out var gp))
                    gp.SetParameters(dir, projectileSpeed, bindingDuration, goblinProjectileLifeTime, goblinProjectileDamage);
                else if (p.TryGetComponent<Rigidbody2D>(out var rb2))
                    rb2.AddForce(dir * projectileSpeed, ForceMode2D.Impulse);
            }
        }

        public void RegisterDarkSkullHitEvent()
        {
            if (player == null || player.isDead) return;
            if (playerStats.TryEvade(transform.position)) return;
            player.StartCoroutine(player.DamageFlash());
            player.ReceiveDarkSkullHit();
            if (pushPlayer)
                player.transform.position += (player.transform.position - transform.position).normalized * darkSkullPushForce;
        }

        public void RegisterTrollHitEvent()
        {
            if (player == null || player.isDead) return;
            if (playerStats.TryEvade(transform.position)) return;
            player.StartCoroutine(player.DamageFlash());
            player.ReceiveTrollHit();
            if (pushPlayer)
                player.transform.position += (player.transform.position - transform.position).normalized * trollPushForce;
        }

        public void ApplySlow(float f, float d) => StartCoroutine(SlowEffect(f, d));
        private IEnumerator SlowEffect(float f, float d)
        {
            float old = speed; speed = old * slowMultiplier;
            yield return new WaitForSeconds(d);
            speed = old;
        }

        public void ApplyPush(Vector2 force) => rb.AddForce(force, ForceMode2D.Impulse);

        #endregion

        #region Line of Sight

        private bool HasLineOfSight()
        {
            if (attackPoint == null || player == null) return false;
            var origin = attackPoint.position;
            var dir = (player.transform.position - origin).normalized;
            float dist = Vector3.Distance(origin, player.transform.position);
            return Physics2D.Raycast(origin, dir, dist, obstacleMask).collider == null;
        }

        #endregion

        #region Utility

        private void TurnToTarget(Vector3 dir) =>
            transform.eulerAngles = dir.x < 0f ? Vector3.zero : new Vector3(0f, 180f, 0f);

        #endregion
    }
}
