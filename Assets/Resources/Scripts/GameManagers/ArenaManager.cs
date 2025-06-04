using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Resources.Scripts.Data;
using UnityEngine.UI;
using Resources.Scripts.Tilemap;

namespace Resources.Scripts.GameManagers
{
    public class ArenaManager : MonoBehaviour
    {
        [Header("Default Settings (fallback if no stage is selected)")]
        [SerializeField] private ArenaSettings defaultArenaSettings = null!;

        [Header("UI Timer")]
        [SerializeField] private Text timerText = null!;
        [SerializeField] private RectTransform clockHand = null!;

        [Header("Spawn Parameters")]
        [Tooltip("Half size of the spawn area (e.g., 50 means range from -50 to 50)")]
        [SerializeField] private float spawnArea = 50f;

        private ArenaSettings currentSettings;
        private float timer;
        private bool playerSurvived;
        private List<Vector3> obstacleSpawnPositions = new();
        private Transform edgeTreesParent;

        private void Start()
        {
            // 1) Берём настройки арены (либо через StageProgressionManager, либо дефолт)
            currentSettings = StageProgressionManager.CurrentArenaSettings
                              ?? defaultArenaSettings;

            // 2) Инициализируем Tilemap на сцене (ищем существующий объект с RandomTilemapGenerator)
            InitializeTilemap();

            // 3) Настраиваем таймер и UI
            timer = currentSettings.survivalTime;
            if (clockHand != null)
            {
                clockHand.localRotation = Quaternion.Euler(0f, 0f, -90f);
                clockHand
                    .DOLocalRotate(new Vector3(0f, 0f, -90f), 0f)
                    .SetId(this);
            }

            if (timerText != null)
            {
                var cg = timerText.GetComponent<CanvasGroup>()
                         ?? timerText.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg
                  .DOFade(1f, 0.5f)
                  .SetEase(Ease.InQuad)
                  .SetId(this);
            }

            // 4) Старт логики спавна
            InitializeArena();

            // 5) Если нужно, сажаем деревья по краю
            if (currentSettings.plantTreesAtEdges)
            {
                edgeTreesParent = new GameObject("EdgeTrees").transform;
                edgeTreesParent.SetParent(transform, false);
                PlantEdgeTrees();
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>
        /// Находит RandomTilemapGenerator на сцене, передаёт ему tilesForThisArena и размеры, а затем вызывает генерацию.
        /// </summary>
        private void InitializeTilemap()
        {
            // 1) Если в настройках вообще нет массива tilesForThisArena, ничего не делаем
            if (currentSettings.tilesForThisArena == null || currentSettings.tilesForThisArena.Length == 0)
            {
                Debug.LogWarning("ArenaManager: tilesForThisArena не назначен или пуст!");
                return;
            }

            // 2) Ищем компонент RandomTilemapGenerator где-то в иерархии сцены "Arena"
            var generator = Object.FindFirstObjectByType<RandomTilemapGenerator>();
            if (generator == null)
            {
                Debug.LogError("ArenaManager: на сцене не найден RandomTilemapGenerator!");
                return;
            }

            // 3) Назначаем массив тайлов (floorTiles) и размеры (width, height)
            generator.floorTiles = currentSettings.tilesForThisArena;
            generator.width      = Mathf.Max(1, currentSettings.tilemapWidth);
            generator.height     = Mathf.Max(1, currentSettings.tilemapHeight);

            // 4) Ищем компонент Tilemap на том же GameObject или в его детях
            var tilemapComp = generator.tilemapComponent
                              ?? generator.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();

            if (tilemapComp == null)
            {
                Debug.LogError("ArenaManager: RandomTilemapGenerator.tilemapComponent не назначен и не найден в children!");
                return;
            }

            generator.tilemapComponent = tilemapComp;

            // 5) Вызываем генерацию
            generator.GenerateRandomMap();
        }

        /// <summary>
        /// Стартует спавн врагов, фей и препятствий.
        /// </summary>
        private void InitializeArena()
        {
            Debug.Log("Arena initialized.");
            SpawnEnemies();
            SpawnFairies();
            SpawnObstacles();
        }

        private void SpawnEnemies()
        {
            if (currentSettings.enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("Enemy prefabs not assigned.");
                return;
            }
            for (int i = 0; i < currentSettings.enemyCount; i++)
            {
                var prefab = currentSettings.enemyPrefabs.RandomElement();
                Instantiate(prefab, GetRandomPosition(), Quaternion.identity);
            }
        }

        private void SpawnFairies()
        {
            if (currentSettings.fairyPrefabs.Length == 0)
            {
                Debug.LogWarning("Fairy prefabs not assigned.");
                return;
            }
            for (int i = 0; i < currentSettings.fairyCount; i++)
            {
                var prefab = currentSettings.fairyPrefabs.RandomElement();
                Instantiate(prefab, GetRandomPosition(), Quaternion.identity);
            }
        }

        private void SpawnObstacles()
        {
            var types = currentSettings.obstacleTypes;
            if (types == null || types.Length == 0)
            {
                Debug.LogWarning("Obstacle types not set.");
                return;
            }

            float region = spawnArea * 2f;
            obstacleSpawnPositions =
                GeneratePoissonPoints(currentSettings.obstacleMinDistance, region, 30);

            int required = types.Sum(os =>
                Random.Range(os.minCount, os.maxCount + 1));
            if (obstacleSpawnPositions.Count < required)
                Debug.LogWarning(
                    $"Available positions: {obstacleSpawnPositions.Count}, required: {required}"
                );

            foreach (var os in types)
            {
                int count = Random.Range(os.minCount, os.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    if (Random.value > os.spawnProbability ||
                        obstacleSpawnPositions.Count == 0)
                        continue;

                    int index = Random.Range(0, obstacleSpawnPositions.Count);
                    var pos = obstacleSpawnPositions[index];
                    obstacleSpawnPositions.RemoveAt(index);

                    var instance = Instantiate(os.obstaclePrefab, pos, Quaternion.identity);
                    float scale = Random.Range(os.minScale, os.maxScale);
                    instance.transform.localScale = Vector3.one * scale;
                }
            }
        }

        private Vector3 GetRandomPosition() =>
            new Vector3(
                Random.Range(-spawnArea, spawnArea),
                Random.Range(-spawnArea, spawnArea),
                0f
            );

        private List<Vector3> GeneratePoissonPoints(
            float minDist,
            float size,
            int attempts
        )
        {
            var points = new List<Vector3>();
            var spawnPoints = new List<Vector2>();

            var first = new Vector2(
                Random.Range(-size / 2f, size / 2f),
                Random.Range(-size / 2f, size / 2f)
            );

            points.Add(first.ToVector3());
            spawnPoints.Add(first);

            while (spawnPoints.Count > 0)
            {
                int index = Random.Range(0, spawnPoints.Count);
                var center = spawnPoints[index];
                bool accepted = false;

                for (int t = 0; t < attempts; t++)
                {
                    float angle = Random.value * Mathf.PI * 2f;
                    var candidate = center +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                        Random.Range(minDist, 2 * minDist);

                    if (Mathf.Abs(candidate.x) > size / 2f ||
                        Mathf.Abs(candidate.y) > size / 2f)
                        continue;

                    if (points.Any(p =>
                        Vector2.Distance(new Vector2(p.x, p.y), candidate) < minDist
                    ))
                        continue;

                    points.Add(candidate.ToVector3());
                    spawnPoints.Add(candidate);
                    accepted = true;
                    break;
                }

                if (!accepted)
                    spawnPoints.RemoveAt(index);
            }

            return points;
        }

        private void PlantEdgeTrees()
        {
            var types = currentSettings.obstacleTypes;
            if (types == null || types.Length == 0)
            {
                Debug.LogWarning("At least one obstacle type is required for edge trees.");
                return;
            }

            var prefabs = types.Select(t => t.obstaclePrefab).ToArray();
            float half = spawnArea;
            int perSide = currentSettings.edgeTreesPerSide;
            int maxAttempts = perSide * 20;
            int totalPlaced = 0;

            for (int side = 0; side < 4; side++)
            {
                int placed = 0, attempts = 0;

                while (placed < perSide && attempts < maxAttempts)
                {
                    attempts++;

                    Vector3 pos = side switch
                    {
                        0 => new Vector3(
                            Random.Range(-half, half),
                            Random.Range(half - currentSettings.edgeForestThickness, half),
                            0f
                        ),
                        1 => new Vector3(
                            Random.Range(-half, half),
                            Random.Range(-half, -half + currentSettings.edgeForestThickness),
                            0f
                        ),
                        2 => new Vector3(
                            Random.Range(-half, -half + currentSettings.edgeForestThickness),
                            Random.Range(-half, half),
                            0f
                        ),
                        _ => new Vector3(
                            Random.Range(half - currentSettings.edgeForestThickness, half),
                            Random.Range(-half, half),
                            0f
                        )
                    };

                    pos.x += Random.Range(
                        -currentSettings.edgeTreeJitterRange.x,
                        currentSettings.edgeTreeJitterRange.x
                    );
                    pos.y += Random.Range(
                        -currentSettings.edgeTreeJitterRange.y,
                        currentSettings.edgeTreeJitterRange.y
                    );

                    var prefab = prefabs.RandomElement();
                    var tree = Instantiate(prefab, pos, Quaternion.identity, edgeTreesParent);
                    float scale = Random.Range(
                        currentSettings.edgeTreeScaleRange.x,
                        currentSettings.edgeTreeScaleRange.y
                    );
                    tree.transform.localScale = Vector3.one * scale;

                    var trigger = tree.GetComponentsInChildren<CircleCollider2D>()
                                      .FirstOrDefault(c => c.isTrigger);

                    if (trigger == null)
                    {
                        Debug.LogWarning("No CircleCollider2D with IsTrigger found on tree prefab!");
                        FinalizeTree(tree);
                        placed++; totalPlaced++;
                        continue;
                    }

                    float worldRadius = trigger.radius *
                        Mathf.Max(tree.transform.lossyScale.x, tree.transform.lossyScale.y);

                    var hits = Physics2D.OverlapCircleAll(pos, worldRadius);
                    if (hits.Length <= 1)
                    {
                        FinalizeTree(tree);
                        placed++; totalPlaced++;
                    }
                    else
                    {
                        Destroy(tree);
                    }
                }

                Debug.Log($"Side {side}: attempts={attempts}, placed={placed}/{perSide}");
            }

            Debug.Log($"Total edge trees placed: {totalPlaced}");
        }

        private void FinalizeTree(GameObject tree)
        {
            if (currentSettings.disableEdgeTreeColliders)
            {
                foreach (var col in tree.GetComponentsInChildren<Collider>())
                    col.enabled = false;
            }

            var renderers = tree.GetComponentsInChildren<SpriteRenderer>();
            float y = tree.transform.position.y;
            float t = Mathf.InverseLerp(spawnArea, -spawnArea, y);
            int baseOrder = Mathf.RoundToInt(Mathf.Lerp(1, 300, t));

            if (renderers.Length >= 2)
            {
                var bottomSr = renderers[0];
                var topSr    = renderers[1];
                bottomSr.sortingOrder = baseOrder;
                topSr.sortingOrder    = baseOrder + 1000;
            }
            else if (renderers.Length == 1)
            {
                renderers[0].sortingOrder = baseOrder;
            }
        }

        private void Update()
        {
            if (playerSurvived) return;

            timer -= Time.deltaTime;
            UpdateTimerUI();

            if (timer <= 0f)
            {
                playerSurvived = true;
                StageProgressionManager.Instance.OnArenaComplete();
            }
        }

        private void UpdateTimerUI()
        {
            if (timerText != null)
                timerText.text = $"{timer:F1}";

            if (clockHand != null)
            {
                float normalized = Mathf.Clamp01(timer / currentSettings.survivalTime);
                float angle = -90f - (1f - normalized) * 360f;
                clockHand
                    .DOLocalRotate(new Vector3(0f, 0f, angle), 0.1f)
                    .SetEase(Ease.Linear)
                    .SetId(this);
            }
        }

        public void OnPlayerDeath()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public static class Extensions
    {
        public static T RandomElement<T>(this T[] array) =>
            array[Random.Range(0, array.Length)];
        public static Vector3 ToVector3(this Vector2 vector) =>
            new(vector.x, vector.y, 0f);
    }
}
