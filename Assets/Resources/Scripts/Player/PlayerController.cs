using UnityEngine;
using System;
using System.Collections;
using Resources.Scripts.Enemy;
using Resources.Scripts.Misc;
using Resources.Scripts.Labyrinth;
using Spine;
using Spine.Unity;
using UObject = UnityEngine.Object;
using UnityEngine.Rendering.Universal;

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
        #region Trail Effects (новое)
        [Header("Trail Effects")]
        [Tooltip("Системы частиц для эффекта движения (2 для ушей, 2 для ног)")]
        [SerializeField] private ParticleSystem[] motionTrails;

        [Header("Trail Settings")]
        [Tooltip("Минимальный стартовый размер частицы")]
        [SerializeField, Range(0.01f, 0.5f)] private float trailStartSizeMin = 0.05f;
        [Tooltip("Максимальный стартовый размер частицы")]
        [SerializeField, Range(0.01f, 0.5f)] private float trailStartSizeMax = 0.1f;
        [Tooltip("Длительность жизни частицы (сек)")]
        [SerializeField, Range(0.05f, 1f)] private float trailLifetime = 0.2f;
        [Tooltip("Цвет частиц")]
        [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.5f);
        [Tooltip("Скорость эмиссии частиц (Rate over Distance)")]
        [SerializeField, Range(0f, 100f)] private float trailEmissionRate = 20f;
        [Tooltip("Множитель обратной скорости для частиц")]
        [SerializeField, Range(0.1f, 5f)] private float trailVelocityMultiplier = 1f;
        [Tooltip("Растяжение частицы по направлению движения")]
        [SerializeField, Range(1f, 10f)] private float trailLengthScale = 3f;
        [Header("Trail Trigger")]
        [Tooltip("Минимальная реальная скорость (ед./с), при которой включается эффект")]
        [SerializeField, Range(0f, 20f)] private float trailSpeedThreshold = 1f;
        #endregion

        #region Constants
        private const string SlowAnimationName   = "Goes_01_002";
        private const string RunAnimationName    = "Run_02_001";
        private const string JumpAnimationName   = "Jamp_04_001";
        private const string DeathAnimationName  = "Death_05";
        private static readonly string[] IdleAnimations = {
            "Idle_02_003"
        };
        private const float SlowThreshold = 0.5f;
        private const float IdleThreshold = 0.1f;
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

        [Header("DarkSkull / Troll Damage Settings")]
        [SerializeField] private int maxDarkSkullHits = 2;

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
        private int darkSkullHitCount;

        private Vector2 lastMoveDirection = Vector2.left;
        private bool isRolling;
        private bool canRoll = true;
        private float rollCooldownRemaining;
        private float rollDuration;

        private bool idleCycling;
        private int idleIndex;

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

        // Trail modules и renderers
        private ParticleSystem.EmissionModule[] trailEmissions;
        private ParticleSystem.MainModule[] trailMains;
        private ParticleSystem.VelocityOverLifetimeModule[] trailVelocities;
        private ParticleSystemRenderer[] trailRenderers;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            // Плавная интерполяция между FixedUpdate-рендерами
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                Debug.LogError("PlayerController: SkeletonAnimation не назначен", this);
                return;
            }

            initialScaleX = skeletonAnimation.Skeleton.ScaleX;
            skeletonAnimation.state.Complete += HandleAnimationComplete;
            var anim = skeletonAnimation.Skeleton.Data.FindAnimation(JumpAnimationName);
            rollDuration = anim != null ? anim.Duration : 0.3f;

            // Инициализируем Trail Effects
            if (motionTrails != null && motionTrails.Length > 0)
            {
                int len = motionTrails.Length;
                trailEmissions   = new ParticleSystem.EmissionModule[len];
                trailMains       = new ParticleSystem.MainModule[len];
                trailVelocities  = new ParticleSystem.VelocityOverLifetimeModule[len];
                trailRenderers   = new ParticleSystemRenderer[len];

                for (int i = 0; i < len; i++)
                {
                    var ps = motionTrails[i];

                    // Main
                    var main = ps.main;
                    main.startLifetime = trailLifetime;
                    main.startSize     = new ParticleSystem.MinMaxCurve(trailStartSizeMin, trailStartSizeMax);
                    main.startColor    = trailColor;
                    trailMains[i]      = main;

                    // Emission
                    var em = ps.emission;
                    em.rateOverDistance = trailEmissionRate;
                    em.rateOverTime     = 0f;
                    trailEmissions[i]   = em;

                    // Velocity over Lifetime
                    var vel = ps.velocityOverLifetime;
                    vel.enabled         = true;
                    trailVelocities[i]  = vel;

                    // Renderer: Stretch Billboard
                    var renderer        = ps.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode  = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale = trailLengthScale;
                    trailRenderers[i]    = renderer;
                }
            }
        }

        private void Start()
        {
            playerStats = GetComponent<PlayerStatsHandler>();
            PlayIdleSequence();

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
                // Получаем «сырые» значения от джойстика или клавиатуры
                float h = joystick != null ? joystick.Horizontal : Input.GetAxis("Horizontal");
                float v = joystick != null ? joystick.Vertical   : Input.GetAxis("Vertical");
                Vector2 rawInput = new Vector2(h, v);

                // Сглаживаем ввод, чтобы убрать рывки
                moveInput = Vector2.SmoothDamp(moveInput, rawInput, ref inputSmoothVelocity, inputSmoothTime);

                // Анимации всё так же на основе сглаженного moveInput
                UpdateMovementAnimation(moveInput);
            }

            UpdateLightOuterRange();
            TickRollCooldown();
            UpdateTrailEffects(moveInput);

            if (Input.GetKeyDown(KeyCode.LeftShift)) TryRoll();
            if (!isRolling && Input.GetKeyDown(KeyCode.Space))
                PlayAnimation(JumpAnimationName, false);
        }

        private void FixedUpdate()
        {
            if (IsDead) return;
            if (!isRolling && LabyrinthMapController.Instance?.IsMapActive != true)
            {
                // Движение через MovePosition — согласовано с FixedUpdate и интерполяцией
                float spd = playerStats.GetTotalMoveSpeed() * currentSlowMultiplier;
                Vector2 delta = moveInput.normalized * spd * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + delta);
            }
        }

        private void LateUpdate()
        {
            // Здесь можно сделать любые чисто визуальные корректировки,
            // которые нужно применить после интерполяции Rigidbody2D.
        }
        #endregion

        #region Movement & Animation Helpers
        private void UpdateMovementAnimation(Vector2 dir)
        {
            if (LabyrinthMapController.Instance?.IsMapActive == true || dir.magnitude <= IdleThreshold)
            {
                if (!idleCycling)
                    PlayIdleSequence();
                return;
            }

            idleCycling = false;
            lastMoveDirection = dir.normalized;

            PlayAnimation(
                dir.magnitude < SlowThreshold ? SlowAnimationName : RunAnimationName,
                true
            );

            if (Mathf.Abs(dir.x) > 0.01f)
                skeletonAnimation.Skeleton.ScaleX = Mathf.Abs(initialScaleX) * -Mathf.Sign(dir.x);
        }

        private void UpdateTrailEffects(Vector2 dir)
        {
            if (trailEmissions == null) return;

            float currentSpeed = playerStats.GetTotalMoveSpeed() * dir.magnitude * currentSlowMultiplier;
            bool running = currentSpeed >= trailSpeedThreshold;

            for (int i = 0; i < trailEmissions.Length; i++)
            {
                trailEmissions[i].enabled = running;
                if (running)
                {
                    Vector3 baseVel = -new Vector3(dir.normalized.x, dir.normalized.y, 0f)
                                      * currentSpeed
                                      * trailVelocityMultiplier;
                    trailVelocities[i].x = new ParticleSystem.MinMaxCurve(baseVel.x);
                    trailVelocities[i].y = new ParticleSystem.MinMaxCurve(baseVel.y);
                }
            }
        }
        #endregion

        #region Dodge Roll
        public void TryRoll()
        {
            if (canRoll && !isRolling && !IsDead)
                StartCoroutine(RollCoroutine());
        }

        private IEnumerator RollCoroutine()
        {
            isRolling = true;
            canRoll = false;
            rollCooldownRemaining = rollCooldown;
            OnRollCooldownChanged?.Invoke(1f);

            skeletonAnimation.state.SetAnimation(0, JumpAnimationName, false);

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

        public void TakeDamage(EnemyController enemy)
        {
            if (isImmortal || isRolling || IsDead || playerStats.TryEvade(transform.position)) return;

            playerStats.Health -= enemy.GetComponent<EnemyStatsHandler>().Damage;
            StartCoroutine(DamageFlash());

            if (playerStats.Health <= 0f)
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
        public void ReceiveDarkSkullHit()
        {
            if (++darkSkullHitCount >= maxDarkSkullHits)
                Die();
        }
        public void ReceiveTrollHit() => Die();
        private void Die()
        {
            IsDead = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            enabled = false;
            skeletonAnimation.state.ClearTracks();
            var entry = skeletonAnimation.state.SetAnimation(0, DeathAnimationName, false);
            entry.Complete += trackEntry =>
            {
                if (trackEntry.Animation.Name == DeathAnimationName)
                {
                    foreach (var canvas in UObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        canvas.gameObject.SetActive(false);
                    Destroy(gameObject);
                }
            };
        }
        #endregion

        #region Spine Helper
        private void PlayAnimation(string animName, bool loop)
        {
            if (Array.IndexOf(IdleAnimations, animName) < 0)
                idleCycling = false;
            var current = skeletonAnimation.state.GetCurrent(0);
            if (current?.Animation.Name == animName) return;
            skeletonAnimation.state.SetAnimation(0, animName, loop);
        }
        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (idleCycling && entry.Animation.Name == IdleAnimations[idleIndex])
            {
                idleIndex = (idleIndex + 1) % IdleAnimations.Length;
                skeletonAnimation.state.SetAnimation(0, IdleAnimations[idleIndex], false);
            }
            else if (entry.Animation.Name == JumpAnimationName)
            {
                PlayIdleSequence();
            }
        }
        private void PlayIdleSequence()
        {
            idleCycling = true;
            idleIndex = 0;
            skeletonAnimation.state.SetAnimation(0, IdleAnimations[idleIndex], false);
        }
        #endregion
    }
}
