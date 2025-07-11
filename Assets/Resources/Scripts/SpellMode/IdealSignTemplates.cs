using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.SpellMode
{
    public static class IdealSignTemplates
    {
        /// <summary>
        /// Generates an ideal circle template with evenly distributed points.
        /// </summary>
        /// <param name="pointCount">Number of points to generate.</param>
        /// <returns>List of Vector2 points representing a circle.</returns>
        public static List<Vector2> GetCircleTemplate(int pointCount = 64)
        {
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (2 * Mathf.PI * i) / pointCount;
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);
                points.Add(new Vector2(x, y));
            }

            return points;
        }

        /// <summary>
        /// Generates an ideal square template.
        /// The square spans from -1 to 1 on both axes.
        /// Points are evenly distributed along the perimeter.
        /// </summary>
        public static List<Vector2> GetSquareTemplate(int pointCount = 64)
        {
            List<Vector2> points = new List<Vector2>();
            int pointsPerEdge = pointCount / 4;

            // Top edge: from (-1, 1) to (1, 1)
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(new Vector2(Mathf.Lerp(-1, 1, t), 1));
            }

            // Right edge: from (1, 1) to (1, -1)
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(new Vector2(1, Mathf.Lerp(1, -1, t)));
            }

            // Bottom edge: from (1, -1) to (-1, -1)
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(new Vector2(Mathf.Lerp(1, -1, t), -1));
            }

            // Left edge: from (-1, -1) to (-1, 1)
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(new Vector2(-1, Mathf.Lerp(-1, 1, t)));
            }

            return points;
        }

        /// <summary>
        /// Generates an ideal triangle template.
        /// Triangle vertices are: (0,1), (-1,-1) and (1,-1).
        /// Points are evenly distributed along the perimeter.
        /// </summary>
        public static List<Vector2> GetTriangleTemplate(int pointCount = 64)
        {
            List<Vector2> points = new List<Vector2>();
            int pointsPerEdge = pointCount / 3;
            Vector2 top = new Vector2(0, 1);
            Vector2 left = new Vector2(-1, -1);
            Vector2 right = new Vector2(1, -1);

            // Edge from top to left
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(Vector2.Lerp(top, left, t));
            }

            // Edge from left to right
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(Vector2.Lerp(left, right, t));
            }

            // Edge from right to top
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)(pointsPerEdge - 1);
                points.Add(Vector2.Lerp(right, top, t));
            }

            return points;
        }

        /// <summary>
        /// Generates an ideal template for the letter 'Z'.
        /// The template is made of three segments: top horizontal, diagonal, and bottom horizontal.
        /// </summary>
        public static List<Vector2> GetZTemplate(int pointCount = 64)
        {
            List<Vector2> points = new List<Vector2>();
            int pointsPerSegment = pointCount / 3;

            // Top horizontal line: from (-1,1) to (1,1)
            for (int i = 0; i < pointsPerSegment; i++)
            {
                float t = i / (float)(pointsPerSegment - 1);
                points.Add(new Vector2(Mathf.Lerp(-1, 1, t), 1));
            }

            // Diagonal: from (1,1) to (-1,-1)
            for (int i = 0; i < pointsPerSegment; i++)
            {
                float t = i / (float)(pointsPerSegment - 1);
                points.Add(new Vector2(Mathf.Lerp(1, -1, t), Mathf.Lerp(1, -1, t)));
            }

            // Bottom horizontal line: from (-1,-1) to (1,-1)
            for (int i = 0; i < pointsPerSegment; i++)
            {
                float t = i / (float)(pointsPerSegment - 1);
                points.Add(new Vector2(Mathf.Lerp(-1, 1, t), -1));
            }

            return points;
        }

        /// <summary>
        /// Generates an ideal 'S' template using a parametric equation.
        /// The curve starts at (0,1) and ends at (0,-1) forming an S-shape.
        /// </summary>
        public static List<Vector2> GetSTemplate(int pointCount = 64)
        {
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float x = 0.5f * Mathf.Sin(2 * Mathf.PI * t);
                float y = 1 - 2 * t;
                points.Add(new Vector2(x, y));
            }

            return points;
        }

        /// <summary>
        /// Generates an ideal 5-point star template (5-pointed star).
        /// Alternates between outer radius (1) and inner radius (0.5).
        /// </summary>
        public static List<Vector2> GetStarTemplate(int pointCount = 10)
        {
            List<Vector2> points = new List<Vector2>();
            float outerRadius = 1f;
            float innerRadius = 0.5f;
            float angleStep = 2 * Mathf.PI / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * angleStep;
                float r = (i % 2 == 0) ? outerRadius : innerRadius;
                points.Add(new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle)));
            }

            return points;
        }
    }
}
