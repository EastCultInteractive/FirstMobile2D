using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Resources.Scripts.Entity;
using Resources.Scripts.Entity.Enemy.Enum;
using Spine;
using Resources.Scripts.Labyrinth;
using Resources.Scripts.Entity.Player;
using Random = UnityEngine.Random;

namespace Resources.Scripts.Entity.Enemy.Controllers
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : EntityController
    {

        [Header("Animations")] [SerializeField]
        private SerializedDictionary<EEnemyAnimationName, string> animations;
        
        [Header("Detection & Obstacles")] [SerializeField]
        private LayerMask obstacleMask;

        [Header("Attack Settings")]
        [SerializeField] private float attackRange = 1f;

        protected PlayerController Player;

        private LabyrinthField labField;
        private List<Vector3> currentPath = new();
        private int pathIndex;

        private Vector3 moveDirection = Vector3.zero;
        private EnemyStats enemyStats;



        private void Start()
        {
            InitEnemy();
            InitAnimations();
            InitLabyrinthField();
            InitCoroutines();
            
            // Добавляем настройки физики
            RigidBodyInstance.linearDamping = 5f; // Настройте значение
            RigidBodyInstance.angularDamping = 5f; // Настройте значение
            RigidBodyInstance.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        #region Initialization Flow
        private void InitEnemy()
        {
            enemyStats = GetComponent<EnemyStats>();
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) Player = player.GetComponent<PlayerController>();
        }

        private void InitAnimations()
        {
            SkeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void InitLabyrinthField()
        {
            labField = LabyrinthGeneratorWithWalls.CurrentField;
        }

        private void InitCoroutines()
        {
            StartCoroutine(ResetMoveDirectionTimer());
        }
        #endregion
        
        private void Update()
        {
            var distance = 0f;
            var sees = false;

            if (Player)
            {
                distance = Vector3.Distance(transform.position, Player.transform.position);
                sees = distance <= enemyStats.DetectionRange;
            }

            UpdateAttack(distance);
            UpdateRoam(out moveDirection);
            UpdateChase(sees, out moveDirection);
        }

        private void FixedUpdate()
        {
            UpdateMove();
            UpdateAnimations();
        }

        #region UpdateFlow
        private void UpdateRoam(out Vector3 direction)
        {
            direction = moveDirection;
            if (moveDirection != Vector3.zero) return;

            direction = labField == null ? RoamArena() : PatrolLabyrinth();
        }

        private void UpdateAttack(float distance)
        {
            if (distance <= attackRange || !Player) return;
            PlayAnimation(animations, EEnemyAnimationName.Attack);
        }

        private void UpdateChase(bool sees, out Vector3 direction)
        {
            direction = moveDirection;
            if (moveDirection != Vector3.zero) return;
            if (!sees) return;
            
            direction = ChaseBehavior();
        }

        private void UpdateMove()
        {
            var speed = enemyStats.MovementSpeed * enemyStats.SlowMultiplier;
            
            RigidBodyInstance.AddForce(moveDirection.normalized * (speed * Time.deltaTime), ForceMode2D.Force);
            TurnToDirection(moveDirection);
            
            UpdateAnimationSpeed(GetCurrentVelocity());
        }

        private void UpdateAnimations()
        {
            if (GetCurrentVelocity() > 0f) PlayAnimation(animations, EEnemyAnimationName.Walk, true);
            else if (Mathf.Approximately(GetCurrentVelocity(), 0f)) PlayAnimation(animations, EEnemyAnimationName.Idle, true);
        }
        #endregion
        
        
        private Vector3 RoamArena()
        {
            var dirAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var roamDirection = new Vector2(Mathf.Cos(dirAngle), Mathf.Sin(dirAngle)).normalized * Random.Range(0f, 10f);
            return roamDirection;
        }

        private float GetCurrentVelocity()
        {
            return RigidBodyInstance.linearVelocity.magnitude;
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


        private void HandleAnimationComplete(TrackEntry entry)
        {
            var completedAnimation = GetAnimationByName(animations, entry.Animation.Name);
            if (completedAnimation == EEnemyAnimationName.Attack) PerformAttack();
        }

        protected bool HasLineOfSight()
        {
            if (!Player) return false;

            var origin = transform.position;
            var dir = (Player.transform.position - origin).normalized;
            var dist = Vector3.Distance(origin, Player.transform.position);

            return !Physics2D.Raycast(origin, dir, dist, obstacleMask).collider;
        }

        protected virtual void PerformAttack()
        {
            
        }

        private IEnumerator ResetMoveDirectionTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(3f, 6f));
                moveDirection = Vector3.zero;
            }
        }
    }
}