using UnityEngine;

namespace Resources.Scripts.Entity.Obstacles
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
    }
}