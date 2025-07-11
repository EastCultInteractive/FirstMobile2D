using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.SpellMode
{
    public static class GestureUtils
    {
        /// <summary>
        /// Normalizes a gesture by smoothing, resampling, rotating (using indicative angle), scaling, and translating.
        /// Use this method if PCA normalization is not required.
        /// </summary>
        public static List<Vector2> NormalizePoints(List<Vector2> points, int resampleCount, int smoothingWindow, bool normalizeRotation = true)
        {
            if (points == null || points.Count == 0)
                return new List<Vector2>();

            List<Vector2> smoothed = Smoothing(points, smoothingWindow);
            List<Vector2> resampled = Resample(smoothed, resampleCount);
            if (normalizeRotation)
            {
                float angle = IndicativeAngle(resampled);
                resampled = RotateBy(resampled, -angle);
            }
            List<Vector2> scaled = ScaleToUnit(resampled);
            List<Vector2> translated = TranslateToOrigin(scaled);
            return translated;
        }

        /// <summary>
        /// Normalizes a gesture using PCA to determine the principal axis.
        /// </summary>
        public static List<Vector2> NormalizePointsPca(List<Vector2> points, int resampleCount, int smoothingWindow)
        {
            if (points == null || points.Count == 0)
                return new List<Vector2>();

            List<Vector2> smoothed = Smoothing(points, smoothingWindow);
            List<Vector2> resampled = Resample(smoothed, resampleCount);
            float angle = ComputePrincipalAngle(resampled);
            List<Vector2> rotated = RotateBy(resampled, -angle);
            List<Vector2> scaled = ScaleToUnit(rotated);
            List<Vector2> translated = TranslateToOrigin(scaled);
            return translated;
        }

        /// <summary>
        /// Computes the principal (PCA) angle for a set of points.
        /// </summary>
        public static float ComputePrincipalAngle(List<Vector2> points)
        {
            Vector2 centroid = Centroid(points);
            float sumXx = 0f, sumXY = 0f, sumYy = 0f;
            foreach (Vector2 p in points)
            {
                Vector2 d = p - centroid;
                sumXx += d.x * d.x;
                sumXY += d.x * d.y;
                sumYy += d.y * d.y;
            }
            float theta = 0.5f * Mathf.Atan2(2 * sumXY, sumXx - sumYy);
            return theta;
        }

        /// <summary>
        /// Smoothes the gesture using a moving average with the specified window size.
        /// </summary>
        public static List<Vector2> Smoothing(List<Vector2> points, int windowSize)
        {
            if (windowSize <= 1 || points.Count < windowSize)
                return new List<Vector2>(points);

            List<Vector2> smoothed = new List<Vector2>();
            int count = points.Count;
            int halfWindow = windowSize / 2;
            for (int i = 0; i < count; i++)
            {
                Vector2 sum = Vector2.zero;
                int num = 0;
                for (int j = i - halfWindow; j <= i + halfWindow; j++)
                {
                    if (j >= 0 && j < count)
                    {
                        sum += points[j];
                        num++;
                    }
                }
                smoothed.Add(sum / num);
            }
            return smoothed;
        }

        /// <summary>
        /// Resamples the gesture to a fixed number of points.
        /// </summary>
        public static List<Vector2> Resample(List<Vector2> points, int n)
        {
            List<Vector2> newPoints = new List<Vector2>();
            if (points.Count == 0)
                return newPoints;

            newPoints.Add(points[0]);
            float totalLength = PathLength(points);
            if (Mathf.Approximately(totalLength, 0f))
            {
                for (int i = 1; i < n; i++)
                    newPoints.Add(points[0]);
                return newPoints;
            }
            float interval = totalLength / (n - 1);
            float dAccum = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                float segmentDistance = Vector2.Distance(points[i - 1], points[i]);
                if (Mathf.Approximately(segmentDistance, 0f))
                    continue;
                if (dAccum + segmentDistance >= interval)
                {
                    float t = (interval - dAccum) / segmentDistance;
                    Vector2 newPoint = Vector2.Lerp(points[i - 1], points[i], t);
                    newPoints.Add(newPoint);
                    points.Insert(i, newPoint);
                    dAccum = 0f;
                }
                else
                {
                    dAccum += segmentDistance;
                }
            }
            while (newPoints.Count < n)
                newPoints.Add(points[points.Count - 1]);

            return newPoints;
        }

        /// <summary>
        /// Computes the total path length of the gesture.
        /// </summary>
        public static float PathLength(List<Vector2> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
                length += Vector2.Distance(points[i - 1], points[i]);
            return length;
        }

        /// <summary>
        /// Computes the indicative angle from the centroid to the first point.
        /// </summary>
        public static float IndicativeAngle(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return 0f;
            Vector2 centroid = Centroid(points);
            return Mathf.Atan2(points[0].y - centroid.y, points[0].x - centroid.x);
        }

        /// <summary>
        /// Rotates all points by a given angle.
        /// </summary>
        public static List<Vector2> RotateBy(List<Vector2> points, float angle)
        {
            List<Vector2> rotated = new List<Vector2>();
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            foreach (Vector2 p in points)
            {
                float x = p.x * cos - p.y * sin;
                float y = p.x * sin + p.y * cos;
                rotated.Add(new Vector2(x, y));
            }
            return rotated;
        }

        /// <summary>
        /// Scales the gesture to fit within a unit square.
        /// </summary>
        public static List<Vector2> ScaleToUnit(List<Vector2> points)
        {
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
            float scale = Mathf.Max(width, height);
            List<Vector2> scaled = new List<Vector2>();
            if (scale > 0)
            {
                foreach (Vector2 p in points)
                    scaled.Add(p / scale);
            }
            else
            {
                scaled.AddRange(points);
            }
            return scaled;
        }

        /// <summary>
        /// Translates the gesture so that its centroid is at the origin.
        /// </summary>
        public static List<Vector2> TranslateToOrigin(List<Vector2> points)
        {
            Vector2 centroid = Centroid(points);
            List<Vector2> translated = new List<Vector2>();
            foreach (Vector2 p in points)
                translated.Add(p - centroid);
            return translated;
        }

        /// <summary>
        /// Computes the centroid (average position) of the points.
        /// </summary>
        public static Vector2 Centroid(List<Vector2> points)
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 p in points)
                sum += p;
            return sum / points.Count;
        }
    }
}
