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
using Resources.Scripts.SpellMode;

namespace Resources.Scripts.Player
{
    /// <summary>
    /// Controls player movement, light, animation, dodge roll, evasion and traps.
    /// Uses Spine animation instead of Unity Animator / SpriteRenderer.
    /// When damaged, player briefly flashes red.
    /// Добавлен «ветровой» эффект от ушей и ног при беге с помощью ParticleSystem.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
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

        [Header("Animation Settings")]
        [Tooltip("Ссылка на Spine SkeletonAnimation (дочерний объект)")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Dodge Roll Settings")]
        [SerializeField, Tooltip("Дальность кувырка (в единицах Unity)")]
        private float rollDistance = 6f;
        [SerializeField, Tooltip("Кулдаун между кувырками")]
        private float rollCooldown = 2f;
        [SerializeField, Tooltip("Множитель скорости движения при кувырке (1 = стандартная скорость)"), Range(0.1f, 3f)]
        private float rollSpeedMultiplier = 1f;

        [Header("Damage Flash Settings")]
        [Tooltip("Цвет мигания при получении урона")]
        [SerializeField] private Color flashColor = Color.red;
        [Tooltip("Длительность мигания (секунд)")]
        [SerializeField, Range(0.05f, 1f)] private float flashDuration = 0.3f;

        [Header("Drawing Mode Settings")]
        [Tooltip("Макс. время рисования, с.")]
        [SerializeField, Range(0.1f, 10f)] private float maxDrawingTime = 5f;

        #region Public Events & Properties
        public event Action<float> OnRollCooldownChanged;
        public float RollCooldownDuration => rollCooldown;
        public bool IsDead { get; private set; }
        #endregion

        #region Private Fields
        private PlayerStatsHandler playerStats;
        private float currentSlowMultiplier = 1f;
        private Coroutine slowCoroutine;
        private bool bonusActive;

        private Vector2 lastMoveDirection = Vector2.left;
        private bool isRolling;
        private bool canRoll = true;
        private float rollCooldownRemaining;
        private float rollDuration;

        // Сохраняем исходный ScaleX скелета
        private float initialScaleX;

        // Rigidbody2D и ввод
        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 inputSmoothVelocity;

        // Для светового флеша урона
        private Coroutine flashCoroutine;

        // Для расчета светового эффекта финиша
        private float initialDistance = -1f;

        private DrawingManager drawingManager;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            drawingManager = GetComponent<DrawingManager>();

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                Debug.LogError("PlayerController: SkeletonAnimation не назначен", this);
                return;
            }

            initialScaleX = skeletonAnimation.Skeleton.ScaleX;
            skeletonAnimation.state.Complete += HandleAnimationComplete;
            var anim = skeletonAnimation.Skeleton.Data.FindAnimation(animations[PlayerAnimationName.Jump]);
            rollDuration = anim != null ? anim.Duration : 0.3f;
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
                float h = joystick != null ? joystick.Horizontal : 0f;
                float v = joystick != null ? joystick.Vertical : 0f;

                if (Mathf.Approximately(h + v, 0f))
                {
                    h = Input.GetAxis("Horizontal");
                    v = Input.GetAxis("Vertical");
                }

                Vector2 rawInput = new Vector2(h, v);

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
            if (!isRolling && LabyrinthMapController.Instance?.IsMapActive != true)
            {
                float spd = playerStats.GetTotalMoveSpeed() * currentSlowMultiplier;
                Vector2 delta = moveInput.normalized * (spd * Time.fixedDeltaTime);
                rb.MovePosition(rb.position + delta);
            }
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
                skeletonAnimation.Skeleton.ScaleX = Mathf.Abs(initialScaleX) * -Mathf.Sign(dir.x);
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

            float baseSpeed = rollDistance / rollDuration;
            float effectiveRollSpeed = baseSpeed * rollSpeedMultiplier;
            rb.linearVelocity = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < rollDuration)
            {
                Vector2 step = lastMoveDirection * (effectiveRollSpeed * Time.deltaTime);
                rb.MovePosition(rb.position + step);
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
            if (finishPoint == null || playerLight == null || initialDistance < 0f) return;
            float t = 1f - Mathf.Clamp01(Vector2.Distance(transform.position, finishPoint.position) / initialDistance);
            playerLight.pointLightOuterRadius = Mathf.Lerp(baseLightRange, maxLightRange, t);
        }

        private IEnumerator WaitForFinishMarker()
        {
            while (finishPoint == null)
            {
                var obj = GameObject.FindGameObjectWithTag(ETag.Fairy.ToString());
                if (obj != null)
                {
                    finishPoint = obj.transform;
                    initialDistance = Vector2.Distance(transform.position, finishPoint.position);
                }
                yield return null;
            }
        }
        #endregion

        #region Damage and Evasion
        public IEnumerator DamageFlash()
        {
            if (flashCoroutine != null) yield break;
            flashCoroutine = StartCoroutine(_DamageFlash());
            yield return flashCoroutine;
        }

        private IEnumerator _DamageFlash()
        {
            var skel = skeletonAnimation.Skeleton;
            Color orig = skel.GetColor();
            skel.SetColor(new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a));
            yield return new WaitForSeconds(flashDuration);
            skel.SetColor(orig);
            flashCoroutine = null;
        }

        /// <summary>
        /// Единый метод получения урона от любого врага.
        /// </summary>
        public void TakeDamage(EnemyController enemy, EnemyStatsHandler stats)
        {
            if (isImmortal || isRolling || IsDead || drawingManager.IsDrawing || playerStats.TryEvade(transform.position))
                return;

            int damage = stats.Damage; // прямой доступ к кэшированному полю
            playerStats.Health -= damage;
            StartCoroutine(DamageFlash());

            if (playerStats.Health <= 0)
            {
                Die();
                return;
            }

            if (enemy.pushPlayer)
                EntityUtils.MakeDash(transform, transform.position - enemy.transform.position);
        }
        #endregion

        #region Other Effects
        public void ApplySlow(float factor, float duration)
        {
            if (slowCoroutine != null) StopCoroutine(slowCoroutine);
            slowCoroutine = StartCoroutine(SlowCoroutine(factor, duration));
        }
        private IEnumerator SlowCoroutine(float factor, float duration)
        {
            currentSlowMultiplier = factor;
            yield return new WaitForSeconds(duration);
            currentSlowMultiplier = 1f;
        }
        public void ApplyBinding(float duration) => StartCoroutine(BindingCoroutine(duration));
        private IEnumerator BindingCoroutine(float duration)
        {
            float orig = currentSlowMultiplier;
            currentSlowMultiplier = 0f;
            yield return new WaitForSeconds(duration);
            currentSlowMultiplier = orig;
        }
        public void Stun(float duration) => StartCoroutine(StunCoroutine(duration));
        private IEnumerator StunCoroutine(float duration)
        {
            float orig = currentSlowMultiplier;
            currentSlowMultiplier = 0f;
            yield return new WaitForSeconds(duration);
            currentSlowMultiplier = orig;
        }
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
        private void Die()
        {
            IsDead = true;
            PlayAnimation(PlayerAnimationName.Death, false);
        }
        #endregion

        #region Spine Helper
        private TrackEntry PlayAnimation(PlayerAnimationName animName, bool loop)
        {
            var current = skeletonAnimation.state.GetCurrent(0);
            if (current?.Animation.Name == animations[animName]) return null;
            return skeletonAnimation.state.SetAnimation(0, animations[animName], loop);
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
