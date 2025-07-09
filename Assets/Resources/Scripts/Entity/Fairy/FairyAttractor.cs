using Resources.Scripts.Entity.Player;
using UnityEngine;

namespace Resources.Scripts.Fairy
{
    /// <summary>
    /// Пассивное притягивание фей: активирует притягивание и учитывает в статистике.
    /// Радиус всасывания теперь берет значение из PlayerStatsHandler.PullRange,
    /// которое можно изменять через перк FairyPullRangeIncrease.
    /// </summary>
    [RequireComponent(typeof(PlayerStatsHandler))]
    public class FairyAttractor : MonoBehaviour
    {
        [Header("Pull Settings")]
        [SerializeField, Tooltip("Скорость перемещения фей к игроку")]
        private float pullSpeed = 10f;

        [SerializeField, Tooltip("Минимальное расстояние до игрока для поглощения феи")]
        private float absorbRadius = 0.2f;

        private PlayerStatsHandler stats;

        private void Awake()
        {
            stats = GetComponent<PlayerStatsHandler>();
        }

        private void Update()
        {
            // Диапазон всасывания теперь управляется stats.PullRange,
            // и будет увеличиваться при перке FairyPullRangeIncrease
            float pullRadius = stats.PullRange;

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                pullRadius,
                LayerMask.GetMask("Fairy")
            );

            foreach (var col in hits)
            {
                var fairy = col.GetComponent<FairyController>();
                if (fairy == null) continue;

                float dist = Vector3.Distance(fairy.transform.position, transform.position);

                if (dist > absorbRadius)
                {
                    fairy.PullTo(transform.position, pullSpeed);
                }
                else
                {
                    stats.FairyCount++;
                    stats.RestoreMana(20f);
                    fairy.DestroyFairy();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (stats != null)
            {
                // показываем текущий радиус всасывания из stats.PullRange
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, stats.PullRange);

                // показываем радиус поглощения
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, absorbRadius);
            }
        }
    }
}
