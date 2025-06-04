using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using Resources.Scripts.Data;
using Resources.Scripts.GameManagers;
using Resources.Scripts.Tilemap;

namespace Resources.Scripts.Labyrinth
{
    public class LabyrinthGeneratorWithWalls : MonoBehaviour
    {
        [Header("Настройки лабиринта (ScriptableObject).\nЕсли оставить поле пустым — будут браться настройки из StageData.")]
        [SerializeField] private LabyrinthSettings labyrinthSettings;

        [Header("Резервные параметры")]
        [SerializeField, Range(4, 20)] private int defaultRows = 5;
        [SerializeField, Range(4, 20)] private int defaultCols = 5;
        [SerializeField] private float defaultCellSizeX = 1f;
        [SerializeField] private float defaultCellSizeY = 1f;
        [SerializeField] private float defaultTimeLimit = 30f;

        [Header("Префабы стен")]
        [SerializeField] private GameObject topWallPrefab;
        [SerializeField] private GameObject bottomWallPrefab;
        [SerializeField] private GameObject leftWallPrefab;
        [SerializeField] private GameObject leftNoIsoWallPrefab;
        [SerializeField] private GameObject rightWallPrefab;
        [SerializeField] private GameObject rightNoIsoWallPrefab;

        [Header("Смещения")]
        [SerializeField] private float topWallSpacingY;
        [SerializeField] private float bottomWallSpacingY;
        [SerializeField] private float leftWallSpacingX;
        [SerializeField] private float rightWallSpacingX;

        [Header("Коллайдеры")]
        [SerializeField] private bool enableWallColliders = true;
        [SerializeField] private Vector2 horizontalColliderSize = new Vector2(1f, 0.2f);
        [SerializeField] private Vector2 topColliderOffset = Vector2.zero;
        [SerializeField] private Vector2 bottomColliderOffset = Vector2.zero;
        [SerializeField] private Vector2 verticalColliderSize = new Vector2(0.2f, 1f);
        [SerializeField] private Vector2 leftColliderOffset = Vector2.zero;
        [SerializeField] private Vector2 rightColliderOffset = Vector2.zero;

        [Header("Бонусы и ловушки")]
        [SerializeField] private GameObject bonusPrefab;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField, Range(1, 10)] private int minPlacementDistance = 5;
        [SerializeField] private int bonusCount = 1;
        [SerializeField] private int trapCount = 3;

        [Header("Настройки спавна врагов")]
        [SerializeField, Tooltip("Список префабов врагов")]
        private List<GameObject> enemyPrefabs = new List<GameObject>();
        [SerializeField, Range(0, 50), Tooltip("Количество врагов для спавна")]
        private int enemyCount = 5;
        [SerializeField, Tooltip("Минимальное расстояние между спавном врагов (в клетках)")]
        private float minEnemySpawnDistance = 3f;

        [Header("Маркеры старта/финиша")]
        [SerializeField, Tooltip("Префаб маркера старта")]
        private GameObject startMarkerPrefab;
        [SerializeField, Tooltip("Префаб маркера финиша")]
        private GameObject finishMarkerPrefab;

        [Header("UI Таймер")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private RectTransform clockHand;

        [Header("Тени")]
        [SerializeField] private float shadowWidthOffset = 0.1f;
        [SerializeField] private float shadowHeightOffset = 0.1f;

        [Header("Sorting Order Настройки")]
        [SerializeField] private int sortingOrderTopUp = 4;
        [SerializeField] private int sortingOrderTopDown = 4;
        [SerializeField] private int sortingOrderBottomUp = 2;
        [SerializeField] private int sortingOrderBottomDown = 2;
        [SerializeField] private int sortingOrderLeftIso = 3;
        [SerializeField] private int sortingOrderLeftNoIso = 3;
        [SerializeField] private int sortingOrderRightIso = 3;
        [SerializeField] private int sortingOrderRightNoIsoUp = 3;
        [SerializeField] private int sortingOrderRightNoIsoDown = 3;

        [Header("Unity Layer ID для Labyrinth")]
        [SerializeField] private int labyrinthUnityLayer = 3;

        private int rows, cols;
        private float cellSizeX, cellSizeY;
        private float labyrinthTimer, totalLabyrinthTime;
        private LabyrinthField labyrinth;

        // Для доступа извне
        public static LabyrinthField CurrentField { get; private set; }

        private enum WallOrientation { Top, Bottom, Left, Right }

        // Reflection-кэш приватных полей ShadowCaster2D
        private static readonly FieldInfo FiShapePath;
        private static readonly FieldInfo FiShapePathHash;
        private static readonly FieldInfo FiMesh;
        private static readonly MethodInfo MiGenerateShadowMesh;

        // Ссылка на RandomTilemapGenerator в сцене
        private RandomTilemapGenerator floorGenerator;

        static LabyrinthGeneratorWithWalls()
        {
            var scType = typeof(ShadowCaster2D);
            FiShapePath         = scType.GetField("m_ShapePath",     BindingFlags.NonPublic | BindingFlags.Instance);
            FiShapePathHash     = scType.GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
            FiMesh              = scType.GetField("m_Mesh",          BindingFlags.NonPublic | BindingFlags.Instance);
            var utilType        = scType.Assembly.GetType("UnityEngine.Rendering.Universal.ShadowUtility");
            MiGenerateShadowMesh = utilType?
                .GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);
        }

