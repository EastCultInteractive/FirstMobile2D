using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.SpellMode
{
    public class IconGestureRecognizer : MonoBehaviour
    {
        public SignTemplateDatabaseIcon signDatabase;
        [Tooltip("Recognition threshold (final distance)")]
        public float recognitionThreshold;

        [Header("Normalization Settings")]
        public int resampleCount;
        public int smoothingWindow;
        [Tooltip("Use PCA for normalization (improves rotation invariance)")]
        public bool usePcaForNormalization;
        [Tooltip("Use alternative algorithm (average distance) instead of DTW")]
        public bool useAlternativeAlgorithm;

        [Header("Metric Weights")]
        [Tooltip("Weight for DTW distance")]
        public float dtwWeight;
        [Tooltip("Weight for aspect ratio difference")]
        public float aspectRatioWeight;
        [Tooltip("Weight for circularity difference")]
        public float circularityWeight;
        [Tooltip("Weight for average curvature difference")]
        public float curvatureWeight;

        [Header("Closed Gesture Parameters")]
        [Tooltip("Threshold to determine if a gesture is closed (after normalization)")]
        public float closedThreshold;
        [Tooltip("Penalty if the closure property of the input and template do not match")]
        public float closedMismatchPenalty;

        /// <summary>
        /// Recognizes the drawn gesture by comparing it with the stored templates.
        /// </summary>
        public SignTemplateIcon Recognize(List<Vector2> inputPoints)
        {
            if (inputPoints == null || inputPoints.Count == 0)
                return null;

            List<Vector2> normalizedInput = usePcaForNormalization ?
                GestureUtils.NormalizePointsPca(inputPoints, resampleCount, smoothingWindow) :
                GestureUtils.NormalizePoints(inputPoints, resampleCount, smoothingWindow);

            float inputAspectRatio = GetBoundingBoxRatio(normalizedInput);
            float inputCircularity = ComputeCircularity(normalizedInput);
            bool inputClosed = IsClosed(normalizedInput);
            float inputCurvature = ComputeAverageCurvature(normalizedInput);
            Debug.Log("Normalized Input: " + Vector2ListToString(normalizedInput) +
                      " | Aspect Ratio: " + inputAspectRatio +
                      " | Circularity: " + inputCircularity +
                      " | Curvature: " + inputCurvature +
                      " | Closed: " + inputClosed);

            SignTemplateIcon bestTemplate = null;
            float bestDistance = float.PositiveInfinity;

            foreach (SignTemplateIcon template in signDatabase.templates)
            {
                if (template == null || template.points == null || template.points.Count == 0)
                    continue;

                List<Vector2> normalizedTemplate = usePcaForNormalization ?
                    GestureUtils.NormalizePointsPca(template.points, resampleCount, smoothingWindow) :
                    GestureUtils.NormalizePoints(template.points, resampleCount, smoothingWindow);

                float templateAspectRatio = GetBoundingBoxRatio(normalizedTemplate);
                float templateCircularity = ComputeCircularity(normalizedTemplate);
                bool templateClosed = IsClosed(normalizedTemplate);
                float templateCurvature = ComputeAverageCurvature(normalizedTemplate);

                float dtwDist = useAlternativeAlgorithm ?
                                  CalculateAverageDistance(normalizedInput, normalizedTemplate) :
                                  CalculateDtwDistance(normalizedInput, normalizedTemplate);

                float aspectDiff = Mathf.Abs(inputAspectRatio - templateAspectRatio);
                float circularityDiff = Mathf.Abs(inputCircularity - templateCircularity);
                float curvatureDiff = Mathf.Abs(inputCurvature - templateCurvature);

                float totalDistance = dtwWeight * dtwDist +
                                      aspectRatioWeight * aspectDiff +
                                      circularityWeight * circularityDiff +
                                      curvatureWeight * curvatureDiff;

                if (inputClosed != templateClosed)
                {
                    totalDistance *= closedMismatchPenalty;
                }

                Debug.Log($"Template {template.id}: DTW = {dtwDist:F3}, Aspect Diff = {aspectDiff:F3}, " +
                          $"Circularity Diff = {circularityDiff:F3}, Curvature Diff = {curvatureDiff:F3}, " +
                          $"Closed: {templateClosed}, Total = {totalDistance:F3}");

                if (totalDistance < bestDistance)
                {
                    bestDistance = totalDistance;
                    bestTemplate = template;
                }
            }

            Debug.Log("Best Distance: " + bestDistance);
            return (bestDistance < recognitionThreshold) ? bestTemplate : null;
        }

        /// <summary>
        /// Checks if the gesture is considered closed.
        /// </summary>
        private bool IsClosed(List<Vector2> points)
        {
            if (points == null || points.Count < 2)
                return false;
            return Vector2.Distance(points[0], points[points.Count - 1]) < closedThreshold;
        }

        /// <summary>
        /// Calculates the Dynamic Time Warping (DTW) distance between two sequences.
        /// </summary>
        private float CalculateDtwDistance(List<Vector2> seqA, List<Vector2> seqB)
        {
            int n = seqA.Count;
            int m = seqB.Count;
            float[][] dtw = new float[n + 1][];
            for (int i = 0; i <= n; i++)
            {
                dtw[i] = new float[m + 1];
                for (int j = 0; j <= m; j++)
                    dtw[i][j] = float.PositiveInfinity;
            }
            dtw[0][0] = 0f;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    float cost = Vector2.Distance(seqA[i - 1], seqB[j - 1]);
                    dtw[i][j] = cost + Mathf.Min(dtw[i - 1][j],
                                                 Mathf.Min(dtw[i][j - 1], dtw[i - 1][j - 1]));
                }
            }
            return dtw[n][m];
        }

        /// <summary>
        /// Calculates the average point-to-point distance if sequences have equal length.
        /// </summary>
        private float CalculateAverageDistance(List<Vector2> seqA, List<Vector2> seqB)
        {
            if (seqA.Count != seqB.Count)
                return float.PositiveInfinity;

            float total = 0f;
            for (int i = 0; i < seqA.Count; i++)
                total += Vector2.Distance(seqA[i], seqB[i]);
            return total / seqA.Count;
        }

        /// <summary>
        /// Computes the aspect ratio of the gesture's bounding box.
        /// </summary>
        private float GetBoundingBoxRatio(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return 1f;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (Vector2 p in points)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }
            float width = maxX - minX;
            float height = maxY - minY;
            if (width < Mathf.Epsilon || height < Mathf.Epsilon)
                return 0f;
            return Mathf.Min(width, height) / Mathf.Max(width, height);
        }

        /// <summary>
        /// Computes the circularity metric of the gesture.
        /// </summary>
        private float ComputeCircularity(List<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return 0f;

            float area = Mathf.Abs(ShoelaceArea(points));
            float perimeter = 0f;
            for (int i = 1; i < points.Count; i++)
                perimeter += Vector2.Distance(points[i - 1], points[i]);
            perimeter += Vector2.Distance(points[points.Count - 1], points[0]);
            if (perimeter < Mathf.Epsilon)
                return 0f;
            return (4 * Mathf.PI * area) / (perimeter * perimeter);
        }

        /// <summary>
        /// Computes the area of the polygon using the Shoelace formula.
        /// </summary>
        private float ShoelaceArea(List<Vector2> points)
        {
            float area = 0f;
            int j = points.Count - 1;
            for (int i = 0; i < points.Count; i++)
            {
                area += (points[j].x + points[i].x) * (points[j].y - points[i].y);
                j = i;
            }
            return area / 2f;
        }

        /// <summary>
        /// Computes the average curvature (angle between segments) in degrees.
        /// </summary>
        private float ComputeAverageCurvature(List<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return 0f;

            float totalAngle = 0f;
            int count = 0;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 a = points[i] - points[i - 1];
                Vector2 b = points[i + 1] - points[i];
                if (a.magnitude < Mathf.Epsilon || b.magnitude < Mathf.Epsilon)
                    continue;
                float angle = Vector2.Angle(a, b);
                totalAngle += angle;
                count++;
            }
            return (count > 0) ? totalAngle / count : 0f;
        }

        /// <summary>
        /// Converts a list of Vector2 points to a formatted string.
        /// </summary>
        private string Vector2ListToString(List<Vector2> points)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (Vector2 p in points)
                sb.Append($"({p.x:F2},{p.y:F2}) ");
            return sb.ToString();
        }
    }
}
