using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Resources.Scripts.Entity.Data;
using Resources.Scripts.Entity.GameManagers;

namespace Resources.Scripts.Entity.Menu
{
    public class PerkSelectionController : MonoBehaviour
    {
        [Header("Кнопки-опции (по порядку)")]
        [SerializeField] private Button[] optionButtons = null!;
        [SerializeField] private TextMeshProUGUI[] descriptionTexts = null!;

        [Header("Иконки для опций (соответствие кнопкам)")]
        [SerializeField] private Image[] iconImages = null!;

        [Header("Иконки рамок для опций (соответствие кнопкам)")]
        [SerializeField] private Image[] frameImages = null!;

        private PerkDefinition[] options;

        /// <summary>
        /// Вызывается сразу после Instantiate панели.
        /// Добавляет анимацию появления кнопок и подставляет иконки (перка и его рамки),
        /// раскрашивает значение перка в зависимости от качества и оборачивает
        /// «значение + единицу» в <nobr>, чтобы не разбивать слово.
        /// Числовые значения выводятся без десятичных знаков.
        /// </summary>
        public void Setup(PerkDefinition[] perks)
        {
            options = perks;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i >= perks.Length)
                {
                    optionButtons[i].gameObject.SetActive(false);
                    continue;
                }

                var perk = perks[i];
                var btn = optionButtons[i];

                // Выбираем цвет для «+значение» в зависимости от качества перка
                string colorHex = perk.Quality switch
                {
                    PerkQuality.Small  => "#00FF00", // зелёный
                    PerkQuality.Medium => "#FFA500", // оранжевый
                    PerkQuality.Large  => "#E500E5", // светлый фиолетовый
                    _                  => "#FFFFFF"  // белый по умолчанию
                };

                // Формируем описательный текст.
                // «<nobr><color=...>+VALUE</color> ЕДИНИЦА</nobr>» гарантирует,
                // что «+VALUE ЕДИНИЦА» не разобьётся на части.
                string description = perk.Type switch
                {
                    PerkType.ManaRegenAmountIncrease =>
                        $"<nobr><color={colorHex}>+{perk.Value:0}</color> маны</nobr> к регену",

                    PerkType.MaxManaIncrease =>
                        $"<nobr><color={colorHex}>+{perk.Value:0}</color> к макс. мане</nobr>",

                    PerkType.MoveSpeedIncrease =>
                        $"<nobr><color={colorHex}>+{perk.Value:0}%</color> к скорости</nobr> передвижения",

                    PerkType.EvasionChanceIncrease =>
                        $"<nobr><color={colorHex}>+{perk.Value:0}%</color> к шансу</nobr> уклонения",

                    PerkType.FairyPullRangeIncrease =>
                        $"<nobr><color={colorHex}>+{perk.Value:0}%</color> к радиусу</nobr> притягивания фей",

                    _ => string.Empty
                };

                descriptionTexts[i].text = description;

                // Устанавливаем иконку перка
                if (i < iconImages.Length && perk.Icon != null)
                {
                    iconImages[i].sprite = perk.Icon;
                    iconImages[i].SetNativeSize();
                }

                // Устанавливаем иконку рамки перка
                if (i < frameImages.Length && perk.FrameIcon != null)
                {
                    frameImages[i].sprite = perk.FrameIcon;
                    frameImages[i].SetNativeSize();
                }

                int idx = i; // для замыкания

                // Подготовка к анимации
                btn.transform.localScale = Vector3.zero;
                btn.onClick.AddListener(() =>
                {
                    StageProgressionManager.Instance.OnPerkChosen(options[idx]);
                    Destroy(gameObject);
                });

                // Анимация появления с задержкой
                btn.transform
                    .DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.1f * i);
            }
        }
    }
}