        private void Start()
        {
            // 1) Получаем настройки лабиринта преимущественно из StageData
            var stageData = GameStageManager.currentStageData;
            if (stageData != null && stageData.labyrinthSettings != null)
            {
                var s = stageData.labyrinthSettings;
                rows           = s.rows;
                cols           = s.cols;
                cellSizeX      = s.cellSizeX;
                cellSizeY      = s.cellSizeY;
                labyrinthTimer = s.labyrinthTimeLimit;
                // Инициализируем tilemap согласно настройкам из SO
                InitializeFloorTilemap(s);
            }
            else if (labyrinthSettings != null)
            {
                // Запуск напрямую (без менеджера этапов)
                rows           = labyrinthSettings.rows;
                cols           = labyrinthSettings.cols;
                cellSizeX      = labyrinthSettings.cellSizeX;
                cellSizeY      = labyrinthSettings.cellSizeY;
                labyrinthTimer = labyrinthSettings.labyrinthTimeLimit;
                InitializeFloorTilemap(labyrinthSettings);
            }
            else
            {
                // Резервный вариант
                rows           = defaultRows;
                cols           = defaultCols;
                cellSizeX      = defaultCellSizeX;
                cellSizeY      = defaultCellSizeY;
                labyrinthTimer = defaultTimeLimit;
                // Резервная генерация пола не выполняется
            }

            totalLabyrinthTime = labyrinthTimer;

            if (clockHand != null)
                clockHand.localRotation = Quaternion.Euler(0f, 0f, -90f);

            labyrinth = new LabyrinthField(rows, cols, cellSizeX, cellSizeY);
            CurrentField = labyrinth;

            GenerateWalls();
            PlaceGameplayElements();
        }

        /// <summary>
        /// Находит RandomTilemapGenerator в сцене и генерирует пол по настройкам из SO.
        /// </summary>
        private void InitializeFloorTilemap(LabyrinthSettings s)
        {
            if (s.tilesForThisLabyrinth == null || s.tilesForThisLabyrinth.Length == 0)
            {
                Debug.LogWarning("LabyrinthGenerator: tilesForThisLabyrinth не назначены в LabyrinthSettings!");
                return;
            }

            // Найдём в сцене компонент RandomTilemapGenerator
            floorGenerator = Object.FindFirstObjectByType<RandomTilemapGenerator>();
            if (floorGenerator == null)
            {
                Debug.LogError("LabyrinthGenerator: на сцене не найден RandomTilemapGenerator!");
                return;
            }

            floorGenerator.floorTiles = s.tilesForThisLabyrinth;
            floorGenerator.width      = Mathf.Max(1, s.floorWidth);
            floorGenerator.height     = Mathf.Max(1, s.floorHeight);

            var tilemapComp = floorGenerator.tilemapComponent
                              ?? floorGenerator.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();

            if (tilemapComp == null)
            {
                Debug.LogError("LabyrinthGenerator: RandomTilemapGenerator.tilemapComponent не задан!");
                return;
            }

            floorGenerator.tilemapComponent = tilemapComp;
            floorGenerator.GenerateRandomMap();
        }

