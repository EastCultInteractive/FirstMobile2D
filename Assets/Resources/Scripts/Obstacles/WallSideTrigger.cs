using UnityEngine;
using Resources.Scripts.Player;

namespace Resources.Scripts.Obstacles
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WallSideTrigger : MonoBehaviour
    {
        [Header("Смещение при касании сбоку (Side)")]
        [Tooltip("Насколько слоёв ниже ставить игрока при касании стены сбоку")]
        [SerializeField] private int sideOffset = -1;

        private BoxCollider2D  _collider;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = false;

            _renderer = GetComponent<SpriteRenderer>()
                        ?? GetComponentInChildren<SpriteRenderer>();
            if (_renderer == null)
                Debug.LogError($"{name}: не найден SpriteRenderer!");
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!col.collider.CompareTag("Player")) return;
            var keeper = col.collider.GetComponent<PlayerSortingKeeper>()
                         ?? col.collider.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            int target = _renderer.sortingOrder + sideOffset;
            keeper.EnterSide(target);
        }

        private void OnCollisionExit2D(Collision2D col)
        {
            if (!col.collider.CompareTag("Player")) return;
            var keeper = col.collider.GetComponent<PlayerSortingKeeper>()
                         ?? col.collider.GetComponentInChildren<PlayerSortingKeeper>();
            if (keeper == null) return;

            int target = _renderer.sortingOrder + sideOffset;
            keeper.ExitSide(target);
        }
    }
}