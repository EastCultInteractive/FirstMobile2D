using UnityEngine;
using System;
using System.Collections;
using AYellowpaper.SerializedCollections;
using Resources.Scripts.Labyrinth;
using Spine;
using Resources.Scripts.GameManagers;
using Resources.Scripts.Player.Enum;
using Resources.Scripts.Entity;
using Resources.Scripts.SpellMode;

namespace Resources.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : EntityController
    {
        [Header("Animations")]
        [SerializedDictionary("Animation Code", "Value")]
        public SerializedDictionary<PlayerAnimationName, string> animations;

        [Header("Movement Settings")]
        [SerializeField] private PlayerJoystick joystick;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private float inputSmoothTime = 0.1f;

        [Header("Dodge Roll Settings")]
        [SerializeField] private float rollDistance = 6f;
        [SerializeField] private float rollCooldown = 2f;
        [SerializeField, Range(0.1f, 3f)] private float rollSpeedMultiplier = 1f;

        #region Public Events & Properties
        public event Action<float> OnRollCooldownChanged;
        public float RollCooldownDuration => rollCooldown;
        #endregion

        #region Private Fields

        private PlayerStatsHandler playerStats;
        private bool bonusActive;

        private Vector2 lastMoveDirection = Vector2.left;
        private bool isRolling;
        private bool canRoll = true;
        private float rollCooldownRemaining;
        private float rollDuration;

        // Сохраняем исходный ScaleX скелета
        private float initialScaleX;

        // Rigidbody2D и ввод
        private Vector2 moveInput;
        private Vector2 inputSmoothVelocity;

        private DrawingManager drawingManager;
        private LabyrinthMapController mapController;
        #endregion

        #region Unity Methods
        
        private void Start()
        {
            InitComponents();
            InitAnimations();
        }
        
        #region Init

        private void InitComponents()
        {
            playerStats = GetComponent<PlayerStatsHandler>();
            drawingManager = GetComponent<DrawingManager>();
        }

        private void InitAnimations()
        {
            SkeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void InitLabyrinth()
        {
            mapController = LabyrinthMapController.Instance;
        }
        #endregion

        private void Update()
        {
            if (IsDead) return;

            if (!isRolling)
            {
                var h = joystick ? joystick.Horizontal : 0f;
                var v = joystick ? joystick.Vertical : 0f;

                if (Mathf.Approximately(h + v, 0f))
                {
                    h = Input.GetAxis("Horizontal");
                    v = Input.GetAxis("Vertical");
                }

                var rawInput = new Vector2(h, v);

                moveInput = Vector2.SmoothDamp(moveInput, rawInput, ref inputSmoothVelocity, inputSmoothTime);
                UpdateMovementAnimation(moveInput);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift)) TryRoll();
        }

        private void FixedUpdate()
        {
            if (IsDead || isRolling || mapController.IsMapActive) return;
            var speed = playerStats.MovementSpeed * playerStats.SlowMultiplier;
            var delta = moveInput.normalized * (speed * Time.fixedDeltaTime);
            RigidBodyInstance.MovePosition(RigidBodyInstance.position + delta);
        }
        #endregion

        #region Movement & Animation Helpers
        private void UpdateMovementAnimation(Vector2 dir)
        {
            lastMoveDirection = dir.normalized;

            if (drawingManager.IsDrawing)
            {
                PlayAnimation(animations, PlayerAnimationName.Draw);
            }
            else
            {
                PlayAnimation(animations, PlayerAnimationName.Run, true);
            }

            if (Mathf.Abs(dir.x) > 0.01f)
                SkeletonAnimation.Skeleton.ScaleX = Mathf.Abs(initialScaleX) * -Mathf.Sign(dir.x);
        }
        #endregion

        #region Dodge Roll
        public void TryRoll()
        {
            if (canRoll && !isRolling && !IsDead && !drawingManager.IsDrawing)
                StartCoroutine(RollCoroutine());
        }

        private IEnumerator RollCoroutine()
        {
            isRolling = true;
            canRoll = false;
            rollCooldownRemaining = rollCooldown;
            OnRollCooldownChanged?.Invoke(1f);

            PlayAnimation(animations, PlayerAnimationName.Jump);

            var baseSpeed = rollDistance / rollDuration;
            var effectiveRollSpeed = baseSpeed * rollSpeedMultiplier;
            RigidBodyInstance.linearVelocity = Vector2.zero;

            var elapsed = 0f;
            while (elapsed < rollDuration)
            {
                var step = lastMoveDirection * (effectiveRollSpeed * Time.deltaTime);
                RigidBodyInstance.MovePosition(RigidBodyInstance.position + step);
                elapsed += Time.deltaTime;
                yield return null;
            }

            isRolling = false;
            yield return new WaitForSeconds(rollCooldownRemaining);
            canRoll = true;
        }

        private void TickRollCooldown()
        {
            if (rollCooldownRemaining <= 0f) return;
            rollCooldownRemaining = Mathf.Max(0f, rollCooldownRemaining - Time.deltaTime);
            OnRollCooldownChanged?.Invoke(rollCooldownRemaining / rollCooldown);
        }
        #endregion

        #region Other Effects
        protected override void Die()
        {
            PlayAnimation(animations, PlayerAnimationName.Death);
        }
        #endregion

        #region Spine Helper
        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (entry.Animation.Name == animations[PlayerAnimationName.Death])
            {
                StageProgressionManager.Instance.ShowGameOver();
                Destroy(gameObject);
            }
            
            PlayAnimation(animations, PlayerAnimationName.Idle);
        }
        #endregion
    }
}
