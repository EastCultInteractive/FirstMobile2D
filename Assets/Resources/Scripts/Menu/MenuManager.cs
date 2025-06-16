using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Resources.Scripts.Menu
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Ссылка на панель выбора этапов")]
        [SerializeField] private GameObject stageSelectionPanel;

        [Header("Ссылки на кнопки")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            // Подписываемся на все клики здесь, чтобы иметь доступ к самим Button
            if (playButton != null)    playButton.onClick.AddListener(OnPlayButton);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsButton);
            if (exitButton != null)    exitButton.onClick.AddListener(OnExitButton);
        }

        private void OnDestroy()
        {
            // Отписываемся, чтобы не было утечек
            if (playButton != null)    playButton.onClick.RemoveListener(OnPlayButton);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsButton);
            if (exitButton != null)    exitButton.onClick.RemoveListener(OnExitButton);
        }

        public void OnPlayButton()
        {
            // Анимация появления панели выбора этапов
            if (stageSelectionPanel == null)
                return;

            var t = stageSelectionPanel.transform;
            t.DOKill(true);          // завершить старые твины и вернуть к исходному
            t.localScale = Vector3.zero;
            stageSelectionPanel.SetActive(true);

            t
             .DOScale(1f, 0.4f)
             .SetEase(Ease.OutBack);
        }

        public void OnOptionsButton()
        {
            // небольшой «пранч» самой кнопки
            if (optionsButton != null)
            {
                var t = optionsButton.transform;
                t.DOKill(true);                      // остановить старые
                t.localScale = Vector3.one;          // сбросить масштаб
                t
                 .DOPunchScale(Vector3.one * 0.05f, 0.3f, 10, 0.5f)
                 .SetEase(Ease.OutBack)
                 .OnComplete(() => t.localScale = Vector3.one);
            }

            Debug.Log("Опции пока не реализованы");
        }

        public void OnExitButton()
        {
            // анимация «пранча» и выход из игры
            if (exitButton != null)
            {
                var t = exitButton.transform;
                t.DOKill(true);
                t.localScale = Vector3.one;
                t
                 .DOPunchScale(Vector3.one * 0.05f, 0.3f, 10, 0.5f)
                 .SetEase(Ease.OutBack)
                 .OnComplete(() =>
                 {
                     Application.Quit();
                     Debug.Log("Выход из игры");
                 });
            }
        }
    }
}
