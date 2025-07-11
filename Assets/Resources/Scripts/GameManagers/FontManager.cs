using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Resources.Scripts.Entity.GameManagers
{
    [DefaultExecutionOrder(-100)]
    public class FontManager : MonoBehaviour
    {
        private static FontManager Instance { get; set; }

        [Header("Шрифт для TextMeshPro")]
        [SerializeField] private TMP_FontAsset tmpFont;

        [Header("Шрифт для Legacy UI Text")]
        [SerializeField] private Font legacyFont;

        // Кеш компонентов на сцене
        private readonly List<TMP_Text> tmpTexts = new List<TMP_Text>();
        private readonly List<Text> uiTexts = new List<Text>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Первый проход по уже загруженной сцене
            RefreshTextList();
            ApplyFonts();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshTextList();
            ApplyFonts();
        }

        /// <summary>
        /// Собирает на сцене все TMP_Text и UI Text (legacy)
        /// </summary>
        private void RefreshTextList()
        {
            tmpTexts.Clear();
            uiTexts.Clear();

            // Все TextMeshPro (включая неактивные)
            var foundTMP = Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            tmpTexts.AddRange(foundTMP);

            // Все UnityEngine.UI.Text (включая неактивные)
            var foundUI = Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            uiTexts.AddRange(foundUI);
        }

        /// <summary>
        /// Применяет два шрифта: tmpFont к TMP_Text и legacyFont к UI Text
        /// </summary>
        private void ApplyFonts()
        {
            if (tmpFont != null)
            {
                foreach (var t in tmpTexts)
                {
                    t.font = tmpFont;
                    t.ForceMeshUpdate();
                }
            }

            if (legacyFont != null)
            {
                foreach (var t in uiTexts)
                {
                    t.font = legacyFont;
                    t.SetAllDirty();
                }
            }
        }
    }
}
