using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Resources.Scripts.Entity.Enemy.Enum;
using Resources.Scripts.Entity.Labyrinth;
using Resources.Scripts.Entity.Player;
using Resources.Scripts.Utils;
using Spine;
using UnityEngine;
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
        
        protected PlayerController Player;

        private LabyrinthField _labField;
        private List<Vector3> _currentPath = new();
        private int _pathIndex;

        private EnemyStats _enemyStats;

        private Timer _moveDirectionTimer;


        private void Start()
        {
            InitEnemy();
            InitAnimations();
            InitLabyrinthField();
            
            // Добавляем настройки физики
            RigidBodyInstance.linearDamping = 5f; // Настройте значение
            RigidBodyInstance.angularDamping = 5f; // Настройте значение
            RigidBodyInstance.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        #region Initialization Flow
        private void InitEnemy()
        {
            _enemyStats = GetComponent<EnemyStats>();
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) Player = player.GetComponent<PlayerController>();
            
            _moveDirectionTimer = new Timer(3f, 7f);
            _moveDirectionTimer.OnTimerFinished += ResetMoveDirection;
            
            MoveDirection = Random.insideUnitCircle;
        }

        private void InitAnimations()
        {
            SkeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void InitLabyrinthField()
        {
            _labField = LabyrinthGeneratorWithWalls.CurrentField;
        }
        #endregion
        
        private void Update()
        {
            var distance = 0f;
            var sees = false;

            if (Player)
            {
                distance = Vector3.Distance(transform.position, Player.transform.position);
                sees = distance <= _enemyStats.DetectionRange;
            }

            UpdateAttack(distance);
            UpdateChase(sees, out MoveDirection);
            UpdateAnimations();
            
            _moveDirectionTimer.Tick(Time.deltaTime);
        }

        #region UpdateFlow
        private void UpdateAttack(float distance)
        {
            if (distance <= _enemyStats.AttackRange || !Player) return;
            PlayAnimation(animations, EEnemyAnimationName.Attack);
        }

        private void UpdateChase(bool sees, out Vector3 direction)
        {
            direction = MoveDirection;
            if (MoveDirection != Vector3.zero) return;
            if (!sees) return;
            
            direction = ChaseBehavior();
        }

        private void UpdateAnimations()
        {
            if (GetCurrentVelocity() > 0f) PlayAnimation(animations, EEnemyAnimationName.Walk, true);
            else if (Mathf.Approximately(GetCurrentVelocity(), 0f)) PlayAnimation(animations, EEnemyAnimationName.Idle, true);
        }
        #endregion
        
        
        private Vector3 PatrolLabyrinth()
        {
            if (_currentPath.Count == 0 || _pathIndex >= _currentPath.Count)
            {
                var from = WorldToCell(transform.position);
                var to = new Vector2Int(
                    Random.Range(0, _labField.Rows),
                    Random.Range(0, _labField.Cols)
                );

                BuildPath(from, to);
            }

            return FollowPath();
        }

        private void BuildPath(Vector2Int from, Vector2Int to)
        {
            var cells = _labField.FindPath(from, to);
            _currentPath = _labField.PathToWorld(cells);
            _pathIndex = 0;
        }

        private Vector3 FollowPath()
        {
            if (_pathIndex >= _currentPath.Count)
            {
                _currentPath.Clear();
                return Vector3.zero;
            }

            var goal = _currentPath[_pathIndex];

            if (Vector3.Distance(transform.position, goal) < 0.05f)
                _pathIndex++;
            
            return goal;
        }

        private Vector2Int WorldToCell(Vector3 w) =>
            new(
                Mathf.RoundToInt(-w.y / _labField.CellSizeY),
                Mathf.RoundToInt(w.x / _labField.CellSizeX)
            );

        private Vector3 ChaseBehavior()
        {
            if (_labField == null) return Player.transform.position;
            if (_pathIndex >= _currentPath.Count)
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
        
        private void ResetMoveDirection(object sender, EventArgs e)
        {
            MoveDirection = _labField == null ? Random.insideUnitCircle : PatrolLabyrinth();
        }

        protected virtual void PerformAttack()
        {
            
        }
    }
}