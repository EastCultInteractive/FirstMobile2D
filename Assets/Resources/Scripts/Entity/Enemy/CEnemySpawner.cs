using UnityEngine;
using System.Collections;
using Resources.Scripts.GameManagers;

namespace Resources.Scripts.Enemy
{
    public class CEnemySpawner : MonoBehaviour
    {
        [Header("Spawning Settings")]
        [SerializeField, Tooltip("Prefab of the enemy to spawn.")]          private GameObject enemyPrefab;
        [SerializeField, Tooltip("Time interval between enemy spawns (in seconds).")] private float spawnInterval = 5f;
        [SerializeField, Tooltip("Maximum number of enemies allowed at once.")]      private int maxEnemies = 10;
        [SerializeField, Tooltip("Radius of the spawn area.")]                     private float spawnRadius = 10f;
        [SerializeField, Tooltip("Enable random spawn positions within the spawn area.")] private bool randomizeSpawnPosition = true;

        [Header("Spawn Constraints")]
        [SerializeField, Tooltip("Minimum distance from the player allowed for enemy spawn.")] private float minDistanceFromPlayer = 5f;
        [SerializeField, Tooltip("Minimum distance between spawned enemies.")]                private float minDistanceBetweenEnemies = 2f;
        [SerializeField, Tooltip("Maximum number of attempts to generate a valid spawn position.")] private int maxSpawnAttempts = 10;
        [SerializeField, Tooltip("Layer mask for enemy objects (to check spacing between enemies).")] private LayerMask enemyLayerMask;

        [Header("Debug Settings")]
        [SerializeField, Tooltip("Enable debug logging for enemy spawning.")] private bool debugLog;

        private int currentEnemyCount;
        private Transform playerTransform;

        private void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else if (debugLog) Debug.LogWarning("Player object not found. Check the tag 'Player'.");

            StartCoroutine(SpawnEnemyLoop());
        }

        private IEnumerator SpawnEnemyLoop()
        {
            while (true)
            {
                if (currentEnemyCount < maxEnemies && enemyPrefab != null)
                    SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private Vector3 GetValidSpawnPosition()
        {
            var candidatePos = transform.position;
            int attempts = 0;
            bool valid = false;

            while (attempts < maxSpawnAttempts && !valid)
            {
                if (randomizeSpawnPosition)
                {
                    var off = Random.insideUnitCircle * spawnRadius;
                    candidatePos = transform.position + new Vector3(off.x, off.y, 0f);
                }
                else candidatePos = transform.position;

                valid = true;

                if (playerTransform != null)
                {
                    float dist = Vector3.Distance(candidatePos, playerTransform.position);
                    if (dist < minDistanceFromPlayer)
                    {
                        valid = false;
                        if (debugLog) Debug.Log($"Too close to player ({dist:F1}). Pos: {candidatePos}");
                    }
                }

                var hit = Physics2D.OverlapCircle(candidatePos, minDistanceBetweenEnemies, enemyLayerMask);
                if (hit != null)
                {
                    valid = false;
                    if (debugLog) Debug.Log($"Too close to enemy '{hit.name}'. Pos: {candidatePos}");
                }

                attempts++;
            }

            if (!valid && debugLog)
                Debug.LogWarning($"No valid spawn pos after {attempts} attempts, using fallback.");
            return candidatePos;
        }

        private void SpawnEnemy()
        {
            var pos = GetValidSpawnPosition();
            CharacterScaleManager.Factory.CreateCharacter(enemyPrefab, pos, Quaternion.identity);
            currentEnemyCount++;
            if (debugLog) Debug.Log($"Spawned enemy at {pos}. Count: {currentEnemyCount}");
        }

        public void OnEnemyDestroyed()
        {
            currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
        }
    }
}
