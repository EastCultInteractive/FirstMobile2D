using UnityEngine;
using Resources.Scripts.Player;

namespace Resources.Scripts.Obstacles
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WallUpTriggerBehaviour : MonoBehaviour
    {
        [Header("Смещение при нахождении за стеной (Up)")]
        [Tooltip("Насколько слоёв ниже ставить игрока, когда он за стеной")]
        [SerializeField] private int upOffset = -1;

        private BoxCollider2D _trigger;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider2D>();
            _trigger.isTrigger = true;

            _renderer = GetComponent<SpriteRenderer>()
                        ?? GetComponentInChildren<SpriteRenderer>();
            if (_renderer == null)
                Debug.LogError($"{name}: не найден SpriteRenderer!");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var keeper = other.GetComponent<PlayerSortingKeeper>()
                         ?? other.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            int target = _renderer.sortingOrder + upOffset;
            keeper.EnterUp(target);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var keeper = other.GetComponent<PlayerSortingKeeper>()
                         ?? other.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            int target = _renderer.sortingOrder + upOffset;
            keeper.ExitUp(target);
        }
    }
}