using UnityEngine;

namespace Resources.Scripts.Fairy
{
    /// <summary>
    /// Управляет перемещением феи и притягиванием к игроку.
    /// Во время PullTo обновляет в шейдере параметры _AtrPos и _Progress для эффекта dissolve.
    /// </summary>
    [DisallowMultipleComponent]
    public class FairyController : MonoBehaviour
    {
        [Header("Параметры движения")]
        [SerializeField, Range(1, 10), Tooltip("Максимальный радиус перемещения от исходной позиции.")]
        private int maxMoveRadius = 2;

        [SerializeField, Range(1, 20), Tooltip("Скорость перемещения феи.")]
        private int speed = 5;

        [SerializeField, Tooltip("Сглаживание интерполяции движения.")]
        private float moveSmoothing = 0.1f;

        [Header("Параметры рандомизации")]
        [SerializeField, Tooltip("Минимальный множитель случайного смещения.")]
        private float minOffsetMultiplier = 1f;

        [SerializeField, Tooltip("Максимальный множитель случайного смещения.")]
        private float maxOffsetMultiplier = 3f;

        [Header("Отладка")]
        [SerializeField, Tooltip("Выводить в консоль цели движения.")]
        private bool debugLog;

        // Ссылка на SpriteRenderer и материал (инстанс)
        private SpriteRenderer spriteRenderer;
        private Material dissolveMat;

        // Pull-логика
        private bool isBeingPulled;
        private Vector3 pullTarget;
        private float pullSpeed;
        private float initialPullDistance;
        [Tooltip("Минимальное расстояние для поглощения")]
        public float absorbDistance = 0.2f;

        // Патруль
        private Vector3 originPosition;
        private Vector3 targetPosition;

        // Кэш ID-шников для шейдера
        private static readonly int ProgressID = Shader.PropertyToID("_Progress");
        private static readonly int AtrPosID   = Shader.PropertyToID("_AtrPos");

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning("SpriteRenderer не найден.", this);
                return;
            }

            // Создаем и сразу инициализируем инстанс материала
            dissolveMat = Instantiate(spriteRenderer.sharedMaterial);
            spriteRenderer.material = dissolveMat;
            // Дефолтное состояние: без dissolve, центр шейдера по середине спрайта
            dissolveMat.SetFloat(ProgressID, 0f);
            dissolveMat.SetVector(AtrPosID, new Vector4(0.5f, 0.5f, 0f, 0f));

            originPosition = transform.position;
            targetPosition = originPosition;
        }

        private void Update()
        {
            if (isBeingPulled)
            {
                // Рассчитываем прогресс эффекта dissolve
                float currentDist = Vector3.Distance(transform.position, pullTarget);
                float t = 1f - Mathf.Clamp01((currentDist - absorbDistance) / (initialPullDistance - absorbDistance));
                dissolveMat.SetFloat(ProgressID, t);

                // Фиксируем ArtPos в центр (0.5, 0.5)
                dissolveMat.SetVector(AtrPosID, new Vector4(0.5f, 0.5f, 0f, 0f));

                // Физика притягивания
                Vector3 dir = (pullTarget - transform.position).normalized;
                transform.position += dir * pullSpeed * Time.deltaTime;
                if (dir.x != 0f) spriteRenderer.flipX = dir.x > 0f;
                return;
            }

            // Стандартный патруль
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                UpdateTargetPosition();

            float interp = moveSmoothing * speed * Time.deltaTime;
            Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, interp);
            Vector3 moveDir = newPos - transform.position;
            transform.position = newPos;
            if (moveDir.x != 0f) spriteRenderer.flipX = moveDir.x > 0f;
        }

        /// <summary>
        /// Инициализация исходной позиции при создании.
        /// </summary>
        public void Init(Vector3 initialPosition)
        {
            originPosition = initialPosition;
            targetPosition = initialPosition;
        }

        /// <summary>
        /// Запускает притягивание к точке target со скоростью speed.
        /// </summary>
        public void PullTo(Vector3 target, float speed)
        {
            if (!isBeingPulled)
            {
                pullTarget = target;
                pullSpeed = speed;
                initialPullDistance = Vector3.Distance(transform.position, pullTarget);
                isBeingPulled = true;
            }
        }

        /// <summary>
        /// Выбирает новую случайную цель для патруля.
        /// </summary>
        private void UpdateTargetPosition()
        {
            Vector2 rndDir = Random.insideUnitCircle.normalized;
            float distFactor = Random.Range(minOffsetMultiplier, maxOffsetMultiplier) * maxMoveRadius;
            Vector3 rndOffset = new Vector3(rndDir.x, rndDir.y, 0f) * distFactor;
            targetPosition = originPosition + rndOffset;
            if (debugLog) Debug.Log($"Новая цель: {targetPosition}", this);
        }

        /// <summary>
        /// Уничтожает фею при поглощении.
        /// </summary>
        public void DestroyFairy()
        {
            Destroy(gameObject);
        }
    }
}
