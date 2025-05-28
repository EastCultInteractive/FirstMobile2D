using UnityEngine;
using System;
using System.Collections;
using Resources.Scripts.Enemy;
using Resources.Scripts.Misc;
using UnityEngine.Rendering.Universal;
using Resources.Scripts.Labyrinth;
using Spine;
using Spine.Unity;
using UObject = UnityEngine.Object;

namespace Resources.Scripts.Player
{
    /// <summary>
    /// Controls player movement, light, animation, dodge roll, evasion and traps.
    /// Uses Spine animation instead of Unity Animator / SpriteRenderer.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        #region Constants
        private const string SlowAnimationName   = "Goes_01_001";
        private const string RunAnimationName    = "Run_02_001";
        private const string JumpAnimationName   = "Jamp_04_001";
        private const string DamageAnimationName = "Damage_01_003";
        private const string DeathAnimationName  = "Death_04";
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
        private float rollDuration;
        #endregion

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
        private float initialDistance = -1f;
        private int darkSkullHitCount;

        private Vector2 lastMoveDirection = Vector2.left;
        private bool isRolling;
        private bool canRoll = true;
        private float rollCooldownRemaining;

        private bool idleCycling;
        private int idleIndex;

        // Сохраняем исходный ScaleX скелета
        private float initialScaleX;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            if (skeletonAnimation == null)
                Debug.LogError("PlayerController: SkeletonAnimation не назначен", this);

            initialScaleX = skeletonAnimation.Skeleton.ScaleX;
            skeletonAnimation.state.Complete += HandleAnimationComplete;

            var anim = skeletonAnimation.Skeleton.Data.FindAnimation(JumpAnimationName);
            // Длительность ролла берётся из анимации прыжка (кувырка)
            rollDuration = anim != null ? anim.Duration : 0.3f;
        }

        private void OnDestroy()
        {
            if (skeletonAnimation != null)
                skeletonAnimation.state.Complete -= HandleAnimationComplete;
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

            if (!isRolling) UpdateMovement();
            UpdateLightOuterRange();
            TickRollCooldown();

            if (Input.GetKeyDown(KeyCode.LeftShift)) TryRoll();
            if (!isRolling && Input.GetKeyDown(KeyCode.Space))
                PlayAnimation(JumpAnimationName, false);
        }
        #endregion

        #region Movement Methods
        private void UpdateMovement()
        {
            if (LabyrinthMapController.Instance?.IsMapActive == true || Input.GetKey(KeyCode.Space))
                return;

            float h = joystick != null ? joystick.Horizontal : Input.GetAxis("Horizontal");
            float v = joystick != null ? joystick.Vertical   : Input.GetAxis("Vertical");
            Vector2 dir = new Vector2(h, v);

            if (dir.magnitude > IdleThreshold)
            {
                idleCycling = false;
                lastMoveDirection = dir.normalized;

                PlayAnimation(
                    dir.magnitude < SlowThreshold ? SlowAnimationName : RunAnimationName,
                    true
                );

                if (lastMoveDirection.x != 0f)
                {
                    float sign = -Mathf.Sign(lastMoveDirection.x);
                    skeletonAnimation.Skeleton.ScaleX = Mathf.Abs(initialScaleX) * sign;
                }
            }
            else if (!idleCycling)
            {
                PlayIdleSequence();
            }

            float spd = playerStats.GetTotalMoveSpeed() * currentSlowMultiplier;
            transform.Translate(dir * (spd * Time.deltaTime), Space.World);
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

            // Проигрываем анимацию кувырка
            skeletonAnimation.state.SetAnimation(0, JumpAnimationName, false);

            // Вычисляем эффективную скорость: стандартная скорость ролла умножается на множитель
            float baseSpeed = rollDistance / rollDuration;
            float effectiveRollSpeed = baseSpeed * rollSpeedMultiplier;

            for (float t = 0f; t < rollDuration; t += Time.deltaTime)
            {
                transform.Translate(lastMoveDirection * (effectiveRollSpeed * Time.deltaTime), Space.World);
                yield return null;
            }

            transform.rotation = Quaternion.identity;
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
            if (finishPoint == null || playerLight == null || initialDistance <= 0f) return;
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

        /// <summary>
        /// Вызывается врагом, когда должен проигрываться только эффект "получения удара" без урона.
        /// </summary>
        public void PlayDamageAnimation()
        {
            if (IsDead) return;
            var entry = skeletonAnimation.state.SetAnimation(0, DamageAnimationName, false);
            skeletonAnimation.state.AddAnimation(0, IdleAnimations[0], true, entry.Animation.Duration);
        }

        public void TakeDamage(EnemyController enemy)
        {
            if (isImmortal || isRolling || IsDead || playerStats.TryEvade(transform.position)) return;

            playerStats.Health -= enemy.GetComponent<EnemyStatsHandler>().Damage;
            if (playerStats.Health <= 0f)
            {
                Die();
                return;
            }

            PlayDamageAnimation();

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

        public void ApplyBinding(float duration) =>
            StartCoroutine(BindingCoroutine(duration));

        private IEnumerator BindingCoroutine(float duration)
        {
            float orig = currentSlowMultiplier;
            currentSlowMultiplier = 0f;
            yield return new WaitForSeconds(duration);
            currentSlowMultiplier = orig;
        }

        public void Stun(float duration) =>
            StartCoroutine(StunCoroutine(duration));

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
            enabled = false;

            skeletonAnimation.state.Complete -= HandleAnimationComplete;
            skeletonAnimation.state.ClearTracks();

            var entry = skeletonAnimation.state.SetAnimation(0, DeathAnimationName, false);
            entry.Complete += trackEntry =>
            {
                if (trackEntry.Animation.Name == DeathAnimationName)
                {
                    foreach (var canvas in UObject.FindObjectsByType<Canvas>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        canvas.gameObject.SetActive(false);
                    }
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
                return;
            }
            if (entry.Animation.Name == JumpAnimationName)
                PlayIdleSequence();
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
