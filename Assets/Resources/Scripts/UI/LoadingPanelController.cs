using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace Resources.Scripts.Entity.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingPanelController : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("Длительность появления/исчезновения")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Text Animation")]
        [Tooltip("Ссылка на TextMeshProUGUI с текстом \"Загрузка\"")]
        [SerializeField] private TMP_Text loadingText;
        [Tooltip("Интервал между изменениями точек (сек)")]
        [SerializeField] private float dotInterval = 0.5f;

        private CanvasGroup canvasGroup;
        private Coroutine dotsCoroutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Показывает панель: fade-in через DOTween + анимация точек.</summary>
        public void Show()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup
                .DOFade(1f, fadeDuration)
                .SetUpdate(true);

            dotsCoroutine = StartCoroutine(AnimateDots());
        }

        /// <summary>Скрывает панель: fade-out через DOTween и останавливает анимацию точек.</summary>
        public void Hide()
        {
            if (dotsCoroutine != null)
                StopCoroutine(dotsCoroutine);

            canvasGroup
                .DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private IEnumerator AnimateDots()
        {
            const string baseText = "Загрузка";
            int dots = 0;
            while (true)
            {
                loadingText.text = baseText + new string('.', dots);
                dots = (dots + 1) % 4;
                yield return new WaitForSecondsRealtime(dotInterval);
            }
        }
    }
}
