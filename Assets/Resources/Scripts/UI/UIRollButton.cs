using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Resources.Scripts.Entity.UI
{
    /// <summary>
    /// Обновляет UI кнопки кувырка: radial fill + текст оставшегося времени.
    /// Также добавляет анимацию нажатия кнопки.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIRollButton : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Radial Image (Fill Method = Radial360)")]
        public Image cooldownImage;

        [Tooltip("TMP Text для отображения оставшихся секунд")]
        public TextMeshProUGUI cooldownText;

        private void SetCooldownUI(float value)
        {
            cooldownImage.fillAmount = value;

            // Пересчитываем секунды
            var secInt = Mathf.CeilToInt(value);
            cooldownText.text = secInt.ToString();

            // Скрываем текст, когда нет кулдауна
            cooldownText.enabled = value > 0f;
        }
    }
}
