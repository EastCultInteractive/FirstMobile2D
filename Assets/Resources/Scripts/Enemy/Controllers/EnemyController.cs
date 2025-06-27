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
    public class EnemyController : MonoBehaviour
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
        
        
        protected PlayerController Player;
        
        protected float LastAttackTime;
        protected bool IsAttacking;
        
        private bool isChasing;
        
        private Rigidbody2D rb;
        private BoxCollider2D attackZoneCollider;
        
        private LabyrinthField labField;
        private List<Vector3> currentPath = new();
        private int pathIndex;
        
        private Vector2 roamDirection;
        private float roamTimeRemaining;

        protected EnemyStatsHandler Stats;
        protected SkeletonAnimation SkeletonAnimation;
        
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

            SkeletonAnimation = spineChild.GetComponent<SkeletonAnimation>();
            if (SkeletonAnimation == null)
            {
                Debug.LogError($"[{enemyName}] SkeletonAnimation отсутствует");
                return;
            }

            SkeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void Start()
        {
            Stats = GetComponent<EnemyStatsHandler>();
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) Player = go.GetComponent<PlayerController>();

            labField = LabyrinthGeneratorWithWalls.CurrentField;
            roamTimeRemaining = 0f;

            var sd = SkeletonAnimation.SkeletonDataAsset.GetSkeletonData(true);
            var anim = sd.FindAnimation(animations[EnemyAnimationName.Attack]);
            if (anim != null) OnAdjustAttackCooldown(anim.Duration);

            PlayAnimation(EnemyAnimationName.Idle, true);
        }

        private void Update()
        {
            if (!Player || Player.IsDead)
            {
                IsAttacking = false;
                isChasing = false;

                if (labField != null) PatrolLabyrinth();
                else RoamArena();

                return;
            }

            var dist = Vector3.Distance(transform.position, Player.transform.position);
            var sees = dist <= Stats.DetectionRange;

            if (sees && CanAttack(dist))
            {
                AttemptAttack();
                return;
            }

            IsAttacking = false;

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
            if (Player == null || Player.IsDead) return;

            if (other == attackZoneCollider && other.CompareTag("Player"))
                AttemptAttack();
        }

        private void RoamArena()
        {
            if (IsAttacking) return;

            if (roamTimeRemaining <= 0f)
            {
                var a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                roamDirection = new Vector2(Mathf.Cos(a), Mathf.Sin(a)).normalized;
                roamTimeRemaining = Random.Range(2f, 5f);

                PlayAnimation(EnemyAnimationName.Walk, true);
            }

            roamTimeRemaining -= Time.deltaTime;
            var speed = Stats.MovementSpeed * Stats.SlowMultiplier;
            var move = new Vector3(roamDirection.x, roamDirection.y, 0f) * (speed * Time.deltaTime);

            transform.position += move;
            TurnToTarget(move);
        }

        private void PatrolLabyrinth()
        {
            if (IsAttacking) return;

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

            var speed = Stats.MovementSpeed * Stats.SlowMultiplier;
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
                    BuildPath(WorldToCell(transform.position), WorldToCell(Player.transform.position));
                FollowPath();
            }
            else
            {
                var dir = (Player.transform.position - transform.position).normalized;
                TurnToTarget(dir);

                var speed = Stats.MovementSpeed * Stats.SlowMultiplier;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    Player.transform.position,
                    speed * Time.deltaTime
                );

                PlayAnimation(EnemyAnimationName.Walk, true);
            }
        }

        protected TrackEntry PlayAnimation(EnemyAnimationName animName, bool loop)
        {
            var current = SkeletonAnimation.state.GetCurrent(0);
            return current?.Animation.Name == animations[animName]
                ? null
                : SkeletonAnimation.state.SetAnimation(0, animations[animName], loop);
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
        
        /// <summary>
        /// Применяет импульсную силу к врагу.
        /// </summary>
        /// <param name="force">Вектор силы, в направлении которого будет отброшен враг.</param>
        public void ApplyPush(Vector2 force)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        private IEnumerator SlowEffect(float factor, float duration)
        {
            Stats.SetSlowMultiplier(factor);
            yield return new WaitForSeconds(duration);
            Stats.ResetSlowMultiplier();
        }

        protected bool HasLineOfSight()
        {
            if (!Player) return false;

            var origin = attackZoneCollider.transform.position;
            var dir = (Player.transform.position - origin).normalized;
            var dist = Vector3.Distance(origin, Player.transform.position);

            return !Physics2D.Raycast(origin, dir, dist, obstacleMask).collider;
        }

        private void TurnToTarget(Vector3 dir) =>
            transform.eulerAngles = dir.x < 0f
                ? Vector3.zero
                : new Vector3(0f, 180f, 0f);

        protected virtual bool CanAttack(float distanceToPlayer)
        {
            return false;
        }

        protected virtual void AttemptAttack()
        {
        }
        protected virtual void OnAdjustAttackCooldown(float animationDuration) { }
        protected virtual void OnStartChase() { }
        public bool PushPlayer => pushPlayer;
    }
}
