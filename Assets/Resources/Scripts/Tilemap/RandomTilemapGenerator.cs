using System.Collections.Generic;
using UnityEngine;
using UT = UnityEngine.Tilemaps;

namespace Resources.Scripts.Entity.Tilemap
{
    /// <summary>
    /// Генерирует рандомный Tilemap, выставляя тайл из списка floorTiles,
    /// избегая совпадения соседних тайлов. Позиции тайлов центрированы в (0,0).
    /// При width=20,height=20 тайлы займут диапазон X:[-10…+9], Y:[-10…+9].
    /// </summary>
    public class RandomTilemapGenerator : MonoBehaviour
    {
        [Header("Tilemap Settings")]
        [Tooltip("Ссылка на компонент Tilemap (тот же GameObject или его потомок).")]
        public UT.Tilemap tilemapComponent;

        [Tooltip("Массив вариантов тайлов для данной сцены (floor variants).")]
        public UT.TileBase[] floorTiles;

        [Tooltip("Ширина генерируемой области (в клетках).")]
        [Min(1)]
        public int width = 10;

        [Tooltip("Высота генерируемой области (в клетках).")]
        [Min(1)]
        public int height = 10;

        // Направления для проверки всех 8 соседних клеток.
        private readonly Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int( 1,  0, 0),
            new Vector3Int( 1,  1, 0),
            new Vector3Int( 0,  1, 0),
            new Vector3Int(-1,  1, 0),
            new Vector3Int(-1,  0, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int( 0, -1, 0),
            new Vector3Int( 1, -1, 0)
        };

        /// <summary>
        /// Сбрасываем все смещения до старта сцены
        /// и жёстко фиксируем culling‑границы.
        /// </summary>
        private void Awake()
        {
            if (tilemapComponent == null)
            {
                Debug.LogError("RandomTilemapGenerator: tilemapComponent не назначен!");
                return;
            }

            // Якорь тайлов в левом-нижнем углу клетки
            tilemapComponent.tileAnchor = Vector3.zero;

            // Сбрасываем позицию GameObject'а Tilemap
            tilemapComponent.transform.localPosition = Vector3.zero;

            // Переводим рендерер в Manual‑режим расчёта culling‑границ
            var renderer = tilemapComponent.GetComponent<UT.TilemapRenderer>();
            if (renderer != null)
            {
                renderer.detectChunkCullingBounds = UT.TilemapRenderer.DetectChunkCullingBounds.Manual;
                // Размер области, в которой тайлы будут рендериться (от центра пивота)
                renderer.chunkCullingBounds = new Vector3(
                    width * tilemapComponent.layoutGrid.cellSize.x / 2f,
                    height * tilemapComponent.layoutGrid.cellSize.y / 2f,
                    0f
                );
            }
        }

        /// <summary>
        /// Вызывать после настройки полей tilemapComponent, floorTiles, width и height.
        /// Генерация центрируется в (0,0).
        /// </summary>
        public void GenerateRandomMap()
        {
            if (tilemapComponent == null)
            {
                Debug.LogError("RandomTilemapGenerator: tilemapComponent не назначен!");
                return;
            }

            if (floorTiles == null || floorTiles.Length == 0)
            {
                Debug.LogError("RandomTilemapGenerator: floorTiles пуст или не назначен!");
                return;
            }

            // Очищаем предыдущие тайлы
            tilemapComponent.ClearAllTiles();

            // Смещение начала генерации, чтобы выровнять центр в (0,0)
            int xStart = -width  / 2;
            int yStart = -height / 2;

            for (int ix = 0; ix < width; ix++)
            {
                for (int iy = 0; iy < height; iy++)
                {
                    Vector3Int currentPos = new Vector3Int(
                        xStart + ix,
                        yStart + iy,
                        0
                    );

                    // Собираем доступные варианты тайлов
                    var available = new List<UT.TileBase>(floorTiles);
                    foreach (var dir in directions)
                    {
                        var neigh = tilemapComponent.GetTile(currentPos + dir);
                        if (neigh != null)
                            available.Remove(neigh);
                    }

                    // Выбор случайного тайла
                    UT.TileBase chosen = available.Count > 0
                        ? available[Random.Range(0, available.Count)]
                        : floorTiles[Random.Range(0, floorTiles.Length)];

                    tilemapComponent.SetTile(currentPos, chosen);
                }
            }
        }
    }
}