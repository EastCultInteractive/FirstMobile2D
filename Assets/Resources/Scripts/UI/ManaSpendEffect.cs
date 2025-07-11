using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Resources.Scripts.Entity.UI
{
    /// <summary>
    /// Эффект вылетающего текста затрат маны, который летит к краю заполненной полоски маны.
    /// Добавлена пульсация при спавне.
    /// </summary>
    public class ManaSpendEffect : MonoBehaviour
    {
        [Header("Настройте в инспекторе")]
        [SerializeField] private TMP_Text manaText;      // assign in prefab
        [SerializeField] private float speed = 5f;
        [Tooltip("Distance (in UI units) at which effect is considered 'hit' and destroyed.")]
        [SerializeField] private float destroyDistance = 5f;

        private RectTransform rectTransform;
        private RectTransform canvasRect;
        private Vector2 targetAnchoredPos;
        private Camera uiCamera;
        private System.Action onComplete;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                Debug.LogError("[ManaSpendEffect] Нет RectTransform на этом объекте!");

            if (manaText == null)
            {
                manaText = GetComponentInChildren<TMP_Text>();
                if (manaText == null)
                    Debug.LogError("[ManaSpendEffect] manaText не назначен, и дочернего TMP_Text не найдено!");
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ManaSpendEffect] Не удалось найти Canvas среди родителей!");
            }
            else
            {
                canvasRect = canvas.GetComponent<RectTransform>();
                uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);
            }
        }

        /// <summary>
        /// amount — mana spent;
        /// worldPosition — игрока в world space;
        /// targetUI — RectTransform полоски маны (Filled Image Horizontal Left);
        /// onDone — колбэк по достижении цели.
        /// </summary>
        public void Initialize(int amount, Vector3 worldPosition, RectTransform targetUI, System.Action onDone = null)
        {
            onComplete = onDone;

            if (manaText != null)
                manaText.text = $"-{amount}";
            else
                Debug.LogWarning("[ManaSpendEffect] manaText равен null, текст не установлен!");

            if (canvasRect == null || rectTransform == null)
            {
                Debug.LogError("[ManaSpendEffect] Отсутствует CanvasRect или RectTransform, позиционирование отменено.");
                return;
            }

            // Пульс при спавне
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // 1) Стартовая позиция: из worldPosition игрока в локальные UI-координаты
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 startLocal
            );
            rectTransform.anchoredPosition = startLocal;

            // 2) Целевая позиция: к текущему правому краю заполнения полоски
            if (targetUI != null)
            {
                var img = targetUI.GetComponent<Image>();
                if (img != null && img.type == Image.Type.Filled)
                {
                    float f = img.fillAmount;

                    // Мировые углы RectTransform полоски
                    Vector3[] corners = new Vector3[4];
                    targetUI.GetWorldCorners(corners);
                    Vector3 bottomLeft = corners[0];
                    Vector3 bottomRight = corners[3];
                    Vector3 topLeft = corners[1];

                    float width  = bottomRight.x - bottomLeft.x;
                    float height = topLeft.y - bottomLeft.y;

                    // Точка в мире на fill-краю
                    Vector3 fillWorldPos = new Vector3(
                        bottomLeft.x + width * f,
                        bottomLeft.y + height * 0.5f,
                        bottomLeft.z
                    );

                    // Конвертация world -> screen -> локальные координаты канваса
                    Vector2 fillScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, fillWorldPos);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, fillScreenPos, uiCamera, out targetAnchoredPos
                    );
                }
                else
                {
                    // fallback на центр targetUI
                    Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, targetUI.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, centerScreen, uiCamera, out targetAnchoredPos
                    );
                }
            }
            else
            {
                Debug.LogError("[ManaSpendEffect] targetUI равен null!");
            }
        }

        void Update()
        {
            if (rectTransform == null) return;

            // Летим к цели
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetAnchoredPos,
                Time.deltaTime * speed
            );

            // Уничтожение по достижении
            if (Vector2.Distance(rectTransform.anchoredPosition, targetAnchoredPos) <= destroyDistance)
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
