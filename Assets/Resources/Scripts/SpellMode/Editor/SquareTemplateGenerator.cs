#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Resources.Scripts.Entity.SpellMode.Editor
{
    /// <summary>
    /// Editor utility for generating a Square template asset.
    /// </summary>
    public static class SquareTemplateGenerator
    {
        [MenuItem("Tools/Generate Square Template")]
        public static void GenerateSquareTemplate()
        {
            // Generate points along the perimeter of a square using IdealSignTemplates.
            // Default pointCount (64) is used.
            List<Vector2> points = IdealSignTemplates.GetSquareTemplate();
            const string path = "Assets/SquareTemplate.asset";
            SignTemplateIcon template = AssetDatabase.LoadAssetAtPath<SignTemplateIcon>(path);
            if (template == null)
            {
                template = ScriptableObject.CreateInstance<SignTemplateIcon>();
                template.id = "Square";
                AssetDatabase.CreateAsset(template, path);
            }
            template.points = points;
            EditorUtility.SetDirty(template);
            AssetDatabase.SaveAssets();
            Debug.Log($"Square template generated with {points.Count} points.");
        }
    }
}
#endif