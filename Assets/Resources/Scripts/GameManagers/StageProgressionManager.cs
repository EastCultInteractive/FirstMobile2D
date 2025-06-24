using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [Header("Game Over UI")]
        [Tooltip("Префаб панели Game Over с Legacy-текстом и двумя кнопками (RestartButton, ExitButton)")]
        [SerializeField] private GameObject gameOverPanelPrefab = null!;

        [Header("Имена универсальных сцен")]
        [Tooltip("Имя единственной сцены арены (например, \"Arena\").")]
        [SerializeField] private string genericArenaSceneName = "Arena";
        [Tooltip("Имя единственной сцены лабиринта (например, \"Labyrinth\").")]
        [SerializeField] private string genericLabyrinthSceneName = "Labyrinth";

        private int currentArenaIndex;
        private readonly List<PerkDefinition> selectedPerks = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
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
            DOTween.Kill(gameObject);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Запускает текущий Stage: начинает с его первой арены.
        /// </summary>
        public void StartStage()
        {
            Time.timeScale = 1f;

            currentArenaIndex = 0;
            selectedPerks.Clear();
            LoadArena(currentArenaIndex);
        }

        private void LoadArena(int idx)
        {
            var data = GameStageManager.currentStageData;
            if (data == null || idx < 0 || idx >= data.arenaSettingsList.Length)
            {
                Debug.LogError($"StageProgressionManager.LoadArena: неверный индекс арены {idx}");
                return;
            }

            CurrentArenaSettings = data.arenaSettingsList[idx];
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
            if (string.IsNullOrEmpty(genericLabyrinthSceneName))
            {
                Debug.LogError("StageProgressionManager: не задано имя genericLabyrinthSceneName.");
                return;
            }
            SceneManager.LoadScene(genericLabyrinthSceneName);
        }

        /// <summary>
        /// Вызывается при провале арены или смерти в лабиринте.
        /// Показывает панель Game Over и останавливает игру.
        /// </summary>
        public void ShowGameOver()
        {
            Time.timeScale = 0f;

            if (gameOverPanelPrefab == null)
            {
                Debug.LogError("StageProgressionManager.ShowGameOver: не назначен gameOverPanelPrefab!");
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            var panel = Instantiate(
                gameOverPanelPrefab,
                canvas != null ? canvas.transform : null,
                false);
            
            var text = panel.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null)
                text.text = "Вы проиграли...";
            
            foreach (var btn in panel.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (btn.name == "RestartButton")
                {
                    btn.onClick.AddListener(() =>
                    {
                        Time.timeScale = 1f;
                        StartStage();
                    });
                }
                else if (btn.name == "ExitButton")
                {
                    btn.onClick.AddListener(() =>
                    {
                        Time.timeScale = 1f;
                        SceneManager.LoadScene("Menu");
                    });
                }
            }
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

        public void OnLabyrinthCompleted()
        {
            bool hasNext = GameStageManager.Instance.LoadNextStage();
            if (hasNext)
                StartStage();
            else
                Debug.Log("Поздравляем! Все этапы пройдены.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var valid = new List<string> { genericArenaSceneName, genericLabyrinthSceneName };
            if (!valid.Contains(scene.name))
            {
                selectedPerks.Clear();
                var stats = Object.FindFirstObjectByType<PlayerStatsHandler>();
                stats?.ResetStats();
            }
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
