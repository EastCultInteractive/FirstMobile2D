using System.Collections.Generic;
using System.Reflection;
using Resources.Scripts.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.SceneManagement;
using Resources.Scripts.GameManagers;
using Resources.Scripts.Tilemap;
using Object = UnityEngine.Object;

namespace Resources.Scripts.Labyrinth
{
    public class LabyrinthGeneratorWithWalls : MonoBehaviour
    {
        [Header("Labyrinth Settings (ScriptableObject).\nIf empty, StageData will be used.")]
        [SerializeField] private LabyrinthSettings labyrinthSettings;

        [Header("Fallback Parameters")]
        [SerializeField, Range(4, 20)] private int defaultRows = 5;
        [SerializeField, Range(4, 20)] private int defaultCols = 5;
        [SerializeField] private float defaultCellSizeX = 1f;
        [SerializeField] private float defaultCellSizeY = 1f;
        [SerializeField] private float defaultTimeLimit = 30f;

        [Header("Wall Prefabs (only three)")]
        [Tooltip("Top/Bottom walls prefab")]
        [SerializeField] private GameObject wallPrefabTopBottom;
        [Tooltip("Left/Right isometric walls prefab")]
        [SerializeField] private GameObject wallPrefabIso;
        [Tooltip("Left/Right non-iso walls prefab")]
        [SerializeField] private GameObject wallPrefabNoIso;

        [Header("Offsets")]
        [SerializeField] private float topWallSpacingY;
        [SerializeField] private float bottomWallSpacingY;
        [SerializeField] private float leftWallSpacingX;
        [SerializeField] private float rightWallSpacingX;

        [Header("Bonuses & Traps")]
        [SerializeField] private GameObject bonusPrefab;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField, Range(1, 10)] private int minPlacementDistance = 5;
        [SerializeField] private int bonusCount = 1;
        [SerializeField] private int trapCount = 3;

        [Header("Enemy Spawn Settings")]
        [SerializeField, Tooltip("List of enemy prefabs")]
        private List<GameObject> enemyPrefabs = new List<GameObject>();
        [SerializeField, Range(0, 50), Tooltip("Number of enemies to spawn")]
        private int enemyCount = 5;
        [SerializeField, Tooltip("Min distance between enemy spawns (in cells)")]
        private float minEnemySpawnDistance = 3f;

        [Header("Start/Finish Markers")]
        [SerializeField, Tooltip("Start marker prefab")] private GameObject startMarkerPrefab;
        [SerializeField, Tooltip("Finish marker prefab")] private GameObject finishMarkerPrefab;

