using Resources.Scripts.Entity.Player;
using UnityEngine;

namespace Resources.Scripts.Data
{
    public enum PerkType
    {
        ManaRegenAmountIncrease,    // увеличение количества восстанавливаемой маны в секунду
        MaxManaIncrease,            // увеличение маны
        MoveSpeedIncrease,          // скорость передвижения
        EvasionChanceIncrease,      // шанс уклонения
        FairyPullRangeIncrease      // притягивание фей
    }

    public enum PerkQuality { Small, Medium, Large }

    [CreateAssetMenu(fileName = "PerkDefinition", menuName = "GameSettings/Perk Definition", order = 2)]
    public class PerkDefinition : ScriptableObject
    {
        [Header("Основные параметры перка")]
        public PerkType Type;
        public PerkQuality Quality;
        public float Value;  // для процентных и числовых бонусов

        [Header("Шанс выпадения перка")]
        [Tooltip("Вероятность (от 0 до 1), с которой этот перк может выпасть")]
        public float DropChance;

        [Header("Иконка перка")]
        [Tooltip("Иконка, которая будет отображаться в UI при выборе перка")]
        public Sprite Icon;

        [Header("Иконка рамки перка")]
        [Tooltip("Иконка рамки, которая будет отображаться в UI в зависимости от качества перка")]
        public Sprite FrameIcon;

        public string GetDescription()
        {
            return Type switch
            {
                PerkType.ManaRegenAmountIncrease =>
                    $"+{Value:F1} маны к регену ({Quality})",
                PerkType.MaxManaIncrease =>
                    $"+{Value} к макс. мане ({Quality})",
                PerkType.MoveSpeedIncrease =>
                    $"+{Value:F1}% к скорости передвижения ({Quality})",
                PerkType.EvasionChanceIncrease =>
                    $"+{Value:F1}% к шансу уклонения ({Quality})",
                PerkType.FairyPullRangeIncrease =>
                    $"+{Value:F1}% к радиусу притягивания фей ({Quality})",
                _ => string.Empty
            };
        }

        public void Apply(PlayerStatsHandler stats)
        {
            switch (Type)
            {
                case PerkType.ManaRegenAmountIncrease:
                    stats.ModifyManaRegenRate(Value);
                    break;
                case PerkType.MaxManaIncrease:
                    stats.ModifyMaxMana(Value);
                    break;
                case PerkType.MoveSpeedIncrease:
                    stats.ModifyMoveSpeedPercent(Value);
                    break;
                case PerkType.EvasionChanceIncrease:
                    stats.ModifyEvasion(Value);
                    break;
                case PerkType.FairyPullRangeIncrease:
                    stats.ModifyPullRangePercent(Value);
                    break;
            }
        }
    }
}
