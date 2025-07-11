using DG.Tweening;
using Resources.Scripts.Data;
using Resources.Scripts.Entity.Tilemap;
using UnityEngine;
using UnityEngine.UI;

namespace Resources.Scripts.GameManagers
{
    public class ArenaManager : MonoBehaviour
    {
        [Header("Default Settings (fallback if no stage is selected)")]
        [SerializeField] private ArenaSettings defaultArenaSettings = null!;

        [Header("UI Timer")]
        [SerializeField] private Text timerText = null!;
        [SerializeField] private RectTransform clockHand = null!;

        [Header("Spawn Parameters")]
        [Tooltip("Half size of the spawn area (e.g., 50 means range from -50 to 50)")]
        [SerializeField] private float spawnArea = 50f;

        [Header("Game Over UI")]
        [Tooltip("Префаб панели Game Over с Legacy-текстом и двумя кнопками (RestartButton, ExitButton)")]
        [SerializeField] private GameObject gameOverPanelPrefab = null!;

        private ArenaSettings _currentSettings;
        private float _timer;
        private bool _playerSurvived;
        private Transform _edgeTreesParent;

        private void Start()
        {
            // 1) Берём настройки арены (либо через StageProgressionManager, либо дефолт)
            _currentSettings = StageProgressionManager.CurrentArenaSettings
                              ?? defaultArenaSettings;

            // 2) Инициализируем Tilemap на сцене (ищем существующий объект с RandomTilemapGenerator)
            InitializeTilemap();

            // 3) Настраиваем таймер и UI
            _timer = _currentSettings.survivalTime;
            if (clockHand != null)
            {
                clockHand.localRotation = Quaternion.Euler(0f, 0f, -90f);
                clockHand
                    .DOLocalRotate(new Vector3(0f, 0f, -90f), 0f)
                    .SetId(this);
            }

            if (timerText != null)
            {
                var cg = timerText.GetComponent<CanvasGroup>()
                         ?? timerText.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg
                  .DOFade(1f, 0.5f)
                  .SetEase(Ease.InQuad)
                  .SetId(this);
            }

            InitializeArena();
        }

        /// <summary>
        /// Находит RandomTilemapGenerator на сцене, передаёт ему tilesForThisArena и размеры, а затем вызывает генерацию.
        /// </summary>
        private void InitializeTilemap()
        {
            // 1) Если в настройках вообще нет массива tilesForThisArena, ничего не делаем
            if (_currentSettings.tilesForThisArena == null || _currentSettings.tilesForThisArena.Length == 0)
            {
                Debug.LogWarning("ArenaManager: tilesForThisArena не назначен или пуст!");
                return;
            }

            // 2) Ищем компонент RandomTilemapGenerator где-то в иерархии сцены "Arena"
            var generator = FindFirstObjectByType<RandomTilemapGenerator>();
            if (generator == null)
            {
                Debug.LogError("ArenaManager: на сцене не найден RandomTilemapGenerator!");
                return;
            }

            // 3) Назначаем массив тайлов (floorTiles) и размеры (width, height)
            generator.floorTiles = _currentSettings.tilesForThisArena;
            generator.width      = Mathf.Max(1, _currentSettings.tilemapWidth);
            generator.height     = Mathf.Max(1, _currentSettings.tilemapHeight);

            // 4) Ищем компонент Tilemap на том же GameObject или в его детях
            var tilemapComp = generator.tilemapComponent
                              ?? generator.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();

            if (tilemapComp == null)
            {
                Debug.LogError("ArenaManager: RandomTilemapGenerator.tilemapComponent не назначен и не найден в children!");
                return;
            }

            generator.tilemapComponent = tilemapComp;

            // 5) Вызываем генерацию
            generator.GenerateRandomMap();
        }

        private void InitializeArena()
        {
        }


        private void Update()
        {
            if (_playerSurvived) return;

            _timer -= Time.deltaTime;
            UpdateTimerUI();

            if (_timer <= 0f)
            {
                _playerSurvived = true;
                StageProgressionManager.Instance.OnArenaComplete();
            }
        }

        private void UpdateTimerUI()
        {
            if (timerText != null)
                timerText.text = $"{_timer:F1}";

            if (clockHand != null)
            {
                float normalized = Mathf.Clamp01(_timer / _currentSettings.survivalTime);
                float angle = -90f - (1f - normalized) * 360f;
                clockHand
                    .DOLocalRotate(new Vector3(0f, 0f, angle), 0.1f)
                    .SetEase(Ease.Linear)
                    .SetId(this);
            }
        }
    }
}
