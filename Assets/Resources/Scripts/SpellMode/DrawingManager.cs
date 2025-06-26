#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Resources.Scripts.Player;        // Доступ к playerStatsHandler
using Resources.Scripts.SpellMode.Skills;
using Resources.Scripts.UI;            // Для ManaSpendEffect
using Resources.Scripts.Audio;         // Для GlobalAudioManager

namespace Resources.Scripts.SpellMode
{
    /// <summary>
    /// Manages gesture drawing, recognition, and execution of spell skills.
    /// Делегирует звуковой ритм тиков в GlobalAudioManager.
    /// Добавлены безопасные твины с SetId + Kill.
    /// </summary>
    public class DrawingManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("UI Elements")]
        [SerializeField] private Text? feedbackText;

        [Header("Line Prefab")]
        [Tooltip("Prefab с LineRenderer для отрисовки линии.")]
        [SerializeField] private GameObject? drawingLinePrefab;

        [Header("LineRenderer Settings")]
        [SerializeField] private Color drawingLineColor = Color.red;
        [SerializeField] private float drawingLineWidth = 0.1f;
        [SerializeField] private int drawingLineSortingOrder = 100;

        [Header("Drawing Settings")]
        [Tooltip("Макс. время рисования, с.")]
        [SerializeField] private float maxDrawingTime = 5f;
        [Tooltip("Мин. расстояние между точками.")]
        [SerializeField] private float movementThreshold = 0.1f;
        [Tooltip("Время без движения для окончания сегмента.")]
        [SerializeField] private float stopTimeThreshold = 0.5f;
        [Tooltip("Мин. кол-во точек для распознавания.")]
        [SerializeField] private int minPointsForRecognition = 10;
        [Tooltip("Мин. длина пути для валидного жеста.")]
        [SerializeField] private float minTotalPathLength = 1.0f;

        [Header("Combo Settings")]
        [Tooltip("Разрешить несколько жестов за одну сессию.")]
        [SerializeField] private bool enableComboMode = true;
        [Tooltip("Макс. время между жестами в комбо.")]
        [SerializeField] private float comboTimeWindow = 3f;

        [Header("Recognition Settings")]
        [SerializeField] private IconGestureRecognizer? gestureRecognizer;
        [SerializeField] private int resampleCount = 64;
        [SerializeField] private int smoothingWindow = 3;

        [Header("Player Stats")]
        [Tooltip("Управление маной игрока.")]
        [SerializeField] private PlayerStatsHandler? playerStatsHandler;

        [Header("Mana Spend FX")]
        [SerializeField] private GameObject? manaSpendEffectPrefab;
        [SerializeField] private RectTransform? manaBarUI;
        [SerializeField] private Canvas? mainCanvas;

        #endregion
        
        #region Public Events & Properties
        public bool IsDrawing { get; private set; }
        #endregion

        #region Private State

        private float drawingTimer;
        private float noMovementTimer;
        private float comboTimer;

        private Vector3 lastRecordedPosition;
        private readonly List<Vector3> drawnPoints = new List<Vector3>();
        private LineRenderer? currentLine;
        private bool hasMoved;

        private readonly List<SignTemplateIcon> recognizedSigns = new List<SignTemplateIcon>();
        private GlobalAudioManager? audioManager;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            audioManager = GlobalAudioManager.Instance;

            if (manaBarUI == null)
                manaBarUI = Object.FindFirstObjectByType<Canvas>()?
                                  .GetComponentInChildren<RectTransform>();

            lastRecordedPosition = transform.position;
        }

        private void Update()
        {
            if (!IsDrawing)
                return;

            drawingTimer += Time.deltaTime;
            Vector3 currentPos = transform.position;
            float distance = Vector3.Distance(currentPos, lastRecordedPosition);

            if (distance >= movementThreshold)
            {
                noMovementTimer = 0f;
                hasMoved = true;
                drawnPoints.Add(currentPos);
                lastRecordedPosition = currentPos;
                UpdateDrawingLine();
            }
            else
            {
                noMovementTimer += Time.deltaTime;
                if (hasMoved && noMovementTimer >= stopTimeThreshold)
                {
                    if (enableComboMode)
                    {
                        ProcessPartialDrawing();
                        drawnPoints.Clear();
                        noMovementTimer = 0f;
                        if (feedbackText?.transform != null)
                        {
                            feedbackText.transform
                                         .DOPunchScale(Vector3.one * 1.1f, 0.2f)
                                         .SetId(this)
                                         .SetUpdate(true);
                        }
                    }
                    else
                    {
                        EndDrawing();
                        return;
                    }
                }
            }

            if (drawingTimer >= maxDrawingTime)
                EndDrawing();

            if (recognizedSigns.Count > 0)
            {
                comboTimer += Time.deltaTime;
                if (comboTimer >= comboTimeWindow)
                    EndDrawing();
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        #endregion

        #region Public Methods

        public void StartDrawing()
        {
            if (currentLine != null)
            {
                Destroy(currentLine.gameObject);
                currentLine = null;
            }

            IsDrawing = true;
            drawingTimer = noMovementTimer = comboTimer = 0f;
            hasMoved = false;
            drawnPoints.Clear();
            recognizedSigns.Clear();

            lastRecordedPosition = transform.position;
            drawnPoints.Add(lastRecordedPosition);

            if (drawingLinePrefab != null)
            {
                var lineObj = Instantiate(drawingLinePrefab);
                currentLine = lineObj.GetComponent<LineRenderer>();
                if (currentLine != null)
                {
                    currentLine.useWorldSpace = true;
                    currentLine.startWidth = drawingLineWidth;
                    currentLine.endWidth = drawingLineWidth;
                    currentLine.startColor = drawingLineColor;
                    currentLine.endColor = drawingLineColor;
                    currentLine.sortingOrder = drawingLineSortingOrder;
                    currentLine.positionCount = 1;
                    currentLine.SetPosition(0, lastRecordedPosition);
                }
                else
                {
                    Debug.LogError("[DrawingManager] drawingLinePrefab has no LineRenderer!");
                }
            }
            else
            {
                Debug.LogError("[DrawingManager] drawingLinePrefab is not assigned.");
            }

            if (feedbackText != null)
            {
                feedbackText.text = "Drawing...";
                feedbackText
                    .DOFade(1f, 0.2f)
                    .From(0f)
                    .SetId(this)
                    .SetUpdate(true);
            }

            audioManager?.StartTickRhythm(maxDrawingTime);
        }

        public void EndDrawing()
        {
            if (!IsDrawing)
                return;

            IsDrawing = false;

            if (feedbackText != null)
            {
                feedbackText.text = "Processing...";
                feedbackText
                    .DOFade(1f, 0.2f)
                    .From(0f)
                    .SetId(this)
                    .SetUpdate(true);
            }

            audioManager?.StopTickRhythm();

            if (drawnPoints.Count >= minPointsForRecognition)
                ProcessPartialDrawing();

            if (recognizedSigns.Count == 0)
            {
                if (feedbackText != null)
                    feedbackText.text = "Skill not recognized";
                return;
            }

            bool isCombo = recognizedSigns.Count >= 2;
            int totalManaCost = 0;
            foreach (var template in recognizedSigns)
                totalManaCost += Mathf.RoundToInt(template.manaCost);

            Debug.Log($"[DrawingManager] Attempt to spend {totalManaCost} mana");

            if (playerStatsHandler != null && playerStatsHandler.UseMana(totalManaCost))
            {
                ShowManaSpendEffect(totalManaCost);
                foreach (var template in recognizedSigns)
                    ExecuteSkill(template, isCombo);

                if (feedbackText != null)
                    feedbackText.text = isCombo ? "Combo Skill Activated!" : "Skill Activated!";
            }
            else
            {
                if (feedbackText != null)
                    feedbackText.text = "Not enough mana";
            }

            recognizedSigns.Clear();
            drawnPoints.Clear();
            comboTimer = 0f;
        }

        #endregion

        #region Private Methods

        private void ProcessPartialDrawing()
        {
            if (drawnPoints.Count < minPointsForRecognition)
                return;

            float totalLength = 0f;
            for (int i = 1; i < drawnPoints.Count; i++)
                totalLength += Vector3.Distance(drawnPoints[i - 1], drawnPoints[i]);

            if (totalLength < minTotalPathLength)
                return;

            var points2D = new List<Vector2>(drawnPoints.Count);
            foreach (var pt in drawnPoints)
                points2D.Add(new Vector2(pt.x, pt.y));

            var normalized = GestureUtils.NormalizePoints(points2D, resampleCount, smoothingWindow);
            Debug.Log($"Normalized Input: {Vector2ListToString(normalized)}");

            var recognized = gestureRecognizer?.Recognize(normalized);
            if (recognized != null)
            {
                recognizedSigns.Add(recognized);
                comboTimer = 0f;
                if (feedbackText != null)
                {
                    feedbackText.text = $"Added: {recognized.id}";
                    feedbackText.transform
                                .DOPunchScale(Vector3.one * 1.1f, 0.2f)
                                .SetId(this)
                                .SetUpdate(true);
                }
            }
        }

        private void ExecuteSkill(SignTemplateIcon template, bool isCombo)
        {
            if (template.skillPrefab == null)
                return;
            var skillObj = Instantiate(template.skillPrefab, transform.position, Quaternion.identity);
            if (!isCombo) return;

            var skillComp = skillObj.GetComponent<SkillBase>();
            if (skillComp == null) return;

            string idLower = template.id.ToLower();
            if (idLower.Contains("circle") && skillComp is PushEnemiesSkill push)
                push.pushForce *= 1.5f;
            else if (idLower.Contains("square") && skillComp is SlowEnemiesSkill slow)
                slow.slowDuration *= 1.5f;
            else if (idLower.Contains("triangle") && skillComp is SpeedBoostSkill speed)
                speed.boostDuration += 50f;
        }

        private void UpdateDrawingLine()
        {
            if (currentLine == null) return;
            currentLine.positionCount = drawnPoints.Count;
            for (int i = 0; i < drawnPoints.Count; i++)
                currentLine.SetPosition(i, drawnPoints[i]);
        }

        private void ShowManaSpendEffect(int amount)
        {
            Debug.Log("[DrawingManager] Instantiating ManaSpendEffect");

            if (manaSpendEffectPrefab == null)
            {
                Debug.LogError("[DrawingManager] manaSpendEffectPrefab is not assigned!");
                return;
            }
            if (mainCanvas == null)
            {
                mainCanvas = Object.FindFirstObjectByType<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError("[DrawingManager] Could not find any Canvas in the scene!");
                    return;
                }
            }
            if (manaBarUI == null)
            {
                Debug.LogError("[DrawingManager] manaBarUI is not assigned or destroyed!");
                return;
            }

            var effectGo = Instantiate(
                manaSpendEffectPrefab,
                mainCanvas.transform,
                worldPositionStays: false
            );
            effectGo.transform.localScale = Vector3.one;

            var effect = effectGo.GetComponent<ManaSpendEffect>();
            if (effect != null)
                effect.Initialize(amount, transform.position, manaBarUI);
            else
                Debug.LogError("[DrawingManager] Prefab does not contain a ManaSpendEffect component!");
        }

        private string Vector2ListToString(List<Vector2> points)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in points)
                sb.AppendFormat("({0:F2}, {1:F2}) ", p.x, p.y);
            return sb.ToString();
        }

        #endregion
    }
}
