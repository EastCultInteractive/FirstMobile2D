using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using AYellowpaper.SerializedCollections;
using Resources.Scripts.Player;
using Resources.Scripts.Labyrinth;
using Resources.Scripts.Enemy.Enum;

namespace Resources.Scripts.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Common Settings")]
        public string enemyName = "Enemy";
        public EnemyType enemyType = EnemyType.Standard;

        [Header("Animations")]
        [SerializedDictionary("Animation Code", "Value")]
        public SerializedDictionary<EnemyAnimationName, string> animations;

        [Header("Patrol Settings (Labyrinth)")]
        public float patrolRadius = 3f;
        public float patrolSpeedMultiplier = 0.5f;

        [Header("Standard (Melee) Attack Settings")]
        [Tooltip("Range within which melee enemies will attack.")]
        public float attackRange = 1f;
        [Tooltip("Minimum time between attacks; will be clamped to animation length.")]
        public float attackCooldown = 1f;
        [Tooltip("Should this enemy push the player on hit?")]
        public bool pushPlayer = true;
        [Tooltip("Force with which melee enemies push the player.")]
        public float pushForceMultiplier = 1f;

        [Header("Goblin (Ranged) Attack Settings")]
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
        [Tooltip("Delay before projectile spawn relative to start of animation")]
        public float goblinProjectileSpawnDelay = 1.8f;

        [Header("Detection & Obstacles")]
        public LayerMask obstacleMask;

        [Header("Debug Settings")]
        public bool debugLog;

        #endregion

        #region Private Fields

        private Rigidbody2D rb;
        private BoxCollider2D attackZoneCollider;
        private PlayerController player;
        private LabyrinthField labField;
        private List<Vector3> currentPath = new List<Vector3>();
        private int pathIndex;

        private bool isAttacking;
        private bool isChasing;
        private float lastAttackTime;

        private Vector2 roamDirection;
        private float roamTimeRemaining;

        private EnemyStatsHandler stats;

        private SkeletonAnimation skeletonAnimation;

        // Для управления эффектами замедления
        private Coroutine slowEffectCoroutine;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            var zoneTransform = transform.Find("AttackZoneCollider");
            if (zoneTransform == null)
            {
                Debug.LogError($"[{enemyName}] Не найден дочерний объект AttackZoneCollider");
                return;
            }

            attackZoneCollider = zoneTransform.GetComponent<BoxCollider2D>();
            if (attackZoneCollider == null)
            {
                Debug.LogError($"[{enemyName}] У объекта AttackZoneCollider нет BoxCollider2D");
                return;
            }

            var spineChild = transform.Find("SpineVisual");
            if (spineChild == null)
            {
                Debug.LogError($"[{enemyName}] Отсутствует дочерний объект SpineVisual");
                return;
            }
            skeletonAnimation = spineChild.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                Debug.LogError($"[{enemyName}] Отсутствует SkeletonAnimation на SpineVisual");
                return;
            }

            skeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void Start()
        {
            stats = GetComponent<EnemyStatsHandler>();

            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.GetComponent<PlayerController>();

            labField = LabyrinthGeneratorWithWalls.CurrentField;
            roamTimeRemaining = 0f;

            var sd = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(true);
            var anim = sd.FindAnimation(animations[EnemyAnimationName.Attack]);
            if (anim != null)
                attackCooldown = Mathf.Max(attackCooldown, anim.Duration);

            PlayAnimation(EnemyAnimationName.Idle, true);
        }

        private void Update()
        {
            if (player == null || player.IsDead)
            {
                isAttacking = false;
                isChasing = false;
                if (labField != null) PatrolLabyrinth();
                else RoamArena();
                return;
            }

            float dist = Vector3.Distance(transform.position, player.transform.position);
            bool sees = dist <= stats.DetectionRange;

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
            if (player == null || player.IsDead) return;
            if (other == attackZoneCollider && other.CompareTag("Player"))
                AttemptAttack();
        }

        #endregion

        #region Arena Roaming and Patrol

        private void RoamArena()
        {
            if (isAttacking) return;

            if (roamTimeRemaining <= 0f)
            {
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                roamDirection = new Vector2(Mathf.Cos(a), Mathf.Sin(a)).normalized;
                roamTimeRemaining = Random.Range(2f, 5f);
                PlayAnimation(EnemyAnimationName.Walk, true);
            }

            roamTimeRemaining -= Time.deltaTime;
            float currentSpeed = stats.MovementSpeed * stats.SlowMultiplier;
            Vector3 move = new Vector3(roamDirection.x, roamDirection.y, 0f) * currentSpeed * Time.deltaTime;
            transform.position += move;
            TurnToTarget(move);
        }

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
            PlayAnimation(EnemyAnimationName.Walk, true);
        }

        private void FollowPath()
        {
            if (pathIndex >= currentPath.Count)
            {
                currentPath.Clear();
                PlayAnimation(EnemyAnimationName.Idle, true);
                return;
            }

            Vector3 goal = currentPath[pathIndex];
            Vector3 dir = (goal - transform.position).normalized;
            TurnToTarget(dir);
            float currentSpeed = stats.MovementSpeed * stats.SlowMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, goal, currentSpeed * Time.deltaTime);
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
                    PlayAnimation(EnemyAnimationName.Walk, true);
                }
                Vector3 dir = (player.transform.position - transform.position).normalized;
                TurnToTarget(dir);
                float currentSpeed = stats.MovementSpeed * stats.SlowMultiplier;
                transform.position = Vector3.MoveTowards(transform.position, player.transform.position, currentSpeed * Time.deltaTime);
            }
        }

        private void RecalculatePathToPlayer()
        {
            if (labField == null) return;
            BuildPath(WorldToCell(transform.position), WorldToCell(player.transform.position));
        }

        private void AttemptAttack()
        {
            if (player == null || player.IsDead) return;
            if (isAttacking) return;

            float since = Time.time - lastAttackTime;
            switch (enemyType)
            {
                case EnemyType.Goblin:
                    if (since >= goblinAttackCooldownTime) StartCoroutine(PerformGoblinAttack());
                    break;
                default:
                    if (since >= attackCooldown) StartCoroutine(PerformMeleeAttack());
                    break;
            }
        }

        private IEnumerator PerformMeleeAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            PlayAnimation(EnemyAnimationName.Attack, false);

            float hitTime = attackCooldown * 0.4f;
            yield return new WaitForSeconds(hitTime);

            player.TakeDamage(this, stats);

            yield return new WaitForSeconds(attackCooldown - hitTime);

            skeletonAnimation.state.ClearTrack(0);

            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
        }

        private IEnumerator PerformGoblinAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            
            var track = PlayAnimation(EnemyAnimationName.Attack, false);
            
            yield return new WaitForSeconds(goblinProjectileSpawnDelay);
            
            if (!player.IsDead && HasLineOfSight())
                SpawnProjectileEvent();
            
            float remaining = Mathf.Max(0f, track.Animation.Duration - goblinProjectileSpawnDelay);
            yield return new WaitForSeconds(remaining);

            isAttacking = false;
        }

        #endregion

        #region Spine Helper

        private TrackEntry PlayAnimation(EnemyAnimationName animName, bool loop)
        {
            var current = skeletonAnimation.state.GetCurrent(0);
            if (current?.Animation.Name == animations[animName]) return null;
            return skeletonAnimation.state.SetAnimation(0, animations[animName], loop);
        }

        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (!entry.Loop)
                PlayAnimation(EnemyAnimationName.Idle, true);
        }

        #endregion

        #region Animation Events

        public void SpawnProjectileEvent()
        {
            if (player == null || player.IsDead) return;
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

        #endregion

        #region Physics Utilities

        public void ApplyPush(Vector2 force)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        public void ApplySlow(float factor, float duration)
        {
            if (slowEffectCoroutine != null)
                StopCoroutine(slowEffectCoroutine);
            slowEffectCoroutine = StartCoroutine(SlowEffect(factor, duration));
        }

        private IEnumerator SlowEffect(float factor, float duration)
        {
            stats.SetSlowMultiplier(factor);
            yield return new WaitForSeconds(duration);
            stats.ResetSlowMultiplier();
            slowEffectCoroutine = null;
        }

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
