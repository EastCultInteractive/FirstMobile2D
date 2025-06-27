using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Spine;
using Spine.Unity;
using Resources.Scripts.Player;
using Resources.Scripts.Labyrinth;
using Resources.Scripts.Enemy.Enum;

namespace Resources.Scripts.Enemy.Controllers
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyController : MonoBehaviour
    {
        [Header("Common Settings")]
        [SerializeField] private string enemyName = "Enemy";

        [Header("Animations")]
        [SerializeField] private SerializedDictionary<EnemyAnimationName, string> animations;

        [Header("Patrol Settings (Labyrinth)")]
        [SerializeField] private float patrolRadius = 3f;
        [SerializeField] private float patrolSpeedMultiplier = 0.5f;

        [Header("Detection & Obstacles")]
        [SerializeField] private LayerMask obstacleMask;

        [Header("Debug Settings")]
        [SerializeField] private bool debugLog;
        
        [Header("Push Settings")]
        [Tooltip("Если true, при ударе игрок будет отталкиваться")]
        [SerializeField] private bool pushPlayer = true;
        
        public virtual bool PushPlayer => pushPlayer;
        
        private Rigidbody2D rb;
        private BoxCollider2D attackZoneCollider;
        protected PlayerController player;
        private LabyrinthField labField;
        private List<Vector3> currentPath = new List<Vector3>();
        private int pathIndex;

        protected bool isAttacking;
        private bool isChasing;
        protected float lastAttackTime;

        private Vector2 roamDirection;
        private float roamTimeRemaining;

        protected EnemyStatsHandler stats;
        protected SkeletonAnimation skeletonAnimation;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            var zoneTransform = transform.Find("AttackZoneCollider");
            if (zoneTransform == null)
            {
                Debug.LogError($"[{enemyName}] AttackZoneCollider отсутствует");
                return;
            }
            attackZoneCollider = zoneTransform.GetComponent<BoxCollider2D>();

            var spineChild = transform.Find("SpineVisual");
            if (spineChild == null)
            {
                Debug.LogError($"[{enemyName}] SpineVisual отсутствует");
                return;
            }

            skeletonAnimation = spineChild.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                Debug.LogError($"[{enemyName}] SkeletonAnimation отсутствует");
                return;
            }

            skeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void Start()
        {
            stats = GetComponent<EnemyStatsHandler>();
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.GetComponent<PlayerController>();

            labField = LabyrinthGeneratorWithWalls.CurrentField;
            roamTimeRemaining = 0f;

            var sd = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(true);
            var anim = sd.FindAnimation(animations[EnemyAnimationName.Attack]);
            if (anim != null) OnAdjustAttackCooldown(anim.Duration);

            PlayAnimation(EnemyAnimationName.Idle, true);
        }

        private void Update()
        {
            if (!player || player.IsDead)
            {
                isAttacking = false;
                isChasing = false;

                if (labField != null) PatrolLabyrinth();
                else RoamArena();

                return;
            }

            var dist = Vector3.Distance(transform.position, player.transform.position);
            var sees = dist <= stats.DetectionRange;

            if (sees && CanAttack(dist))
            {
                AttemptAttack();
                return;
            }

            isAttacking = false;

            if (sees)
            {
                ChaseBehavior(dist);
            }
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

        private void RoamArena()
        {
            if (isAttacking) return;

            if (roamTimeRemaining <= 0f)
            {
                var a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                roamDirection = new Vector2(Mathf.Cos(a), Mathf.Sin(a)).normalized;
                roamTimeRemaining = Random.Range(2f, 5f);

                PlayAnimation(EnemyAnimationName.Walk, true);
            }

            roamTimeRemaining -= Time.deltaTime;
            var speed = stats.MovementSpeed * stats.SlowMultiplier;
            var move = new Vector3(roamDirection.x, roamDirection.y, 0f) * (speed * Time.deltaTime);

            transform.position += move;
            TurnToTarget(move);
        }

        private void PatrolLabyrinth()
        {
            if (isAttacking) return;

            if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                var from = WorldToCell(transform.position);
                var to = new Vector2Int(
                    Random.Range(0, labField.Rows),
                    Random.Range(0, labField.Cols)
                );

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

            var goal = currentPath[pathIndex];
            var dir = (goal - transform.position).normalized;

            TurnToTarget(dir);

            var speed = stats.MovementSpeed * stats.SlowMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, goal) < 0.05f)
                pathIndex++;
        }

        private Vector2Int WorldToCell(Vector3 w) =>
            new Vector2Int(
                Mathf.RoundToInt(-w.y / labField.CellSizeY),
                Mathf.RoundToInt(w.x / labField.CellSizeX)
            );

        private void ChaseBehavior(float dist)
        {
            if (!isChasing)
            {
                isChasing = true;
                OnStartChase();
            }

            if (labField != null)
            {
                if (pathIndex >= currentPath.Count)
                    BuildPath(WorldToCell(transform.position), WorldToCell(player.transform.position));
                FollowPath();
            }
            else
            {
                var dir = (player.transform.position - transform.position).normalized;
                TurnToTarget(dir);

                var speed = stats.MovementSpeed * stats.SlowMultiplier;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    player.transform.position,
                    speed * Time.deltaTime
                );

                PlayAnimation(EnemyAnimationName.Walk, true);
            }
        }

        protected abstract bool CanAttack(float distanceToPlayer);
        protected abstract void AttemptAttack();
        protected virtual void OnAdjustAttackCooldown(float animationDuration) { }
        protected virtual void OnStartChase() { }
        

        protected TrackEntry PlayAnimation(EnemyAnimationName animName, bool loop)
        {
            var current = skeletonAnimation.state.GetCurrent(0);
            return current?.Animation.Name == animations[animName]
                ? null
                : skeletonAnimation.state.SetAnimation(0, animations[animName], loop);
        }

        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (!entry.Loop)
                PlayAnimation(EnemyAnimationName.Idle, true);
        }

        public void ApplySlow(float factor, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(SlowEffect(factor, duration));
        }

        private IEnumerator SlowEffect(float factor, float duration)
        {
            stats.SetSlowMultiplier(factor);
            yield return new WaitForSeconds(duration);
            stats.ResetSlowMultiplier();
        }

        protected bool HasLineOfSight()
        {
            if (!player) return false;

            var origin = attackZoneCollider.transform.position;
            var dir = (player.transform.position - origin).normalized;
            var dist = Vector3.Distance(origin, player.transform.position);

            return !Physics2D.Raycast(origin, dir, dist, obstacleMask).collider;
        }

        private void TurnToTarget(Vector3 dir) =>
            transform.eulerAngles = dir.x < 0f
                ? Vector3.zero
                : new Vector3(0f, 180f, 0f);
    }
}
