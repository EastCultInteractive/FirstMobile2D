using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Resources.Scripts.Data;
using Resources.Scripts.Player;

namespace Resources.Scripts.GameManagers
{
    [DefaultExecutionOrder(-100)]
    public class StageProgressionManager : MonoBehaviour
    {
        public static StageProgressionManager Instance { get; private set; }
        public static ArenaSettings CurrentArenaSettings { get; private set; }

        [Header("Доступные перки (через инспектор)")]
        [SerializeField] private PerkDefinition[] availablePerks = null!;

        [Header("UI панели перков")]
        [SerializeField] private GameObject perkSelectionPanelPrefab = null!;

        [Header("Имена универсальных сцен")]
        [Tooltip("Имя единственной сцены арены (например, \"Arena\").")]
        [SerializeField] private string genericArenaSceneName = "Arena";

        [Tooltip("Имя единственной сцены лабиринта (например, \"Labyrinth\").")]
        [SerializeField] private string genericLabyrinthSceneName = "Labyrinth";

        private int currentArenaIndex;
        private readonly List<PerkDefinition> selectedPerks = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Запускает текущий Stage: начинает с его первой арены.
        /// </summary>
        public void StartStage()
        {
            currentArenaIndex = 0;
            selectedPerks.Clear();
            LoadArena(currentArenaIndex);
        }

        private void LoadArena(int idx)
        {
            var data = GameStageManager.currentStageData;
            if (data == null || idx < 0
                || idx >= data.arenaSettingsList.Length)
            {
                Debug.LogError($"StageProgressionManager.LoadArena: неверный индекс арены {idx}");
                return;
            }

            // Берём настройки арены из SO
            CurrentArenaSettings = data.arenaSettingsList[idx];

            // Загрузим _универсальную_ сцену арены по имени genericArenaSceneName
            if (string.IsNullOrEmpty(genericArenaSceneName))
            {
                Debug.LogError("StageProgressionManager: не задано имя genericArenaSceneName.");
                return;
            }
            SceneManager.LoadScene(genericArenaSceneName);
        }

        private void LoadLabyrinth()
        {
            var data = GameStageManager.currentStageData;
            if (data == null)
            {
                Debug.LogError("StageProgressionManager.LoadLabyrinth: StageData == null");
                return;
            }

            // Загрузим _универсальную_ сцену лабиринта по имени genericLabyrinthSceneName
            if (string.IsNullOrEmpty(genericLabyrinthSceneName))
            {
                Debug.LogError("StageProgressionManager: не задано имя genericLabyrinthSceneName.");
                return;
            }
            SceneManager.LoadScene(genericLabyrinthSceneName);
        }

        public void OnArenaComplete()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var panel = Instantiate(
                perkSelectionPanelPrefab,
                canvas != null ? canvas.transform : null,
                false);

            panel.transform.localScale = Vector3.zero;
            panel.transform
                 .DOScale(1f, 0.4f)
                 .SetEase(Ease.OutBack)
                 .SetId(this);

            var opts = availablePerks
                .OrderBy(_ => Random.value)
                .Take(3)
                .ToArray();

            var ctrl = panel.GetComponent<Resources.Scripts.Menu.PerkSelectionController>();
            ctrl.Setup(opts);
        }

        public void OnPerkChosen(PerkDefinition perk)
        {
            selectedPerks.Add(perk);
            ApplyAllPerks();

            currentArenaIndex++;
            var data = GameStageManager.currentStageData;
            if (data == null)
            {
                Debug.LogError("StageProgressionManager.OnPerkChosen: StageData == null");
                return;
            }

            if (currentArenaIndex < data.arenaSettingsList.Length)
                LoadArena(currentArenaIndex);
            else
                LoadLabyrinth();
        }

        /// <summary>
        /// Вызывается после прохождения лабиринта.
        /// Пытаемся перейти к следующему StageData и запустить его.
        /// </summary>
        public void OnLabyrinthCompleted()
        {
            bool hasNext = GameStageManager.Instance.LoadNextStage();
            if (hasNext)
            {
                StartStage();
            }
            else
            {
                Debug.Log("Поздравляем! Все этапы пройдены.");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Считаем _валидными_ только две сцены: универсальную арену и универсальный лабиринт.
            var validScenes = new List<string>
            {
                genericArenaSceneName,
                genericLabyrinthSceneName
            };

            // Если загрузилась не та, что мы ждём (например, вернулись в главное меню или куда-то ещё),
            // сбрасываем накопленные перки и статы игрока.
            if (!validScenes.Contains(scene.name))
            {
                selectedPerks.Clear();
                var stats = Object.FindFirstObjectByType<PlayerStatsHandler>();
                stats?.ResetStats();
            }

            // После того, как сцена загрузилась, применим все уже выбранные перки.
            StartCoroutine(DelayedApplyAllPerks());
        }

        private IEnumerator DelayedApplyAllPerks()
        {
            yield return null;
            ApplyAllPerks();
        }

        private void ApplyAllPerks()
        {
            var stats = Object.FindFirstObjectByType<PlayerStatsHandler>();
            if (stats == null) return;

            stats.ResetStats();
            foreach (var p in selectedPerks)
                p.Apply(stats);
        }
    }
}
