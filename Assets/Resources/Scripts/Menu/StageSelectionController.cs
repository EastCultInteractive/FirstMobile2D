using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Resources.Scripts.Data;
using Resources.Scripts.GameManagers;
using Resources.Scripts.UI;

namespace Resources.Scripts.Menu
{
    public class StageSelectionController : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private Image stageImage;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button closeButton;

        [Header("Фон панели")]
        [SerializeField] private Image panelBackground;
        [SerializeField] private Sprite[] backgroundSprites;

        [Header("Данные этапов")]
        [SerializeField] private StageData[] stages;

        [Header("Панель загрузки")]
        [Tooltip("Controller для панели загрузки с CanvasGroup и анимацией точек")]
        [SerializeField] private LoadingPanelController loadingPanelController;

        private int currentStageIndex;

        private void Start()
        {
            UpdateStageDisplay();

            leftButton.onClick.AddListener(OnLeftButton);
            rightButton.onClick.AddListener(OnRightButton);
            playButton.onClick.AddListener(OnPlayButton);
            closeButton.onClick.AddListener(OnCloseButton);

            if (loadingPanelController != null)
                loadingPanelController.gameObject.SetActive(false);
        }

        private void UpdateStageDisplay()
        {
            if (stages == null || stages.Length == 0) return;
            var stage = stages[currentStageIndex];

            stageNameText.text = stage.stageName;
            stageImage.sprite  = stage.stageImage;

            leftButton.interactable  = currentStageIndex > 0;
            rightButton.interactable = currentStageIndex < stages.Length - 1;

            if (panelBackground != null && currentStageIndex < backgroundSprites.Length)
                panelBackground.sprite = backgroundSprites[currentStageIndex];

            // Безопасная перезапись анимации панели
            var bgTransform = panelBackground.transform;
            bgTransform.DOKill(true);
            bgTransform.localScale = Vector3.one;
            bgTransform
                .DOPunchScale(Vector3.one * 0.05f, 0.3f)
                .SetEase(Ease.OutQuad);

            // Плавное проявление картинки и текста
            stageImage.DOKill();
            stageImage.color = new Color(1,1,1,0);
            stageImage
                .DOFade(1f, 0.5f)
                .SetEase(Ease.Linear);

            stageNameText.DOKill();
            stageNameText.color = new Color(1,1,1,0);
            stageNameText
                .DOFade(1f, 0.5f)
                .SetEase(Ease.Linear);
        }

        private void OnLeftButton()
        {
            if (currentStageIndex <= 0) return;
            currentStageIndex--;
            UpdateStageDisplay();
        }

        private void OnRightButton()
        {
            if (currentStageIndex >= stages.Length - 1) return;
            currentStageIndex++;
            UpdateStageDisplay();
        }

        private void OnPlayButton()
        {
            // Показываем панель загрузки
            loadingPanelController?.Show();

            // Safe punch-анимация кнопки Play
            var pt = playButton.transform;
            pt.DOKill(true);
            pt.localScale = Vector3.one;
            pt
                .DOPunchScale(Vector3.one * 0.1f, 0.3f, 8, 0.5f)
                .SetEase(Ease.OutElastic);

            // Устанавливаем выбранный StageData
            if (GameStageManager.Instance != null)
                GameStageManager.Instance.InitializeStage(currentStageIndex);
            else
                Debug.LogError("StageSelectionController: GameStageManager.Instance == null");

            // Гарантируем наличие менеджера прогресса
            if (StageProgressionManager.Instance == null)
            {
                var go = new GameObject("StageProgressionManager");
                go.AddComponent<StageProgressionManager>();
            }

            StartCoroutine(StartStageWithDelay());
        }

        private IEnumerator StartStageWithDelay()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            StageProgressionManager.Instance.StartStage();
        }

        private void OnCloseButton()
        {
            // Safe hide анимация панели выбора
            var t = transform;
            t.DOKill(true);
            t.localScale = Vector3.one;
            t
                .DOScale(0f, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
