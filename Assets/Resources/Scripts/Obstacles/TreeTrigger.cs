using UnityEngine;
using Resources.Scripts.Player;

namespace Resources.Scripts.Obstacles
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class TreeTrigger : MonoBehaviour
    {
        [Tooltip("Насколько слоёв выше ставить игрока, когда он касается дерева")]
        [SerializeField] private int aboveOffset = 1;
        [Tooltip("Насколько слоёв ниже ставить игрока, когда он уходит из-под дерева")]
        [SerializeField] private int belowOffset = -1;

        private CircleCollider2D _trigger;
        private SpriteRenderer   _renderer;
        private int              _aboveTarget;
        private int              _belowTarget;

        private void Awake()
        {
            _trigger = GetComponent<CircleCollider2D>();
            _trigger.isTrigger = true;

            _renderer = GetComponent<SpriteRenderer>() 
                        ?? GetComponentInChildren<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogError($"{name}: не найден SpriteRenderer!");
                return;
            }

            // начальные заглушки, будут пересчитываться в триггерах
            _aboveTarget = _renderer.sortingOrder + aboveOffset;
            _belowTarget = _renderer.sortingOrder + belowOffset;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            var keeper = other.GetComponent<PlayerSortingKeeper>()
                        ?? other.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            // Пересчитаем так, чтобы гарантированно быть над кроной и над исходным слоем игрока
            _aboveTarget = Mathf.Max(_renderer.sortingOrder, keeper.OriginalOrder) + aboveOffset;
            _belowTarget = Mathf.Min(_renderer.sortingOrder, keeper.OriginalOrder) + belowOffset;

            keeper.ExitTreeBelow(_belowTarget);
            keeper.EnterTreeAbove(_aboveTarget);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            var keeper = other.GetComponent<PlayerSortingKeeper>()
                        ?? other.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            // Снова пересчитаем «над кроной» для корректного ExitTreeAbove
            _aboveTarget = Mathf.Max(_renderer.sortingOrder, keeper.OriginalOrder) + aboveOffset;
            // А здесь — вычислим «под кроной», но не позволим упасть ниже исходного слоя игрока:
            var rawBelow = Mathf.Min(_renderer.sortingOrder, keeper.OriginalOrder) + belowOffset;
            _belowTarget   = Mathf.Max(rawBelow, keeper.OriginalOrder);

            keeper.ExitTreeAbove(_aboveTarget);
            keeper.EnterTreeBelow(_belowTarget);
        }
    }
}
