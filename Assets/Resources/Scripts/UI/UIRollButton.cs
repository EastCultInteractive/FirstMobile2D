using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Resources.Scripts.Player;

namespace Resources.Scripts.UI
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

        [Tooltip("Ссылка на PlayerController")]
        public PlayerController player;

        private Button btn;

        private void Awake()
        {
            btn = GetComponent<Button>();
        }

        private void Start()
        {
            // нажатие по кнопке вызывает TryRoll() + анимация “пунча” масштаба
            btn.onClick.AddListener(() =>
            {
                player?.TryRoll();
                btn.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f).SetEase(Ease.OutElastic);
            });

            // подписываемся на изменение кулдауна
            if (player != null)
                player.OnRollCooldownChanged += SetCooldownUI;
        }

        private void OnDestroy()
        {
            if (player != null)
                player.OnRollCooldownChanged -= SetCooldownUI;
        }

        /// <summary>
        /// value от 0 до 1: заполнение radial image
        /// </summary>
        private void SetCooldownUI(float value)
        {
            cooldownImage.fillAmount = value;

            // пересчитываем секунды
            float seconds = value * player.RollCooldownDuration;
            int secInt = Mathf.CeilToInt(seconds);
            cooldownText.text = secInt.ToString();

            // скрываем текст, когда нет кулдауна
            cooldownText.enabled = value > 0f;
        }
    }
}
