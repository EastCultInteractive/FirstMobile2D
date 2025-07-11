using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Resources.Scripts.Entity.Labyrinth
{
    /// <summary>
    /// Controls the minimap display at the beginning of the game.
    /// The map is shown at game start and can be toggled via buttons.
    /// Two timer texts are displayed: one on the map panel and another on the player's panel.
    /// When the countdown expires, the minimap automatically closes.
    /// Also synchronizes the solution path display on the minimap.
    /// </summary>
    public class LabyrinthMapController : MonoBehaviour
    {
        public static LabyrinthMapController Instance { get; private set; }

        [Header("Map Display Settings")]
        [SerializeField, Tooltip("UI panel that shows the minimap.")]
        private GameObject mapDisplay;
        [SerializeField, Tooltip("Button used to show the map.")]
        private Button showMapButton;
        [SerializeField, Tooltip("Button used to hide the map.")]
        private Button hideMapButton;
        [SerializeField, Tooltip("Total time (in seconds) before the map automatically closes.")]
        private float toggleDuration = 100f;
        [SerializeField, Tooltip("Enable debug logging for the minimap controller.")]
        private bool debugLog;

        [Header("Timer Texts")]
        [SerializeField, Tooltip("Text displayed on the map panel for countdown.")]
        private Text mapTimerText;
        [SerializeField, Tooltip("Text displayed on the player's panel above the 'Show Map' button.")]
        private Text playerTimerText;

        [Header("Solution Path")]
        [SerializeField, Tooltip("Reference to the solution path drawer component.")]
        private LabyrinthMinimapSolutionPathDrawer solutionPathDrawer;

        private float timer;
        private bool canToggle = true;

        /// <summary>
        /// Returns true if the map display is currently active.
        /// </summary>
        public bool IsMapActive => mapDisplay != null && mapDisplay.activeSelf;

        private void Awake()
        {
            // Singleton pattern.
            Instance = this;
        }

        private void Start()
        {
            // Show the map at the start.
            if (mapDisplay != null)
            {
                mapDisplay.SetActive(true);
                if (debugLog) Debug.Log("Minimap display activated at start.");
            }
            else
            {
                Debug.LogWarning("Map Display is not assigned in LabyrinthMapController!");
            }

            // Assign button listeners.
            if (hideMapButton != null)
            {
                hideMapButton.onClick.AddListener(HideMap);
            }
            else
            {
                Debug.LogWarning("Hide Map Button is not assigned!");
            }

            if (showMapButton != null)
            {
                showMapButton.onClick.AddListener(ShowMap);
            }
            else
            {
                Debug.LogWarning("Show Map Button is not assigned!");
            }
        }

        private void Update()
        {
            // Increase timer based on elapsed time.
            timer += Time.deltaTime;
            // Calculate remaining time (clamped to a minimum of zero).
            float remainingTime = Mathf.Max(0, toggleDuration - timer);
            int secondsRemaining = Mathf.CeilToInt(remainingTime);

            // Update both timer texts.
            if (mapTimerText != null)
                mapTimerText.text = secondsRemaining.ToString();
            if (playerTimerText != null)
                playerTimerText.text = secondsRemaining.ToString();

            // Auto-close the minimap when time expires.
            if (timer >= toggleDuration && canToggle)
            {
                if (mapDisplay != null)
                {
                    mapDisplay.SetActive(false);
                    if (debugLog) Debug.Log("Minimap auto-closed after timer expired.");
                }

                // Remove the "Show Map" button to prevent reopening.
                if (showMapButton != null)
                {
                    Destroy(showMapButton.gameObject);
                    if (debugLog) Debug.Log("Show Map button removed after timer expiration.");
                }

                canToggle = false;
            }
        }

        /// <summary>
        /// Hides the map display.
        /// </summary>
        public void HideMap()
        {
            if (mapDisplay != null)
            {
                mapDisplay.SetActive(false);
                if (debugLog) Debug.Log("Minimap display hidden.");
            }
        }

        /// <summary>
        /// Shows the map display (allowed only while toggleDuration has not expired).
        /// </summary>
        public void ShowMap()
        {
            if (canToggle && mapDisplay != null)
            {
                mapDisplay.SetActive(true);
                if (debugLog) Debug.Log("Minimap display shown.");
            }
        }

        /// <summary>
        /// Sets the solution path on the minimap by passing a list of world positions to the path drawer.
        /// </summary>
        /// <param name="positions">List of world-space positions representing the solution path.</param>
        public void SetSolutionPath(List<Vector3> positions)
        {
            if (solutionPathDrawer != null)
            {
                solutionPathDrawer.DrawSolutionPath(positions);
                if (debugLog) Debug.Log("Solution path drawn on minimap.");
            }
        }
    }
}
