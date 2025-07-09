using Resources.Scripts.GameManagers;
using UnityEngine;

namespace Resources.Scripts.Labyrinth
{
    [RequireComponent(typeof(Collider2D))]
    public class LabyrinthFinishTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) 
                return;

            // Игрок достиг финиша — переход на следующий этап
            if (StageProgressionManager.Instance != null)
            {
                StageProgressionManager.Instance.OnLabyrinthCompleted();
            }
            else
            {
                Debug.LogWarning("StageProgressionManager.Instance is null on labyrinth finish!");
            }
        }
    }
}