using System.Collections;
using Resources.Scripts.Data;
using UnityEngine;
using UnityEngine.UI;
using Resources.Scripts.GameManagers;

namespace Resources.Scripts.Labyrinth
{
    /// <summary>
    /// Manages the display of minimap icons: start, finish and dynamic player icon.
    /// The prefabs must be UI Images and should be children of the RawImage displaying the minimap.
    /// </summary>
    public class LabyrinthMinimapIconController : MonoBehaviour
    {
        [Header("Icon Prefabs (UI Image)")]
        [SerializeField, Tooltip("Prefab for the start icon.")]
        private GameObject startIconPrefab;
        [SerializeField, Tooltip("Prefab for the finish icon.")]
        private GameObject finishIconPrefab;
        [SerializeField, Tooltip("Prefab for the dynamic player icon.")]
        private GameObject playerIconPrefab;

        [Header("Minimap Settings")]
        [SerializeField, Tooltip("Camera used exclusively for the minimap.")]
        private Camera minimapCamera;
        [SerializeField, Tooltip("RawImage that displays the minimap.")]
        private RawImage minimapImage;
        [SerializeField, Tooltip("Maximum time (in seconds) to wait for start and finish cells to appear.")]
        private float maxIconWaitTime = 5f;

        [Header("Labyrinth Settings")]
        [SerializeField, Tooltip("Labyrinth settings that include manual camera transform parameters.")]
        private LabyrinthSettings labyrinthSettings;

        private RectTransform rawImageRectTransform;
        private GameObject startIconInstance;
        private GameObject finishIconInstance;
        private GameObject playerIconInstance;
        private Transform playerTransform;

        private void Start()
        {
            // 1) Получаем RectTransform из RawImage миникарты.
            if (minimapImage != null)
            {
                rawImageRectTransform = minimapImage.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogWarning("Minimap RawImage is not assigned!");
                return;
            }

            // 2) Проверяем назначение камеры миникарты.
            if (minimapCamera == null)
            {
                Debug.LogWarning("Minimap Camera is not assigned!");
                return;
            }

            // 3) Теперь сперва пытаемся взять настройки из текущего StageData (SO), если он есть.
            bool appliedFromSO = false;  // Флаг, что настройки уже применены из StageData
            var stageData = GameStageManager.currentStageData;
            if (stageData != null && stageData.labyrinthSettings != null)
            {
                var s = stageData.labyrinthSettings;
                minimapCamera.transform.position  = s.cameraPosition;
                minimapCamera.transform.eulerAngles = s.cameraRotation;
                minimapCamera.orthographicSize    = s.cameraSize;
                appliedFromSO = true;  // Из SO взяли параметры
            }

            // 4) Если в SO ничего нет, а в инспекторе заполнили labyrinthSettings — используем его.
            if (!appliedFromSO)
            {
                if (labyrinthSettings != null)
                {
                    minimapCamera.transform.position  = labyrinthSettings.cameraPosition;
                    minimapCamera.transform.eulerAngles = labyrinthSettings.cameraRotation;
                    minimapCamera.orthographicSize    = labyrinthSettings.cameraSize;
                }
                else
                {
                    Debug.LogWarning("LabyrinthSettings not assigned to LabyrinthMinimapIconController " +
                                     "and StageData.labyrinthSettings is null!");
                }
            }

            // 5) Ищем объект игрока по тегу.
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Player object not found!");
            }

            // 6) Запускаем корутину ожидания появления ячеек с тегами "Start" и "Finish".
            StartCoroutine(InitializeIconsCoroutine());
        }

