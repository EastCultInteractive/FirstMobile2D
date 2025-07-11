using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.SpellMode
{
    [CreateAssetMenu(fileName = "SignTemplateDatabaseIcon", menuName = "SignTemplates/Icon Template Database")]
    public class SignTemplateDatabaseIcon : ScriptableObject
    {
        // List of sign templates
        public List<SignTemplateIcon> templates = new List<SignTemplateIcon>();
    }
}