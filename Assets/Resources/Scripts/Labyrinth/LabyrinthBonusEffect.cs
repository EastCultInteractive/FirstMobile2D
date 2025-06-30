using UnityEngine;
using Resources.Scripts.Player;

namespace Resources.Scripts.Labyrinth
{
    public class LabyrinthBonusEffect : MonoBehaviour
    {
        [Header("Bonus Settings")]
        [SerializeField, Range(1f, 5f)] private float multiplier = 1.5f;
        [SerializeField, Range(1f, 5f)] private float duration = 1.5f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;
            
            player.ApplySpeedBoost(multiplier, duration);
            Destroy(gameObject);
        }
    }
}