using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Resources.Scripts.Player;
using Resources.Scripts.Labyrinth;

namespace Resources.Scripts.Enemy
{
    public enum EnemyType
    {
        Standard,
        Goblin,
        DarkSkull,
        Troll
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Common Settings")]
        [Tooltip("Имя врага для отладки и логирования.")]
        public string enemyName = "Enemy";

        public EnemyType enemyType = EnemyType.Standard;

        [Header("Movement Settings")]
        [Tooltip("Базовая скорость передвижения врага.")]
        public float speed = 1f;

        [Tooltip("Множитель замедления (при наложении эффекта).")]
        public float slowMultiplier = 1f;

        [Tooltip("Дальность обнаружения игрока.")]
        public float detectionRange = 5f;

        [Header("Patrol Settings")]
        [Tooltip("Радиус патрулирования относительно точки спавна.")]
        public float patrolRadius = 3f;

        [Tooltip("Множитель скорости патрулирования.")]
        public float patrolSpeedMultiplier = 0.5f;

        [Header("Standard Attack Settings")]
        [Tooltip("Дальность атаки стандартного врага.")]
        public float attackRange = 1f;

        [Tooltip("Время между ударами стандартного врага.")]
        public float attackCooldown = 1f;

        [Tooltip("Нужно ли отталкивать игрока при атаке.")]
        public bool pushPlayer = true;

        [Tooltip("Сила отталкивания игрока.")]
        public float pushForceMultiplier = 1f;

        [Header("Goblin Attack Settings")]
        [Tooltip("Время между выстрелами гоблина.")]
        public float goblinAttackCooldownTime = 2f;

        [Tooltip("Длительность эффекта связывания при попадании снаряда.")]
        public float bindingDuration = 0.5f;

        [Tooltip("Скорость полёта снаряда.")]
        public float projectileSpeed = 5f;

        [Tooltip("Время жизни снаряда.")]
        public float goblinProjectileLifeTime = 5f;

        [Tooltip("Угол разброса снаряда.")]
        public float goblinProjectileSpreadAngle;

        [Tooltip("Масштаб снаряда.")]
        public Vector3 goblinProjectileScale = Vector3.one;

        [Tooltip("Урон снаряда гоблина.")]
        public float goblinProjectileDamage;

        [Tooltip("Префаб снаряда гоблина.")]
        public GameObject projectilePrefab;

        [Tooltip("Точка, из которой происходит атака (выстрел).")]
        public Transform attackPoint;

        [Tooltip("Длительность анимации атаки гоблина.")]
        public float goblinAttackAnimationDuration = 1f;

        [Header("DarkSkull Attack Settings")]
        [Tooltip("Время между ударами DarkSkull.")]
        public float darkSkullAttackCooldownTime = 1f;

        [Tooltip("Сила отталкивания игрока при атаке DarkSkull.")]
        public float darkSkullPushForce = 5f;

        [Tooltip("Длительность анимации атаки DarkSkull.")]
        public float darkSkullAttackAnimationDuration = 1f;

        [Header("Troll Attack Settings")]
        [Tooltip("Время между ударами Troll.")]
        public float trollAttackCooldownTime = 1f;

        [Tooltip("Сила отталкивания игрока при атаке Troll.")]
        public float trollPushForce = 5f;

        [Tooltip("Длительность анимации атаки Troll.")]
        public float trollAttackAnimationDuration = 1f;

        [Header("Animation Settings")]
        public Animator animator;

        [Tooltip("Имя анимации состояния ожидания.")]
        public string idleAnimationName = "Idle";

        [Tooltip("Имя анимации бега.")]
        public string walkAnimationName = "Walk";

        [Tooltip("Имя анимации атаки.")]
        public string attackAnimationName = "Attack";

        public SpriteRenderer spriteRenderer;

        [Header("Detection & Obstacles")]
        [Tooltip("Маска слоёв для стен и препятствий.")]
        public LayerMask obstacleMask;

        [Header("Debug Settings")]
        public bool debugLog;

        #endregion

        #region Private Fields

        private Rigidbody2D rb;
        private PlayerController player;
        private PlayerStatsHandler playerStats;
        private bool isAttacking;
        private float lastAttackTime;

