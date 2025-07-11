using UnityEngine;

namespace Resources.Scripts.Entity.Obstacles
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
    }
}