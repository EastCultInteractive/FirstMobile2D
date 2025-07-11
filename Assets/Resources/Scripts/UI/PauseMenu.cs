using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Resources.Scripts.Entity.UI
{
    /// <summary>
    /// Manages the pause menu, allowing the game to be paused/resumed,
    /// opening settings, and exiting to the main menu.
    /// Добавлены анимации появления/скрытия панелей.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("UI panel displayed when the game is paused.")]
        public GameObject pausePanel;
        [Tooltip("UI panel for settings (currently for demonstration).")]
        public GameObject settingsPanel;

        private bool isPaused;

        private void Start()
        {
            // Hide both pause and settings panels at game start.
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        /// <summary>
        /// Called when the Pause button is pressed.
        /// Toggles between pausing and resuming the game.
        /// </summary>
        public void OnPauseButtonPressed()
        {
            if (!isPaused)
                PauseGame();
            else
                ResumeGame();
        }

        /// <summary>
        /// Pauses the game by showing the pause panel и плавно показываем его через DOTween.
        /// </summary>
        public void PauseGame()
        {
            if (pausePanel != null)
            {
                pausePanel.transform.localScale = Vector3.zero;
                pausePanel.SetActive(true);
                pausePanel.transform
                    .DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            Time.timeScale = 0f;
            isPaused = true;
        }

        /// <summary>
        /// Resumes the game: скрываем панель через анимацию, затем деактивируем.
        /// </summary>
        public void ResumeGame()
        {
            if (pausePanel != null)
            {
                pausePanel.transform
                    .DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => pausePanel.SetActive(false));
            }

            Time.timeScale = 1f;
            isPaused = false;
        }

        /// <summary>
        /// Opens the settings panel while keeping the pause panel active.
        /// Плавное появление.
        /// </summary>
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.transform.localScale = Vector3.zero;
                settingsPanel.SetActive(true);
                settingsPanel.transform
                    .DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            else
                Debug.LogWarning("Settings panel is not assigned!");
        }

        /// <summary>
        /// Closes the settings panel и плавно скрываем его.
        /// </summary>
        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.transform
                    .DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => settingsPanel.SetActive(false));
            }
            else
                Debug.LogWarning("Settings panel is not assigned!");
        }

        /// <summary>
        /// Exits to the main menu scene.
        /// </summary>
        public void ExitToMainMenu()
        {
            // Reset time scale before transitioning.
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }
    }
}
