using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.Labyrinth
{
    /// <summary>
    /// Draws the solution path on the minimap using a LineRenderer.
    /// Ensure this GameObject is on a layer visible only on the minimap.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LabyrinthMinimapSolutionPathDrawer : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField, Tooltip("Color of the solution path line.")]
        private Color lineColor = Color.green;
        [SerializeField, Tooltip("Width of the solution path line.")]
        private float lineWidth = 0.1f;
        [SerializeField, Tooltip("Material shader to use for the line.")]
        private string shaderName = "Sprites/Default";

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            // Create a new material with the specified shader.
            Material lineMaterial = new Material(Shader.Find(shaderName));
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// Draws the solution path using a list of world-space positions.
        /// </summary>
        /// <param name="positions">List of positions representing the solution path.</param>
        public void DrawSolutionPath(List<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
                return;

            lineRenderer.positionCount = positions.Count;
            for (int i = 0; i < positions.Count; i++)
            {
                lineRenderer.SetPosition(i, positions[i]);
            }
        }
    }
}