        /// <summary>
        /// Coroutine that waits for objects with tags "Start" and "Finish" to appear, then instantiates their icons.
        /// </summary>
        private IEnumerator InitializeIconsCoroutine()
        {
            float timer = 0f;
            GameObject startCell = null;
            GameObject finishCell = null;
            // Ожидаем до maxIconWaitTime секунд появления объектов с тегами "Start" и "Finish".
            while (timer < maxIconWaitTime)
            {
                startCell = GameObject.FindGameObjectWithTag("Start");
                finishCell = GameObject.FindGameObjectWithTag("Finish");
                if (startCell != null && finishCell != null)
                    break;
                timer += Time.deltaTime;
                yield return null;
            }

            if (startCell == null)
            {
                Debug.LogWarning("Start cell not found within " + maxIconWaitTime + " seconds!");
            }
            else if (startIconPrefab != null)
            {
                Vector2 uiPos = WorldToUISpace(startCell.transform.position);
                // Инстанциируем иконку старта с worldPositionStays = false, чтобы сразу настроить локальную позицию.
                startIconInstance = Instantiate(startIconPrefab, rawImageRectTransform, false);
                RectTransform startRect = startIconInstance.GetComponent<RectTransform>();
                if (startRect == null)
                {
                    startRect = startIconInstance.AddComponent<RectTransform>();
                }
                startRect.anchoredPosition = uiPos;
            }

            if (finishCell == null)
            {
                Debug.LogWarning("Finish cell not found within " + maxIconWaitTime + " seconds!");
            }
            else if (finishIconPrefab != null)
            {
                Vector2 uiPos = WorldToUISpace(finishCell.transform.position);
                // Инстанциируем иконку финиша с worldPositionStays = false, чтобы сразу настроить локальную позицию.
                finishIconInstance = Instantiate(finishIconPrefab, rawImageRectTransform, false);
                RectTransform finishRect = finishIconInstance.GetComponent<RectTransform>();
                if (finishRect == null)
                {
                    finishRect = finishIconInstance.AddComponent<RectTransform>();
                }
                finishRect.anchoredPosition = uiPos;
            }

            // Создаем динамическую иконку игрока, если объект игрока найден.
            if (playerTransform != null && playerIconPrefab != null)
            {
                Vector2 uiPos = WorldToUISpace(playerTransform.position);
                // Инстанциируем иконку игрока с worldPositionStays = false, чтобы сразу настроить локальную позицию.
                playerIconInstance = Instantiate(playerIconPrefab, rawImageRectTransform, false);
                RectTransform playerRect = playerIconInstance.GetComponent<RectTransform>();
                if (playerRect == null)
                {
                    playerRect = playerIconInstance.AddComponent<RectTransform>();
                }
                playerRect.anchoredPosition = uiPos;
            }
        }

        private void Update()
        {
            // Обновляем позицию иконки игрока относительно RawImage.
            if (playerTransform != null && playerIconInstance != null)
            {
                Vector2 uiPos = WorldToUISpace(playerTransform.position);
                RectTransform playerRect = playerIconInstance.GetComponent<RectTransform>();
                if (playerRect != null)
                {
                    playerRect.anchoredPosition = uiPos;
                }
            }
        }

        /// <summary>
        /// Converts world coordinates to UI coordinates relative to the minimap RawImage using the minimap camera.
        /// Учитывает реальный размер и Pivot RectTransform для корректного вычисления локальной позиции.
        /// </summary>
        /// <param name="worldPos">World position</param>
        /// <returns>Local coordinates (anchoredPosition) for the RawImage</returns>
        private Vector2 WorldToUISpace(Vector3 worldPos)
        {
            // Получаем координаты во viewport камеры (значения от 0 до 1).
            Vector3 viewportPos = minimapCamera.WorldToViewportPoint(worldPos);

            // Берём размеры Rect самого RawImage.
            Rect rect = rawImageRectTransform.rect;
            Vector2 pivot = rawImageRectTransform.pivot;

            // Преобразуем нормализованные координаты в локальные, с учётом Pivot.
            float x = (viewportPos.x * rect.width) - (rect.width * pivot.x);
            float y = (viewportPos.y * rect.height) - (rect.height * pivot.y);

            return new Vector2(x, y);
        }
    }
}
