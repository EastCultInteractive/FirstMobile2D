using Resources.Scripts.Entity.Data;
using UnityEngine;

namespace Resources.Scripts.Entity.GameManagers
{
    [DefaultExecutionOrder(-200)]
    public class GameStageManager : MonoBehaviour
    {
        public static GameStageManager Instance { get; private set; }
        public static StageData currentStageData { get; private set; }

        [Header("Все доступные StageData")]
        [Tooltip("Перетащить сюда SO StageData в порядке прохождения этапов")]
        [SerializeField] private StageData[] stages = null!;

        private int currentStageIndex = 0;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (stages == null || stages.Length == 0)
            {
                Debug.LogError("GameStageManager: не задан ни один StageData!");
                return;
            }
            // при старте по‑умолчанию грузим первый этап
            InitializeStage(0);
        }

        /// <summary>
        /// Устанавливает текущий этап по индексу и запоминает StageData.
        /// </summary>
        public void InitializeStage(int index)
        {
            if (stages == null || index < 0 || index >= stages.Length)
            {
                Debug.LogError($"GameStageManager.InitializeStage: некорректный индекс {index}");
                return;
            }
            currentStageIndex = index;
            currentStageData = stages[index];
            Debug.Log($"GameStageManager: установлен этап #{index} — {currentStageData.stageName}");
        }

        /// <summary>
        /// Пытается перейти к следующему StageData.
        /// Возвращает true, если переключение прошло успешно.
        /// </summary>
        public bool LoadNextStage()
        {
            if (stages == null || currentStageIndex + 1 >= stages.Length)
            {
                Debug.Log("GameStageManager: все этапы пройдены.");
                return false;
            }
            InitializeStage(currentStageIndex + 1);
            return true;
        }
    }
}
