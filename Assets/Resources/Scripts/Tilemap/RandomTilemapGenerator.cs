using System.Collections.Generic;
using UnityEngine;
using UT = UnityEngine.Tilemaps;

namespace Resources.Scripts.Tilemap
{
    /// <summary>
    /// Генерирует рандомный Tilemap, выставляя тайл из списка floorTiles, избегая совпадения соседних тайлов.
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
        /// Вызывать после того, как поля tilemapComponent, floorTiles, width и height настроены.
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

            // Очищаем предыдущие тайлы (если были).
            tilemapComponent.ClearAllTiles();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Создаем список доступных вариантов из всех floorTiles.
                    List<UT.TileBase> availableTiles = new List<UT.TileBase>(floorTiles);
                    // Обратите внимание: y инвертируется, чтобы верхняя строка на холсте соответствовала y=0 в Tilemap.
                    Vector3Int currentPos = new Vector3Int(x, -y, 0);

                    // Проверяем всех 8 соседей.
                    foreach (Vector3Int direction in directions)
                    {
                        Vector3Int neighborPos = currentPos + direction;
                        UT.TileBase neighborTile = tilemapComponent.GetTile(neighborPos);

                        // Если соседний тайл существует, удаляем его тип из доступных вариантов.
                        if (neighborTile != null)
                        {
                            availableTiles.Remove(neighborTile);
                        }
                    }

                    // Выбираем случайный тайл из оставшихся. Если ничего не осталось — случайный из исходного массива.
                    UT.TileBase chosenTile = availableTiles.Count > 0
                        ? availableTiles[Random.Range(0, availableTiles.Count)]
                        : floorTiles[Random.Range(0, floorTiles.Length)];

                    tilemapComponent.SetTile(currentPos, chosenTile);
                }
            }
        }
    }
}
