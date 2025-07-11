using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.SpellMode
{
    /// <summary>
    /// Represents a sign template icon used in Spell Mode.
    /// Contains an identifier, icon, contour points for the sign, an associated skill prefab,
    /// and a configurable mana cost.
    /// </summary>
    [CreateAssetMenu(fileName = "SignTemplateIcon", menuName = "SignTemplates/Icon Template")]
    public class SignTemplateIcon : ScriptableObject
    {
        [Header("Sign Template Settings")]
        [Tooltip("Unique identifier for the sign template.")]
        public string id;
        
        [Tooltip("Icon representing the sign template.")]
        public Sprite icon;

        [Tooltip("Normalized sign contour points for visual representation or collision detection.")]
        public List<Vector2> points;

        [Header("Skill Association")]
        [Tooltip("Prefab of the skill associated with this sign template.")]
        public GameObject skillPrefab;

        [Header("Mana Cost")]
        [Tooltip("Required mana cost to use this sign.")]
        public float manaCost = 10f; // Настраиваемое значение через Inspector
    }
}