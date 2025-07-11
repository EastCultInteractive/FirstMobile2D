using UnityEngine;

namespace Resources.Scripts.Entity.Obstacles
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

        private void Awake()
        {
            _trigger = GetComponent<CircleCollider2D>();
            _trigger.isTrigger = true;

            _renderer = GetComponent<SpriteRenderer>() 
                        ?? GetComponentInChildren<SpriteRenderer>();
        }
    }
}