        private void GenerateWalls()
        {
            var bonusPositions = new List<Vector3>();
            var trapPositions  = new List<Vector3>();
            var enemyPositions = new List<Vector3>();

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Vector3 center = new Vector3(c * cellSizeX, -r * cellSizeY, 0f);
                var cell = labyrinth.Field[r, c];

                if (!cell.IsStart && !cell.IsFinish)
                    enemyPositions.Add(center);

                if (cell.TopBorder && topWallPrefab != null)
                    CreateWall(topWallPrefab, WallOrientation.Top, r, c,
                               center, Vector3.up * (cellSizeY / 2 + topWallSpacingY));
                if (cell.BottomBorder && bottomWallPrefab != null)
                    CreateWall(bottomWallPrefab, WallOrientation.Bottom, r, c,
                               center, Vector3.down * (cellSizeY / 2 + bottomWallSpacingY));

                if (cell.LeftBorder)
                {
                    bool noIso = cell.BottomBorder
                               || (c > 0 && labyrinth.Field[r, c - 1].BottomBorder)
                               || (r < rows - 1 && labyrinth.Field[r + 1, c].LeftBorder);
                    CreateWall(noIso ? leftNoIsoWallPrefab : leftWallPrefab,
                               WallOrientation.Left, r, c,
                               center, Vector3.left * (cellSizeX / 2 + leftWallSpacingX));
                }

                if (cell.RightBorder)
                {
                    bool noIso = cell.BottomBorder
                               || (c < cols - 1 && labyrinth.Field[r, c + 1].BottomBorder)
                               || (r < rows - 1 && labyrinth.Field[r + 1, c].RightBorder);
                    CreateWall(noIso ? rightNoIsoWallPrefab : rightWallPrefab,
                               WallOrientation.Right, r, c,
                               center, Vector3.right * (cellSizeX / 2 + rightWallSpacingX));
                }

                if (!cell.IsStart && !cell.IsFinish)
                {
                    if (cell.IsSolutionPath) bonusPositions.Add(center);
                    else                     trapPositions.Add(center);
                }
            }

