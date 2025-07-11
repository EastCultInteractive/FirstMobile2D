#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Resources.Scripts.Entity.SpellMode.Editor
{
    /// <summary>
    /// Editor utility for generating a Circle template asset.
    /// </summary>
    public static class CircleTemplateGenerator
    {
        [MenuItem("Tools/Generate Circle Template")]
        public static void GenerateCircleTemplate()
        {
            // Generate points on a circle using IdealSignTemplates.
            // Default pointCount (64) is used.
            List<Vector2> points = IdealSignTemplates.GetCircleTemplate();
            const string path = "Assets/CircleTemplate.asset";
            SignTemplateIcon template = AssetDatabase.LoadAssetAtPath<SignTemplateIcon>(path);
            if (template == null)
            {
                template = ScriptableObject.CreateInstance<SignTemplateIcon>();
                template.id = "Circle";
                AssetDatabase.CreateAsset(template, path);
            }
            template.points = points;
            EditorUtility.SetDirty(template);
            AssetDatabase.SaveAssets();
            Debug.Log($"Circle template generated with {points.Count} points.");
        }
    }
}
#endif