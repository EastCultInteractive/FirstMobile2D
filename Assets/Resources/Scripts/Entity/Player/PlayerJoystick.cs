using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using Resources.Scripts.Labyrinth;

namespace Resources.Scripts.Entity.Player
{
    /// <summary>
    /// Implements a fully “flexible” virtual joystick for mobile/touch input.
    /// Джойстик появляется там, куда игрок коснулся в любом месте экрана,
    /// и остаётся на экране, пока палец не будет отпущен.
    /// </summary>
    public class PlayerJoystick : MonoBehaviour
    {
        [Header("Joystick UI")]
        [SerializeField, Tooltip("Фон (background) джойстика (RectTransform).")]
        private RectTransform background;
        [SerializeField, Tooltip("Ручка (handle) джойстика (RectTransform).")]
        private RectTransform handle;

        [Header("Joystick Handle Settings")]
        [SerializeField, Tooltip("Максимальное расстояние движения ручки (handle) от центра фона.")]
        private float handleRange = 50f;

        // Текущий вектор ввода (нормализованный), описывает направление движения.
        private Vector2 inputVector = Vector2.zero;

        // CanvasGroup фона джойстика, чтобы фон можно было скрывать (alpha = 0),
        // но при этом он продолжал блокировать Raycast (blocksRaycasts = true).
        private CanvasGroup backgroundCanvasGroup;

        // RectTransform всего Canvas, чтобы правильно конвертировать экранные координаты.
        private RectTransform canvasRect;

        // Ссылка на родительский Canvas (чтобы передавать корректную камеру при конвертации координат).
        private Canvas parentCanvas;

        // GraphicRaycaster для проверки нажатий по UI (например, кнопкам).
        private GraphicRaycaster graphicRaycaster;

        // Буфер для результатов Raycast при проверке UI.
        private PointerEventData pointerEventData;
        private List<RaycastResult> raycastResults = new List<RaycastResult>();

        // Идентификатор пальца (fingerId), который управляет джойстиком. -1 значит «никто не управляет».
        private int joystickTouchId = -1;

        private void Awake()
        {
            // 1) Находим ближайший Canvas в родителях и сохраняем его RectTransform.
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError("PlayerJoystick: не удалось найти Canvas в родителях! Убедитесь, что этот скрипт находится под Canvas.");
                return;
            }
            canvasRect = parentCanvas.GetComponent<RectTransform>();

            // 2) Репарентим background (фон джойстика) прямо под Canvas,
            //    чтобы он всегда рисовался в пространстве UI, а не “улетал” в world space.
            if (background == null)
            {
                Debug.LogError("PlayerJoystick: поле background не заполнено в инспекторе!");
                return;
            }
            background.SetParent(canvasRect, false);

            // Устанавливаем масштаб фона (0.5, 0.5, 0.5).
            background.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // 3) Убедимся, что handle — ребёнок background. Если нет, делаем его таковым.
            if (handle == null)
            {
                Debug.LogError("PlayerJoystick: поле handle не заполнено в инспекторе!");
                return;
            }
            if (handle.parent != background)
            {
                handle.SetParent(background, false);
            }

            // Устанавливаем локальный масштаб handle в (1, 1, 1).
            handle.localScale = Vector3.one;

            // 4) Устанавливаем CanvasGroup на background, чтобы управлять видимостью фона.
            backgroundCanvasGroup = background.GetComponent<CanvasGroup>();
            if (backgroundCanvasGroup == null)
            {
                backgroundCanvasGroup = background.gameObject.AddComponent<CanvasGroup>();
            }
            // Фон невидим (alpha = 0), но он всё ещё блокирует Raycast (blocksRaycasts = true).
            backgroundCanvasGroup.alpha = 0f;
            backgroundCanvasGroup.blocksRaycasts = true;

            // 5) Скрываем handle, поскольку джойстик неактивен до первого касания.
            handle.gameObject.SetActive(false);

