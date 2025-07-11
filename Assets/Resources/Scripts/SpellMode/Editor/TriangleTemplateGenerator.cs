#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Resources.Scripts.Entity.SpellMode.Editor
{
    /// <summary>
    /// Editor utility for generating a Triangle template asset.
    /// </summary>
    public static class TriangleTemplateGenerator
    {
        [MenuItem("Tools/Generate Triangle Template")]
        public static void GenerateTriangleTemplate()
        {
            // Generate points along the perimeter of a triangle using IdealSignTemplates.
            // Default pointCount (64) is used.
            List<Vector2> points = IdealSignTemplates.GetTriangleTemplate();
            const string path = "Assets/TriangleTemplate.asset";
            SignTemplateIcon template = AssetDatabase.LoadAssetAtPath<SignTemplateIcon>(path);
            if (template == null)
            {
                template = ScriptableObject.CreateInstance<SignTemplateIcon>();
                template.id = "Triangle";
                AssetDatabase.CreateAsset(template, path);
            }
            template.points = points;
            EditorUtility.SetDirty(template);
            AssetDatabase.SaveAssets();
            Debug.Log($"Triangle template generated with {points.Count} points.");
        }
    }
}
#endif