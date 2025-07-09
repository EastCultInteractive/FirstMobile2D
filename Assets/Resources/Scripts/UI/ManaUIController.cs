using Resources.Scripts.Entity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Resources.Scripts.UI
{
    /// <summary>
    /// Обновляет UI заполнения маны и текст.
    /// </summary>
    public class ManaUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField, Tooltip("Image с Fill Amount для маны")]
        private Image manaFillImage;
        [SerializeField, Tooltip("Текст в формате current/max")]
        private Text manaText;

        [Header("Player Stats")]
        [SerializeField, Tooltip("Ссылка на PlayerStatsHandler")]
        private PlayerStatsHandler playerStats;

        private void Update()
        {
            if (playerStats == null || manaFillImage == null || manaText == null)
                return;

            float fill = playerStats.CurrentMana / playerStats.MaxMana;
            manaFillImage.fillAmount = fill;
            manaText.text = $"{(int)playerStats.CurrentMana}/{(int)playerStats.MaxMana}";
        }
    }
}