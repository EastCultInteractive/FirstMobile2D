using Resources.Scripts.Entity.Player;
using UnityEngine;

namespace Resources.Scripts.Obstacles
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WallBottomTriggerBehaviour : MonoBehaviour
    {
        [Header("Смещение при касании снизу (Bottom)")]
        [Tooltip("Насколько слоёв выше ставить игрока, когда он внизу стены")]
        [SerializeField] private int bottomOffset = 1;

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

            // ставим игрока над стеной, но не ниже своего оригинального слоя
            int target = Mathf.Max(_renderer.sortingOrder, keeper.OriginalOrder) + bottomOffset;
            keeper.EnterBottom(target);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var keeper = other.GetComponent<PlayerSortingKeeper>()
                         ?? other.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            int target = Mathf.Max(_renderer.sortingOrder, keeper.OriginalOrder) + bottomOffset;
            keeper.ExitBottom(target);
        }
    }
}