            PlaceItems(bonusPrefab, bonusCount, bonusPositions);
            PlaceItems(trapPrefab,  trapCount,  trapPositions);
            PlaceEnemies(enemyPrefabs, enemyCount, enemyPositions, minEnemySpawnDistance);
        }

        private void PlaceGameplayElements()
        {
            // 1. Вычисляем старт/финиш
            Vector3 startPos  = labyrinth.GetStartWorldPosition();
            Vector3 finishPos = labyrinth.GetFinishWorldPosition();

            // 2. Ставим игрока в старт
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.transform.position = startPos;

            // 3. Маркеры
            if (startMarkerPrefab != null)
            {
                var go = Instantiate(startMarkerPrefab, startPos, Quaternion.identity, transform);
                go.tag = "Start";
            }

            if (finishMarkerPrefab != null)
            {
                var go = Instantiate(finishMarkerPrefab, finishPos, Quaternion.identity, transform);
                go.tag = "Finish";
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(cellSizeX, cellSizeY);
                go.AddComponent<LabyrinthFinishTrigger>();
            }

            // 4. Мини-карта
            if (LabyrinthMapController.Instance != null)
                LabyrinthMapController.Instance.SetSolutionPath(
                    labyrinth.GetSolutionPathWorldPositions()
                );
        }

        private GameObject CreateWall(
            GameObject prefab,
            WallOrientation ori,
            int r,
            int c,
            Vector3 center,
            Vector3 offset
        )
        {
            var go = Instantiate(prefab, center + offset, Quaternion.identity, transform);
            go.name = $"{ori}Wall_R{r}_C{c}";
            SetLayerRecursively(go, labyrinthUnityLayer);

            switch (ori)
            {
                case WallOrientation.Top:
                    {
                        var up   = go.transform.Find("Up");
                        var down = go.transform.Find("Down");
                        if (up   != null) up.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderTopUp;
                        if (down != null) down.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderTopDown;
                    }
                    break;
                case WallOrientation.Bottom:
                    {
                        var up   = go.transform.Find("Up");
                        var down = go.transform.Find("Down");
                        if (up   != null) up.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderBottomUp;
                        if (down != null) down.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderBottomDown;
                    }
                    break;
                case WallOrientation.Left:
                    {
                        var sr = go.GetComponent<SpriteRenderer>();
                        bool noIso = prefab == leftNoIsoWallPrefab;
                        sr.sortingOrder = noIso ? sortingOrderLeftNoIso : sortingOrderLeftIso;
                    }
                    break;
                case WallOrientation.Right:
                    {
                        bool noIso = prefab == rightNoIsoWallPrefab;
                        if (noIso)
                        {
                            var up   = go.transform.Find("Up");
                            var down = go.transform.Find("Down");
                            if (up   != null) up.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderRightNoIsoUp;
                            if (down != null) down.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderRightNoIsoDown;
                        }
                        else
                        {
                            go.GetComponent<SpriteRenderer>().sortingOrder = sortingOrderRightIso;
                        }
                    }
                    break;
            }

            AddCollidersAndShadows(go, ori);
            return go;
        }

        private void AddCollidersAndShadows(GameObject go, WallOrientation ori)
        {
            if (ori == WallOrientation.Top || ori == WallOrientation.Bottom)
            {
                foreach (Transform child in go.transform)
                {
                    var part = child.gameObject;
                    if (enableWallColliders) AddCollider(part, ori);

                    var sc = part.GetComponent<ShadowCaster2D>() ?? part.AddComponent<ShadowCaster2D>();
                    sc.castsShadows = true;
                    sc.selfShadows  = false;
                    sc.alphaCutoff  = 1f;
                    ApplyCustomShadowPath(sc, cellSizeX + shadowWidthOffset, cellSizeY + shadowHeightOffset);
                }
            }
            else
            {
                if (enableWallColliders) AddCollider(go, ori);

                var sc = go.GetComponent<ShadowCaster2D>() ?? go.AddComponent<ShadowCaster2D>();
                sc.castsShadows = true;
                sc.selfShadows  = false;
                sc.alphaCutoff  = 1f;
                ApplyCustomShadowPath(sc, cellSizeX + shadowWidthOffset, cellSizeY + shadowHeightOffset);
            }
        }

        private void AddCollider(GameObject go, WallOrientation ori)
        {
            BoxCollider2D phys = null;
            foreach (var c in go.GetComponents<BoxCollider2D>())
                if (!c.isTrigger) { phys = c; break; }
            if (phys == null) phys = go.AddComponent<BoxCollider2D>();
            phys.isTrigger = false;

            switch (ori)
            {
                case WallOrientation.Top:
                    phys.size   = horizontalColliderSize;
                    phys.offset = topColliderOffset;
                    break;
                case WallOrientation.Bottom:
                    phys.size   = horizontalColliderSize;
                    phys.offset = bottomColliderOffset;
                    break;
                case WallOrientation.Left:
                    phys.size   = verticalColliderSize;
                    phys.offset = leftColliderOffset;
                    break;
                case WallOrientation.Right:
                    phys.size   = verticalColliderSize;
                    phys.offset = rightColliderOffset;
                    break;
            }
        }

        private static void ApplyCustomShadowPath(ShadowCaster2D sc, float width, float height)
        {
            if (sc == null || FiShapePath == null || FiShapePathHash == null || FiMesh == null || MiGenerateShadowMesh == null)
                return;

            float hw = width / 2f;
            float hh = height / 2f;
            var pts2D = new[]
            {
                new Vector2(-hw, -hh),
                new Vector2(-hw,  hh),
                new Vector2( hw,  hh),
                new Vector2( hw, -hh)
            };
            var pts3D = new Vector3[pts2D.Length];
            for (int i = 0; i < pts2D.Length; i++)
                pts3D[i] = pts2D[i];

            FiShapePath   .SetValue(sc, pts3D);
            FiShapePathHash.SetValue(sc, Random.Range(int.MinValue, int.MaxValue));

            var mesh = (Mesh)FiMesh.GetValue(sc);
            MiGenerateShadowMesh.Invoke(null, new object[]{ mesh, pts3D });
            FiMesh.SetValue(sc, mesh);
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private void PlaceItems(GameObject prefab, int count, List<Vector3> positions)
        {
            if (prefab == null || positions.Count == 0 || count <= 0)
                return;

            int placed = 0;
            var available = new List<Vector3>(positions);
            while (placed < count && available.Count > 0)
            {
                int idx = Random.Range(0, available.Count);
                Instantiate(prefab, available[idx], Quaternion.identity, transform);
                placed++;

                var chosen = available[idx];
                available.RemoveAll(p =>
                    Mathf.Abs(p.x - chosen.x) / cellSizeX +
                    Mathf.Abs(p.y - chosen.y) / cellSizeY < minPlacementDistance
                );
            }
        }

        private void PlaceEnemies(List<GameObject> prefabs, int count, List<Vector3> positions, float minDistance)
        {
            if (prefabs == null || prefabs.Count == 0 || positions.Count == 0 || count <= 0)
                return;

            int placed = 0;
            var available = new List<Vector3>(positions);
            while (placed < count && available.Count > 0)
            {
                int idx = Random.Range(0, available.Count);
                Vector3 pos = available[idx];
                var prefab = prefabs[Random.Range(0, prefabs.Count)];
                Instantiate(prefab, pos, Quaternion.identity);
                placed++;
                available.RemoveAll(p =>
                    Vector3.Distance(p, pos) < minDistance * Mathf.Max(cellSizeX, cellSizeY)
                );
            }
        }

        private void Update()
        {
            labyrinthTimer -= Time.deltaTime;

            if (timerText != null)
                timerText.text = $"{labyrinthTimer:F1}";

            if (clockHand != null && totalLabyrinthTime > 0f)
            {
                float norm = Mathf.Clamp01(labyrinthTimer / totalLabyrinthTime);
                float angle = -90f - (1f - norm) * 360f;
                clockHand.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (labyrinthTimer <= 0f)
            {
                if (StageProgressionManager.Instance != null)
                    // Когда время вышло — считаем, что лабиринт пройден и переходим к выбору перков/следующему этапу
                    StageProgressionManager.Instance.OnPerkChosen(null);
                else
                    Debug.LogWarning("StageProgressionManager отсутствует!");
            }
        }
    }
}