        private LabyrinthField labField;
        private List<Vector3> currentPath = new List<Vector3>();
        private int pathIndex;

        // Флаг, что мы в режиме погони
        private bool isChasing;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            var bc = GetComponent<BoxCollider2D>();
            bc.isTrigger = true;
            // игнорируем столкновения между врагами
            int layer = gameObject.layer;
            Physics2D.IgnoreLayerCollision(layer, layer);
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.GetComponent<PlayerController>();
                playerStats = go.GetComponent<PlayerStatsHandler>();
            }

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator != null)
                animator.Play(idleAnimationName);

            labField = LabyrinthGeneratorWithWalls.CurrentField;
            Patrol(); // сразу идём патрулировать
        }

        private void Update()
        {
            if (labField == null || player == null || player.IsDead)
            {
                isChasing = false;
                Patrol();
                return;
            }

            float dist = Vector3.Distance(transform.position, player.transform.position);
            bool inDetection = dist <= detectionRange;
            bool inMeleeRange = (enemyType != EnemyType.Goblin) && dist <= attackRange;
            bool goblinCanShoot = enemyType == EnemyType.Goblin && HasLineOfSight();

            if (inDetection)
            {
                // стартуем погоню
                if (!isChasing)
                {
                    isChasing = true;
                    RecalculatePathToPlayer();
                }

                // если дошли до конца пути — пересчитать
                if (pathIndex >= currentPath.Count)
                {
                    RecalculatePathToPlayer();
                }

                // атака ближнего боя
                if (inMeleeRange && enemyType != EnemyType.Goblin)
                {
                    AttackPlayer();
                    return;
                }
                // гоблин стреляет при LOS
                if (enemyType == EnemyType.Goblin && goblinCanShoot)
                {
                    AttackPlayer();
                    return;
                }

                // двигаемся по пути
                FollowPath();
            }
            else
            {
                // выходим из погони
                if (isChasing)
                {
                    isChasing = false;
                    Patrol();
                }
                Patrol();
            }
        }

        #endregion

        #region Patrol & Chase

        private void Patrol()
        {
            if (currentPath == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                // запускаем новый случайный маршрут
                var randomTarget = new Vector2Int(
                    Random.Range(0, labField.Rows),
                    Random.Range(0, labField.Cols)
                );
                BuildPath(WorldToCell(transform.position), randomTarget);
            }
            FollowPath();
        }

        private void RecalculatePathToPlayer()
        {
            BuildPath(WorldToCell(transform.position),
                      WorldToCell(player.transform.position));
        }

        // общий метод построения пути
        private void BuildPath(Vector2Int fromCell, Vector2Int toCell)
        {
            var cells = labField.FindPath(fromCell, toCell);
            currentPath = labField.PathToWorld(cells);
            pathIndex = 0;
            PlayWalkAnim();
        }

        private void FollowPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
                return;

            Vector3 goal = currentPath[pathIndex];
            TurnToTarget(goal);
            transform.position = Vector3.MoveTowards(
                transform.position,
                goal,
                speed * Time.deltaTime
            );
            if (Vector3.Distance(transform.position, goal) < 0.05f)
                pathIndex++;
        }

        private Vector2Int WorldToCell(Vector3 worldPos)
        {
            int col = Mathf.RoundToInt(worldPos.x / labField.CellSizeX);
            int row = Mathf.RoundToInt(-worldPos.y / labField.CellSizeY);
            return new Vector2Int(row, col);
        }

        private void TurnToTarget(Vector3 target)
        {
            float dx = target.x - transform.position.x;
            transform.eulerAngles = dx < 0
                ? Vector3.zero
                : new Vector3(0, 180, 0);
        }

        private void PlayWalkAnim()
        {
            if (animator == null) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(walkAnimationName))
                animator.Play(walkAnimationName);
        }

        #endregion

        #region Attack

        private void AttackPlayer()
        {
            if (isAttacking) return;
            float since = Time.time - lastAttackTime;

            switch (enemyType)
            {
                case EnemyType.Goblin:
                    if (since < goblinAttackCooldownTime) return;
                    StartCoroutine(PerformGoblinAttack());
                    break;
                case EnemyType.DarkSkull:
                    if (since < darkSkullAttackCooldownTime) return;
                    StartCoroutine(PerformDarkSkullAttack());
                    break;
                case EnemyType.Troll:
                    if (since < trollAttackCooldownTime) return;
                    StartCoroutine(PerformTrollAttack());
                    break;
                default:
                    if (since < attackCooldown) return;
                    StartCoroutine(PerformStandardAttack());
                    break;
            }
        }

        private IEnumerator PerformStandardAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed;
            speed = 0f;
            animator?.Play(attackAnimationName);

            yield return new WaitForSeconds(attackCooldown);

            if (!player.IsDead)
            {
                player.TakeDamage(this);
                if (pushPlayer)
                {
                    var dir = (player.transform.position - transform.position).normalized;
                    player.transform.position += dir * pushForceMultiplier;
                }
            }

            speed = oldSpeed;
            isAttacking = false;
        }

        private IEnumerator PerformGoblinAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed;
            speed = 0f;
            animator?.Play(attackAnimationName);

            yield return new WaitForSeconds(goblinAttackAnimationDuration);

            if (HasLineOfSight())
                SpawnProjectileEvent();

            speed = oldSpeed;
            isAttacking = false;
        }

        private IEnumerator PerformDarkSkullAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed;
            speed = 0f;
            animator?.Play(attackAnimationName);

            yield return new WaitForSeconds(darkSkullAttackAnimationDuration);

            RegisterDarkSkullHitEvent();

            speed = oldSpeed;
            isAttacking = false;
        }

        private IEnumerator PerformTrollAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            float oldSpeed = speed;
            speed = 0f;
            animator?.Play(attackAnimationName);

            yield return new WaitForSeconds(trollAttackAnimationDuration);

            RegisterTrollHitEvent();

            speed = oldSpeed;
            isAttacking = false;
        }

        #endregion

        #region Animation Events & Effects

        public void SpawnProjectileEvent()
        {
            if (player.IsDead) return;
            var origin = attackPoint != null ? attackPoint.position : transform.position;
            var dir = (player.transform.position - origin).normalized;
            if (goblinProjectileSpreadAngle > 0f)
            {
                float a = Random.Range(-goblinProjectileSpreadAngle, goblinProjectileSpreadAngle);
                dir = Quaternion.Euler(0, 0, a) * dir;
            }

            if (projectilePrefab != null)
            {
                var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
                proj.transform.localScale = goblinProjectileScale;
                if (proj.TryGetComponent<GoblinProjectile>(out var gp))
                    gp.SetParameters(dir, projectileSpeed, bindingDuration, goblinProjectileLifeTime, goblinProjectileDamage);
                else if (proj.TryGetComponent<Rigidbody2D>(out var prb))
                    prb.AddForce(dir * projectileSpeed, ForceMode2D.Impulse);
            }
            else
            {
                Debug.LogWarning($"[{enemyName}] Projectile prefab not set");
            }
        }

        public void RegisterDarkSkullHitEvent()
        {
            if (playerStats.TryEvade(transform.position)) return;
            player.ReceiveDarkSkullHit();
            if (pushPlayer)
            {
                var dir = (player.transform.position - transform.position).normalized;
                player.transform.position += dir * darkSkullPushForce;
            }
        }

        public void RegisterTrollHitEvent()
        {
            if (playerStats.TryEvade(transform.position)) return;
            player.ReceiveTrollHit();
            if (pushPlayer)
            {
                var dir = (player.transform.position - transform.position).normalized;
                player.transform.position += dir * trollPushForce;
            }
        }

        public void ApplySlow(float factor, float duration)
        {
            StartCoroutine(SlowEffect(factor, duration));
        }

        private IEnumerator SlowEffect(float factor, float duration)
        {
            float old = speed;
            speed = old * slowMultiplier;
            yield return new WaitForSeconds(duration);
            speed = old;
        }

        public void ApplyPush(Vector2 force)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        #endregion

        #region Line of Sight

        private bool HasLineOfSight()
        {
            if (attackPoint == null || player == null) return false;
            var origin = attackPoint.position;
            var dir = (player.transform.position - origin).normalized;
            float dist = Vector3.Distance(origin, player.transform.position);
            var hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
            return hit.collider == null;
        }

        #endregion
    }
}
