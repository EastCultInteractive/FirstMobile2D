using UnityEngine;
using System;
using System.Collections;
using Resources.Scripts.Enemy;
using Resources.Scripts.Misc;
using Resources.Scripts.Labyrinth;
using Spine;
using Spine.Unity;
using UnityEngine.Rendering.Universal;
using Resources.Scripts.GameManagers;

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
        [Tooltip("Скорость эмиссии частиц (Rate over Time)")]
        [SerializeField, Range(0f, 100f)] private float trailEmissionRate = 20f;
        [Tooltip("Множитель обратной скорости для частиц")]
        [SerializeField, Range(0.1f, 5f)] private float trailVelocityMultiplier = 1f;
        [Tooltip("Растяжение частицы по направлению движения")]
        [SerializeField, Range(1f, 10f)] private float trailLengthScale = 3f;

        [Header("Trail Trigger")]
        [Tooltip("Минимальная реальная скорость (ед./с), при которой включается эффект")]
        [SerializeField, Range(0f, 20f)] private float trailSpeedThreshold = 1f;
        #endregion

        #region Animation Names (публичные поля)
        [Header("Animation Names")]
        [Tooltip("Название анимации для беглого передвижения")]
        [SerializeField] private string runAnimationName = "Run_03_001";
        [Tooltip("Название анимации для медленного передвижения")]
        [SerializeField] private string slowAnimationName = "Goes_01_002";
        [Tooltip("Название анимации для прыжка / кувырка")]
        [SerializeField] private string jumpAnimationName = "Jamp_04_001";
        [Tooltip("Название анимации для смерти")]
        [SerializeField] private string deathAnimationName = "Death_05";
        [Tooltip("Названия анимаций для простоя (будет переключаться по очереди)")]
        [SerializeField] private string[] idleAnimations = { "Idle_02_003" };
        [Tooltip("Название анимации для режима черчения (Drawing Mode)")]
        [SerializeField] private string drawingAnimationName = "Draw_06_001";

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
        private int darkSkullHitCount;

        private Vector2 lastMoveDirection = Vector2.left;
        private bool isRolling;
        private bool canRoll = true;
        private float rollCooldownRemaining;
        private float rollDuration;

        private bool idleCycling;
        private int idleIndex;

        // Для режима черчения
        private bool isDrawing;

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
            var anim = skeletonAnimation.Skeleton.Data.FindAnimation(jumpAnimationName);
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
                    main.startLifetime      = trailLifetime;
                    main.startSize          = new ParticleSystem.MinMaxCurve(trailStartSizeMin, trailStartSizeMax);
                    main.startColor         = trailColor;
                    main.simulationSpace    = ParticleSystemSimulationSpace.World; 
                    trailMains[i]           = main;

                    var em = ps.emission;
                    em.rateOverDistance     = 0f;
                    em.rateOverTime         = trailEmissionRate;
                    trailEmissions[i]       = em;

                    var shape = ps.shape;
                    shape.enabled           = true;

                    var vel = ps.velocityOverLifetime;
                    vel.enabled             = true;
                    trailVelocities[i]      = vel;

                    // Renderer: Stretch Billboard + сортировка над спрайтом
                    var renderer            = ps.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode     = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale    = trailLengthScale;
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder     = 10;
                    trailRenderers[i]         = renderer;
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
            if (IsDead || isDrawing) return;

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

                // Мгновенная остановка, если джойстик отпущен
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
            UpdateTrailEffects(moveInput);

            if (Input.GetKeyDown(KeyCode.LeftShift)) TryRoll();
            if (!isRolling && Input.GetKeyDown(KeyCode.Space))
                PlayAnimation(jumpAnimationName, false);

            // Вход в режим черчения
            if (Input.GetButtonDown("SpellModeButton"))
                StartCoroutine(DrawingCoroutine());
        }

        private void FixedUpdate()
        {
            if (IsDead || isDrawing) return;
            if (!isRolling && LabyrinthMapController.Instance?.IsMapActive != true)
            {
                float spd = playerStats.GetTotalMoveSpeed() * currentSlowMultiplier;
                Vector2 delta = moveInput.normalized * spd * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + delta);
            }
        }
        #endregion

        #region Movement & Animation Helpers
        private void UpdateMovementAnimation(Vector2 dir)
        {
            if (LabyrinthMapController.Instance?.IsMapActive == true || dir.magnitude <= idleThreshold)
            {
                if (!idleCycling)
                    PlayIdleSequence();
                return;
            }

            idleCycling = false;
            lastMoveDirection = dir.normalized;

            PlayAnimation(runAnimationName, true);

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

        #region Drawing Mode
        private IEnumerator DrawingCoroutine()
        {
            isDrawing = true;
            // Запускаем анимацию черчения, без зацикливания
            skeletonAnimation.state.SetAnimation(0, drawingAnimationName, false);

            // Ожидаем окончания или максимальное время
            yield return new WaitForSeconds(maxDrawingTime);

            // Возвращаемся в простой
            PlayIdleSequence();
            isDrawing = false;
        }
        #endregion

        #region Dodge Roll
        public void TryRoll()
        {
            if (canRoll && !isRolling && !IsDead && !isDrawing)
                StartCoroutine(RollCoroutine());
        }

        private IEnumerator RollCoroutine()
        {
            isRolling = true;
            canRoll = false;
            rollCooldownRemaining = rollCooldown;
            OnRollCooldownChanged?.Invoke(1f);

            skeletonAnimation.state.SetAnimation(0, jumpAnimationName, false);

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
            if (isImmortal || isRolling || IsDead || isDrawing || playerStats.TryEvade(transform.position)) return;

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
            var entry = skeletonAnimation.state.SetAnimation(0, deathAnimationName, false);
            entry.Complete += trackEntry =>
            {
                if (trackEntry.Animation.Name == deathAnimationName)
                {
                    StageProgressionManager.Instance.ShowGameOver();
                    Destroy(gameObject);
                }
            };
        }
        #endregion

        #region Spine Helper
        private void PlayAnimation(string animName, bool loop)
        {
            if (Array.IndexOf(idleAnimations, animName) < 0)
                idleCycling = false;
            var current = skeletonAnimation.state.GetCurrent(0);
            if (current?.Animation.Name == animName) return;
            skeletonAnimation.state.SetAnimation(0, animName, loop);
        }
        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (idleCycling && entry.Animation.Name == idleAnimations[idleIndex])
            {
                idleIndex = (idleIndex + 1) % idleAnimations.Length;
                skeletonAnimation.state.SetAnimation(0, idleAnimations[idleIndex], false);
            }
            else if (entry.Animation.Name == jumpAnimationName)
            {
                PlayIdleSequence();
            }
        }
        private void PlayIdleSequence()
        {
            idleCycling = true;
            idleIndex = 0;
            skeletonAnimation.state.SetAnimation(0, idleAnimations[idleIndex], false);
        }
        #endregion
    }
}