            // 6) Получаем GraphicRaycaster из parentCanvas для проверки нажатий по UI.
            graphicRaycaster = parentCanvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                Debug.LogError("PlayerJoystick: не удалось найти GraphicRaycaster на Canvas! Убедитесь, что на Canvas есть GraphicRaycaster.");
            }
            // Инициализируем PointerEventData с текущей EventSystem.
            pointerEventData = new PointerEventData(EventSystem.current);
        }

        private void Update()
        {
            // Если джойстик сейчас никому не “привязан” (joystickTouchId == -1),
            // ищем новое касание (TouchPhase.Began) в любой точке экрана.
            if (joystickTouchId == -1)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        // Если мини-карта активна, жмём игнорировать ввод джойстика.
                        if (LabyrinthMapController.Instance != null && LabyrinthMapController.Instance.IsMapActive)
                            continue;

                        // Если касание по UI-кнопке — даём управление кнопке и не активируем джойстик.
                        if (IsTouchOnButton(touch.position))
                            continue;

                        // “Привязываем” этот палец к джойстику:
                        joystickTouchId = touch.fingerId;
                        ActivateJoystick(touch.position);
                        break; // больше не ищем — палец уже занят джойстиком
                    }
                }
            }
            else
            {
                // Если уже есть палец, управляющий джойстиком (joystickTouchId != -1),
                // ищем его в текущем списке Input.touches и обрабатываем.
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.touches[i];
                    if (touch.fingerId == joystickTouchId)
                    {
                        // Если мини-карта сейчас активна, просто игнорируем движение джойстика:
                        if (LabyrinthMapController.Instance != null && LabyrinthMapController.Instance.IsMapActive)
                            return;

                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            // Обновляем позицию ручки
                            HandleDrag(touch.position);
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            // Пользователь отпустил палец — закрываем джойстик
                            ReleaseJoystick();
                            joystickTouchId = -1;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, было ли касание по UI-элементу типа Button.
        /// </summary>
        private bool IsTouchOnButton(Vector2 screenPosition)
        {
            if (graphicRaycaster == null || EventSystem.current == null)
                return false;

            // Подготовка PointerEventData с позицией касания:
            pointerEventData.position = screenPosition;

            // Выполняем Raycast в UI:
            raycastResults.Clear();
            graphicRaycaster.Raycast(pointerEventData, raycastResults);

            // Проверяем все результаты: если среди них есть Button — возврат true.
            foreach (RaycastResult result in raycastResults)
            {
                if (result.gameObject.GetComponent<Button>() != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Активирует джойстик: делает фон видимым, показывает ручку
        /// и позиционирует фон туда, куда коснулся игрок (screenPosition).
        /// </summary>
        private void ActivateJoystick(Vector2 screenPosition)
        {
            // 1) Делаем фон видимым:
            backgroundCanvasGroup.alpha = 1f;
            // 2) Показываем ручку (handle) и сбрасываем её в центр фона:
            handle.gameObject.SetActive(true);
            handle.anchoredPosition = Vector2.zero;

            // 3) Конвертируем screenPosition (в пикселях экрана) в локальную координату внутри canvasRect:
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                         ? null
                         : parentCanvas.worldCamera;

            if (canvasRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    cam,
                    out Vector2 localPointInCanvas))
            {
                // Ставим фон (background) в эту локальную позицию:
                background.anchoredPosition = localPointInCanvas;
            }

            // 4) Чтобы джойстик рисовался поверх остальных UI-элементов,
            //    делаем его последним в иерархии рендеринга:
            background.SetAsLastSibling();
        }

        /// <summary>
        /// Обрабатывает движение пальца внутри фона джойстика.
        /// Переводит screenPosition → локальную координату относительно background,
        /// потом нормализует вектор и анимирует handle.
        /// </summary>
        private void HandleDrag(Vector2 screenPosition)
        {
            // Опять-таки, используем ту же логику с камерой:
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                         ? null
                         : parentCanvas.worldCamera;

            // Переводим экранную точку в локальную относительно фона (background):
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    screenPosition,
                    cam,
                    out Vector2 localPos))
            {
                // Считаем вектор (от -1 до +1 по X и Y), нормализуем, если длина > 1:
                inputVector = new Vector2(
                    localPos.x / (background.sizeDelta.x * 0.5f),
                    localPos.y / (background.sizeDelta.y * 0.5f)
                );
                if (inputVector.sqrMagnitude > 1f)
                    inputVector.Normalize();

                // Вычисляем целевую позицию для ручки:
                Vector2 targetPos = inputVector * handleRange;

                // Плавно двигаем ручку к targetPos:
                handle
                    .DOAnchorPos(targetPos, 0.05f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// Срабатывает, когда палец отпущен (TouchPhase.Ended или Canceled).
        /// Плавно возвращает ручку в центр, а затем скрывает фон и ручку.
        /// </summary>
        private void ReleaseJoystick()
        {
            inputVector = Vector2.zero;

            handle
                .DOAnchorPos(Vector2.zero, 0.15f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // После анимации делаем фон прозрачным и скрываем ручку:
                    backgroundCanvasGroup.alpha = 0f;
                    handle.gameObject.SetActive(false);
                });
        }

        /// <summary>
        /// Горизонтальная составляющая ввода джойстика (от –1 до +1).
        /// </summary>
        public float Horizontal => inputVector.x;

        /// <summary>
        /// Вертикальная составляющая ввода джойстика (от –1 до +1).
        /// </summary>
        public float Vertical => inputVector.y;
    }
}