        [Header("UI Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private RectTransform clockHand;

        [Header("Shadows")]
        [SerializeField] private float shadowWidthOffset = 0.1f;
        [SerializeField] private float shadowHeightOffset = 0.1f;

        [Header("Sorting Orders")]
        [SerializeField] private int sortingOrderTop = 4;
        [SerializeField] private int sortingOrderBottom = 2;
        [SerializeField] private int sortingOrderLeftIso = 3;
        [SerializeField] private int sortingOrderLeftNoIso = 3;
        [SerializeField] private int sortingOrderRightIso = 3;
        [SerializeField] private int sortingOrderRightNoIso = 3;

        [Header("Unity Layer ID (for shadows)")]
        [SerializeField] private int labyrinthUnityLayer = 3;

        private int rows, cols;
        private float cellSizeX, cellSizeY;
        private float labyrinthTimer, totalLabyrinthTime;
        private LabyrinthField labyrinth;
        public static LabyrinthField CurrentField { get; private set; }

        private enum WallOrientation { Top, Bottom, Left, Right }
        private enum WallCategory     { TopBottom, NoIso, Iso }

        private static readonly FieldInfo FiShapePath;
        private static readonly FieldInfo FiShapePathHash;
        private static readonly FieldInfo FiMesh;
        private static readonly MethodInfo MiGenerateShadowMesh;

        private RandomTilemapGenerator floorGenerator;

        private Sprite _lastTopBottom;
        private Sprite _lastNoIso;
        private Sprite _lastIso;

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
            var stageData = GameStageManager.currentStageData;
            if (stageData != null && stageData.labyrinthSettings != null)
                ApplySettings(stageData.labyrinthSettings);
            else if (labyrinthSettings != null)
                ApplySettings(labyrinthSettings);
            else
                ApplyDefaults();

            totalLabyrinthTime = labyrinthTimer;
            if (clockHand != null)
                clockHand.localRotation = Quaternion.Euler(0f, 0f, -90f);

            labyrinth = new LabyrinthField(rows, cols, cellSizeX, cellSizeY);
            CurrentField = labyrinth;

            GenerateWalls();
            PlaceGameplayElements();
        }

        private void ApplySettings(LabyrinthSettings s)
        {
            rows           = s.rows;
            cols           = s.cols;
            cellSizeX      = s.cellSizeX;
            cellSizeY      = s.cellSizeY;
            labyrinthTimer = s.labyrinthTimeLimit;
            InitializeFloorTilemap(s);
        }

        private void ApplyDefaults()
        {
            rows           = defaultRows;
            cols           = defaultCols;
            cellSizeX      = defaultCellSizeX;
            cellSizeY      = defaultCellSizeY;
            labyrinthTimer = defaultTimeLimit;
        }

        private void InitializeFloorTilemap(LabyrinthSettings s)
        {
            if (s.tilesForThisLabyrinth == null || s.tilesForThisLabyrinth.Length == 0)
            {
                Debug.LogWarning("LabyrinthGenerator: no floor tiles assigned!");
                return;
            }

            floorGenerator = Object.FindFirstObjectByType<RandomTilemapGenerator>();
            if (floorGenerator == null)
            {
                Debug.LogError("LabyrinthGenerator: RandomTilemapGenerator not found!");
                return;
            }

            floorGenerator.floorTiles = s.tilesForThisLabyrinth;
            floorGenerator.width      = Mathf.Max(1, s.floorWidth);
            floorGenerator.height     = Mathf.Max(1, s.floorHeight);

            var tilemapComp = floorGenerator.tilemapComponent
                              ?? floorGenerator.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
            if (tilemapComp == null)
            {
                Debug.LogError("LabyrinthGenerator: tilemapComponent missing!");
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

                // Top/Bottom walls
                if ((cell.TopBorder || cell.BottomBorder) && wallPrefabTopBottom != null)
                {
                    var ori    = cell.TopBorder ? WallOrientation.Top : WallOrientation.Bottom;
                    var offset = ori == WallOrientation.Top
                        ? Vector3.up   * (cellSizeY/2  + topWallSpacingY)
                        : Vector3.down * (cellSizeY/2  + bottomWallSpacingY);
                    CreateWall(wallPrefabTopBottom, ori, r, c, center, offset);
                }

                // Left/Right walls
                if (cell.LeftBorder || cell.RightBorder)
                {
                    bool isLeft = cell.LeftBorder;
                    var ori = isLeft ? WallOrientation.Left : WallOrientation.Right;

                    bool hasBorderBelow = false;
                    if (r + 1 < rows)
                    {
                        var below = labyrinth.Field[r + 1, c];
                        hasBorderBelow = isLeft ? below.LeftBorder : below.RightBorder;
                    }
                    bool isClosing = (r == rows - 1) || !hasBorderBelow;

                    // отменяем изометрию, если справа есть BottomBorder
                    if (!isLeft && c + 1 < cols && labyrinth.Field[r, c + 1].BottomBorder)
                        isClosing = false;

                    var prefab = isClosing ? wallPrefabIso : wallPrefabNoIso;
                    var offset = (ori == WallOrientation.Left
                        ? Vector3.left  * (cellSizeX/2 + leftWallSpacingX)
                        : Vector3.right * (cellSizeX/2 + rightWallSpacingX));
                    CreateWall(prefab, ori, r, c, center, offset);
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

        private GameObject CreateWall(
            GameObject prefab,
            WallOrientation ori,
            int r, int c,
            Vector3 center,
            Vector3 offset
        )
        {
            var go = Instantiate(prefab, center + offset, Quaternion.identity, transform);
            go.name = $"{ori}Wall_R{r}_C{c}";

            if (labyrinthSettings != null)
            {
                WallCategory cat = (ori == WallOrientation.Top || ori == WallOrientation.Bottom)
                    ? WallCategory.TopBottom
                    : prefab == wallPrefabNoIso
                        ? WallCategory.NoIso
                        : WallCategory.Iso;

                Sprite[] variants = cat == WallCategory.TopBottom
                    ? labyrinthSettings.topBottomVariants
                    : cat == WallCategory.NoIso
                        ? labyrinthSettings.noIsoVariants
                        : labyrinthSettings.isoVariants;

                Sprite last = cat == WallCategory.TopBottom
                    ? _lastTopBottom
                    : cat == WallCategory.NoIso
                        ? _lastNoIso
                        : _lastIso;

                Sprite chosen = null;
                if (variants != null && variants.Length > 0)
                {
                    int tries = 0;
                    do
                    {
                        chosen = variants[UnityEngine.Random.Range(0, variants.Length)];
                        tries++;
                    } while (chosen == last && tries < 10);

                    if (cat == WallCategory.TopBottom) _lastTopBottom = chosen;
                    else if (cat == WallCategory.NoIso) _lastNoIso = chosen;
                    else _lastIso = chosen;
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = chosen;
            }

            // Устанавливаем слой и сортировку
            SetLayerRecursively(go, labyrinthUnityLayer);
            var rend = go.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                int baseOrder;
                switch (ori)
                {
                    case WallOrientation.Top:
                        baseOrder = sortingOrderTop;
                        break;
                    case WallOrientation.Bottom:
                        baseOrder = sortingOrderBottom + 1;
                        break;
                    case WallOrientation.Left:
                        baseOrder = prefab == wallPrefabIso
                            ? sortingOrderLeftIso
                            : sortingOrderLeftNoIso;
                        break;
                    case WallOrientation.Right:
                        baseOrder = prefab == wallPrefabIso
                            ? sortingOrderRightIso
                            : sortingOrderRightNoIso;
                        break;
                    default:
                        baseOrder = 0;
                        break;
                }

                int order = baseOrder + r;

                // Особые приоритеты для RightWall
                if (ori == WallOrientation.Right && c + 1 < cols)
                {
                    var neigh = labyrinth.Field[r, c + 1];
                    // верхняя соседняя граница
                    if (neigh.TopBorder)
                        order += 2;
                    // нижняя граница справа в ряду 1
                    if (neigh.BottomBorder && r == 1)
                        order += 2;
                    // диагональная нижняя граница справа-вверх
                    if (r - 1 >= 0 && labyrinth.Field[r - 1, c + 1].BottomBorder)
                        order += 2;
                }

                rend.sortingOrder = order;
            }

            var sc = go.GetComponent<ShadowCaster2D>() ?? go.AddComponent<ShadowCaster2D>();
            sc.castsShadows = true;
            sc.selfShadows  = false;
            sc.alphaCutoff  = 1f;
            ApplyCustomShadowPath(sc, cellSizeX + shadowWidthOffset, cellSizeY + shadowHeightOffset);

            return go;
        }

        private static void ApplyCustomShadowPath(ShadowCaster2D sc, float width, float height)
        {
            if (sc == null || FiShapePath == null || FiShapePathHash == null ||
                FiMesh == null || MiGenerateShadowMesh == null) return;

            float hw = width/2f, hh = height/2f;
            var pts2D = new[] {
                new Vector2(-hw, -hh),
                new Vector2(-hw,  hh),
                new Vector2( hw,  hh),
                new Vector2( hw, -hh)
            };
            var pts3D = new Vector3[pts2D.Length];
            for (int i = 0; i < pts2D.Length; i++) pts3D[i] = pts2D[i];

            FiShapePath.SetValue(sc, pts3D);
            FiShapePathHash.SetValue(sc, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var mesh = (Mesh)FiMesh.GetValue(sc);
            MiGenerateShadowMesh.Invoke(null, new object[]{ mesh, pts3D });
            FiMesh.SetValue(sc, mesh);
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform t in obj.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

        private void PlaceItems(GameObject prefab, int count, List<Vector3> positions)
        {
            if (prefab == null || positions.Count == 0 || count <= 0) return;
            int placed = 0;
            var avail = new List<Vector3>(positions);
            while (placed < count && avail.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, avail.Count);
                Instantiate(prefab, avail[idx], Quaternion.identity, transform);
                placed++;
                var chosen = avail[idx];
                avail.RemoveAll(p =>
                    Mathf.Abs(p.x - chosen.x)/cellSizeX +
                    Mathf.Abs(p.y - chosen.y)/cellSizeY < minPlacementDistance);
            }
        }

        private void PlaceEnemies(List<GameObject> prefabs, int count, List<Vector3> positions, float minDist)
        {
            if (prefabs == null || prefabs.Count == 0 || positions.Count == 0 || count <= 0) return;
            int placed = 0;
            var avail = new List<Vector3>(positions);
            while (placed < count && avail.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, avail.Count);
                var pos = avail[idx];
                Instantiate(prefabs[UnityEngine.Random.Range(0, prefabs.Count)], pos, Quaternion.identity);
                placed++;
                avail.RemoveAll(p => Vector3.Distance(p, pos) < minDist * Mathf.Max(cellSizeX, cellSizeY));
            }
        }

        private void PlaceGameplayElements()
        {
            var startPos  = labyrinth.GetStartWorldPosition();
            var finishPos = labyrinth.GetFinishWorldPosition();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) player.transform.position = startPos;

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

            if (LabyrinthMapController.Instance != null)
                LabyrinthMapController.Instance.SetSolutionPath(
                    labyrinth.GetSolutionPathWorldPositions()
                );
        }

        private void Update()
        {
            labyrinthTimer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = $"{labyrinthTimer:F1}";
            if (clockHand != null && totalLabyrinthTime > 0f)
            {
                float norm = Mathf.Clamp01(labyrinthTimer / totalLabyrinthTime);
                clockHand.localRotation = Quaternion.Euler(0f, 0f, -90f - (1f - norm) * 360f);
            }
            if (labyrinthTimer <= 0f)
                SceneManager.LoadScene("Menu");
        }
    }
}
