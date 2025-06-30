using UnityEngine;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Spine;
using Resources.Scripts.Player;
using Resources.Scripts.Labyrinth;
using Resources.Scripts.Enemy.Enum;
using Resources.Scripts.Entity;
using Random = UnityEngine.Random;

namespace Resources.Scripts.Enemy.Controllers
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : EntityController
    {
        [Header("Animations")] [SerializeField]
        private SerializedDictionary<EnemyAnimationName, string> animations;

        [Header("Detection & Obstacles")] [SerializeField]
        private LayerMask obstacleMask;

        [Header("Attack Settings")]
        [SerializeField] private float attackRange = 1f;

        protected PlayerController Player;

        private LabyrinthField labField;
        private List<Vector3> currentPath = new();
        private int pathIndex;

        private Vector3 moveGoal = Vector3.zero;


        #region Initialization Flow
        private void Start()
        {
            InitEnemy();
            InitAnimations();
            InitLabyrinthField();
        }

        private void InitEnemy()
        {
            Stats = GetComponent<EnemyStatsHandler>();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) Player = player.GetComponent<PlayerController>();
        }

        private void InitAnimations()
        {
            SkeletonAnimation.state.Complete += HandleAnimationComplete;
            PlayAnimation(EnemyAnimationName.Idle, true);
        }

        private void InitLabyrinthField()
        {
            labField = LabyrinthGeneratorWithWalls.CurrentField;
        }
        #endregion
        
        #region UpdateFlow
        private void Update()
        {
            var distance = Vector3.Distance(transform.position, Player.transform.position);
            var sees = distance <= Stats.DetectionRange;
            
            UpdateAttack(distance);
            UpdateRoam(out moveGoal);
            UpdateChase(sees, out moveGoal);
            UpdateMove();
        }

        private void UpdateRoam(out Vector3 goal)
        {
            goal = moveGoal;
            if (moveGoal != Vector3.zero) return;

            goal = labField == null ? RoamArena() : PatrolLabyrinth();
        }

        private void UpdateAttack(float distance)
        {
            if (distance <= attackRange) return;
            PlayAnimation(EnemyAnimationName.Attack, false);
        }

        private void UpdateChase(bool sees, out Vector3 goal)
        {
            goal = moveGoal;
            if (!sees) return;
            
            goal = ChaseBehavior();
        }

        private void UpdateMove()
        {
            if (moveGoal == Vector3.zero) return;
            
            var speed = Stats.MovementSpeed * Stats.SlowMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, moveGoal, speed * Time.deltaTime);
            TurnToTarget(moveGoal);
            
            if (moveGoal == transform.position)
                moveGoal = Vector3.zero;
        }
        #endregion
        
        
        private Vector3 RoamArena()
        {
            var dirAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var roamDirection = new Vector2(Mathf.Cos(dirAngle), Mathf.Sin(dirAngle)).normalized * Random.Range(0f, 10f);
            var goal = roamDirection * transform.position;
            return goal;
        }

        private Vector3 PatrolLabyrinth()
        {
            if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                var from = WorldToCell(transform.position);
                var to = new Vector2Int(
                    Random.Range(0, labField.Rows),
                    Random.Range(0, labField.Cols)
                );

                BuildPath(from, to);
            }

            return FollowPath();
        }

        private void BuildPath(Vector2Int from, Vector2Int to)
        {
            var cells = labField.FindPath(from, to);
            currentPath = labField.PathToWorld(cells);
            pathIndex = 0;

            PlayAnimation(EnemyAnimationName.Walk, true);
        }

        private Vector3 FollowPath()
        {
            if (pathIndex >= currentPath.Count)
            {
                currentPath.Clear();
                return Vector3.zero;
            }

            var goal = currentPath[pathIndex];

            if (Vector3.Distance(transform.position, goal) < 0.05f)
                pathIndex++;
            
            return goal;
        }

        private Vector2Int WorldToCell(Vector3 w) =>
            new(
                Mathf.RoundToInt(-w.y / labField.CellSizeY),
                Mathf.RoundToInt(w.x / labField.CellSizeX)
            );

        private Vector3 ChaseBehavior()
        {
            if (labField == null) return Player.transform.position;
            if (pathIndex >= currentPath.Count)
                BuildPath(WorldToCell(transform.position), WorldToCell(Player.transform.position));
            
            return FollowPath();
        }

        private TrackEntry PlayAnimation(EnemyAnimationName newAnimation, bool loop)
        {
            var currentAnimation = GetCurrentAnimation();
            return currentAnimation == newAnimation
                ? null
                : SkeletonAnimation.state.SetAnimation(0, animations[newAnimation], loop);
        }

        private EnemyAnimationName GetCurrentAnimation()
        {
            var currentName = SkeletonAnimation.state.GetCurrent(0).Animation.Name;
            return GetAnimationByName(currentName);
        }

        private EnemyAnimationName GetAnimationByName(string animName)
        {
            if (animations[EnemyAnimationName.Idle] == animName) return EnemyAnimationName.Idle;
            if (animations[EnemyAnimationName.Walk] == animName) return EnemyAnimationName.Walk;
            if (animations[EnemyAnimationName.Attack] == animName) return EnemyAnimationName.Attack;

            return EnemyAnimationName.Idle;
        }

        private void HandleAnimationComplete(TrackEntry entry)
        {
            var completedAnimation = GetAnimationByName(entry.Animation.Name);
            
            if (completedAnimation == EnemyAnimationName.Attack) PerformAttack();
        }

        protected bool HasLineOfSight()
        {
            if (!Player) return false;

            var origin = transform.position;
            var dir = (Player.transform.position - origin).normalized;
            var dist = Vector3.Distance(origin, Player.transform.position);

            return !Physics2D.Raycast(origin, dir, dist, obstacleMask).collider;
        }

        private void TurnToTarget(Vector3 target)
        {
            transform.eulerAngles = (target - transform.position).x < 0f
                ? Vector3.zero
                : new Vector3(0f, 180f, 0f);
        }

        protected virtual void PerformAttack()
        {
            
        }
    }
}
