using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Resources.Scripts.Entity.UI
{
    public class UIButtonAnimator : MonoBehaviour
    {
        [Header("Настройки анимации")]
        [SerializeField] private float punchScale = 0.2f;
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private int vibrato = 10;
        [SerializeField] private float elasticity = 1f;

        [Header("Применять к кнопкам на сцене")]
        [SerializeField] private bool applyOnStart = true;

        private readonly HashSet<Button> animatedButtons = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject); // Переживает смену сцен
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            if (applyOnStart)
                AnimateAllSceneButtons();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Вызывается при загрузке новой сцены.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AnimateAllSceneButtons();
        }

        /// <summary>
        /// Находит все кнопки в сцене и навешивает на них анимацию.
        /// </summary>
        public void AnimateAllSceneButtons()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (animatedButtons.Contains(btn)) 
                    continue;

                AddAnimation(btn);
                animatedButtons.Add(btn);
            }
        }

        /// <summary>
        /// Добавляет анимацию нажатия к кнопке.
        /// </summary>
        private void AddAnimation(Button button)
        {
            button.onClick.AddListener(() => Animate(button.transform));
        }

        /// <summary>
        /// Запускает пунч-анимацию: перед этим убираем все старые твины и сбрасываем масштаб.
        /// </summary>
        private void Animate(Transform target)
        {
            // Завершить все текущие твины сразу и вернуть объект в исходное состояние
            target.DOKill(true);
            target.localScale = Vector3.one;

            // Выполнить «пунч»-анимацию масштаба
            target.DOPunchScale(Vector3.one * punchScale, duration, vibrato, elasticity);
        }
    }
}
