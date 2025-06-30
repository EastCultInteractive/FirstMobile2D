using UnityEngine;
using System;
using System.Collections;
using AYellowpaper.SerializedCollections;
using Resources.Scripts.Enemy;
using Resources.Scripts.Misc;
using Resources.Scripts.Labyrinth;
using Spine;
using Spine.Unity;
using UnityEngine.Rendering.Universal;
using Resources.Scripts.GameManagers;
using Resources.Scripts.Player.Enum;
using Resources.Scripts.Enemy.Controllers;
using Resources.Scripts.Entity;
using Resources.Scripts.SpellMode;

namespace Resources.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : EntityController
    {
        #region Animation Names (публичные поля)

        [Header("Animations")]
        [SerializedDictionary("Animation Code", "Value")]
        public SerializedDictionary<PlayerAnimationName, string> animations;

        [Header("Animation Thresholds")]
        [Tooltip("Порог скорости для медленной анимации")]
        [SerializeField, Range(0f, 2f)] private float slowThreshold = 0.5f;
        [Tooltip("Порог для переключения в простой")]
        [SerializeField, Range(0f, 1f)] private float idleThreshold = 0.1f;
        #endregion

        #region Inspector Fields
        [Header("Movement Settings")]
        [SerializeField] private PlayerJoystick joystick;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField, Tooltip("Время сглаживания ввода (сек)")]
        private float inputSmoothTime = 0.1f;
        #endregion

        [Header("Light Settings")]
        [SerializeField] private Light2D playerLight;
        [SerializeField] private Transform finishPoint;
        [SerializeField, Range(0.1f, 5f)] private float baseLightRange = 1f;
        [SerializeField, Range(1f, 2f)] private float maxLightRange  = 2f;

        [Header("Player Settings")]
        public bool isImmortal;

        [Header("Dodge Roll Settings")]
        [SerializeField, Tooltip("Дальность кувырка (в единицах Unity)")]
        private float rollDistance = 6f;
        [SerializeField, Tooltip("Кулдаун между кувырками")]
        private float rollCooldown = 2f;
        [SerializeField, Tooltip("Множитель скорости движения при кувырке (1 = стандартная скорость)"), Range(0.1f, 3f)]
        private float rollSpeedMultiplier = 1f;

        [Header("Drawing Mode Settings")]
        [Tooltip("Макс. время рисования, с.")]
        [SerializeField, Range(0.1f, 10f)] private float maxDrawingTime = 5f;

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

        // Для расчета светового эффекта финиша
        private float initialDistance = -1f;

        private DrawingManager drawingManager;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            RigidBodyInstance.gravityScale = 0f;
            RigidBodyInstance.freezeRotation = true;
            RigidBodyInstance.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            drawingManager = GetComponent<DrawingManager>();

            initialScaleX = SkeletonAnimation.Skeleton.ScaleX;
            SkeletonAnimation.state.Complete += HandleAnimationComplete;
        }

        private void Start()
        {
            playerStats = GetComponent<PlayerStatsHandler>();
            PlayAnimation(PlayerAnimationName.Idle, false);

            if (finishPoint != null)
                initialDistance = Vector2.Distance(transform.position, finishPoint.position);
            else
                StartCoroutine(WaitForFinishMarker());
        }

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

                if (rawInput.magnitude <= idleThreshold)
                {
                    moveInput = Vector2.zero;
                    inputSmoothVelocity = Vector2.zero;
                }
                else
                {
                    moveInput = Vector2.SmoothDamp(moveInput, rawInput, ref inputSmoothVelocity, inputSmoothTime);
                }

                UpdateMovementAnimation(moveInput);
            }

            UpdateLightOuterRange();
            TickRollCooldown();

            if (Input.GetKeyDown(KeyCode.LeftShift)) TryRoll();
        }

        private void FixedUpdate()
        {
            if (IsDead) return;
            if (isRolling || LabyrinthMapController.Instance?.IsMapActive == true) return;
            var spd = Stats.MovementSpeed * Stats.SlowMultiplier;
            var delta = moveInput.normalized * (spd * Time.fixedDeltaTime);
            RigidBodyInstance.MovePosition(RigidBodyInstance.position + delta);
        }
        #endregion

        #region Movement & Animation Helpers
        private void UpdateMovementAnimation(Vector2 dir)
        {
            if (LabyrinthMapController.Instance?.IsMapActive == true || dir.magnitude <= idleThreshold)
            {
                PlayAnimation(PlayerAnimationName.Idle, false);;
                return;
            }

            lastMoveDirection = dir.normalized;

            if (drawingManager.IsDrawing)
            {
                PlayAnimation(PlayerAnimationName.Draw, false);
            }
            else if (dir.magnitude < slowThreshold)
            {
                PlayAnimation(PlayerAnimationName.Step, true);
            }
            else
            {
                PlayAnimation(PlayerAnimationName.Run, true);
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

            PlayAnimation(PlayerAnimationName.Jump, false);

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

        #region Light Methods
        private void UpdateLightOuterRange()
        {
            if (!finishPoint || !playerLight || initialDistance < 0f) return;
            var t = 1f - Mathf.Clamp01(Vector2.Distance(transform.position, finishPoint.position) / initialDistance);
            playerLight.pointLightOuterRadius = Mathf.Lerp(baseLightRange, maxLightRange, t);
        }

        private IEnumerator WaitForFinishMarker()
        {
            while (!finishPoint)
            {
                var obj = GameObject.FindGameObjectWithTag(ETag.Fairy.ToString());
                if (obj)
                {
                    finishPoint = obj.transform;
                    initialDistance = Vector2.Distance(transform.position, finishPoint.position);
                }
                yield return null;
            }
        }
        #endregion

        #region Other Effects
        public void IncreaseSpeed(float mult)
        {
            if (!bonusActive)
                StartCoroutine(SpeedBoostCoroutine(mult, 5f));
        }
        private IEnumerator SpeedBoostCoroutine(float mult, float duration)
        {
            bonusActive = true;
            playerStats.ModifyMoveSpeedPercent((mult - 1f) * 100f);
            yield return new WaitForSeconds(duration);
            playerStats.ResetStats();
            bonusActive = false;
        }
        protected override void Die()
        {
            PlayAnimation(PlayerAnimationName.Death, false);
        }
        #endregion

        #region Spine Helper
        private TrackEntry PlayAnimation(PlayerAnimationName animName, bool loop)
        {
            var current = SkeletonAnimation.state.GetCurrent(0);
            if (current?.Animation.Name == animations[animName]) return null;
            return SkeletonAnimation.state.SetAnimation(0, animations[animName], loop);
        }
        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (entry.Animation.Name == animations[PlayerAnimationName.Death])
            {
                StageProgressionManager.Instance.ShowGameOver();
                Destroy(gameObject);
            }
            
            PlayAnimation(PlayerAnimationName.Idle, false);
        }
        #endregion
    }
}
