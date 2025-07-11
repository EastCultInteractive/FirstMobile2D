using System;
using UnityEngine;

namespace Resources.Scripts.Entity.Data
{
    [Serializable]
    public class ObstacleTypeSettings
    {
        [Tooltip("Unique obstacle identifier")]
        public string obstacleName = string.Empty;

        [Tooltip("Obstacle prefab (must include SpriteRenderer and Collider)")]
        public GameObject obstaclePrefab = null!;

        [Tooltip("Spawn chance (0 to 1) for this obstacle type")]
        [Range(0f, 1f)]
        public float spawnProbability = 1f;

        [Tooltip("Minimum number of instances to spawn")]
        public int minCount = 1;

        [Tooltip("Maximum number of instances to spawn")]
        public int maxCount = 3;

        [Tooltip("Minimum scale applied on spawn")]
        public float minScale = 1f;

        [Tooltip("Maximum scale applied on spawn")]
        public float maxScale = 1f;
    }

    [CreateAssetMenu(fileName = "ArenaSettings", menuName = "GameSettings/Arena Settings")]
    public sealed class ArenaSettings : ScriptableObject
    {
        #region Arena Parameters

        [Header("Arena Parameters")]
        [Tooltip("Time to survive on the arena, in seconds")]
        public float survivalTime = 10f;

        [Tooltip("Number of enemies to spawn")]
        public int enemyCount = 5;

        [Tooltip("Enemy prefab list")]
        public GameObject[] enemyPrefabs = Array.Empty<GameObject>();

        #endregion

        #region Fairy Parameters

        [Header("Fairy Parameters")]
        [Tooltip("Number of fairies to spawn")]
        public int fairyCount = 3;

        [Tooltip("Fairy prefab list")]
        public GameObject[] fairyPrefabs = Array.Empty<GameObject>();

        #endregion

        #region Obstacle Parameters

        [Header("Obstacle Parameters")]
        [Tooltip("Settings for each obstacle type to spawn")]
        public ObstacleTypeSettings[] obstacleTypes = Array.Empty<ObstacleTypeSettings>();

        [Header("Additional Obstacle Settings")]
        [Tooltip("Minimum distance required between spawned obstacles")]
        public float obstacleMinDistance = 1f;

        #endregion

        #region Edge Forest (Optional)

        [Header("Edge Forest (Optional)")]
        [Tooltip("Enable planting trees along arena edges")]
        public bool plantTreesAtEdges = true;

        [Tooltip("Thickness of forest border inside the arena")]
        public float edgeForestThickness = 5f;

        [Tooltip("Number of trees per side")]
        public int edgeTreesPerSide = 50;

        [Tooltip("Random scale range for edge trees")]
        public Vector2 edgeTreeScaleRange = new Vector2(0.8f, 1.5f);

        [Tooltip("Position jitter range (X and Y axes)")]
        public Vector2 edgeTreeJitterRange = new Vector2(1f, 1f);

        [Tooltip("Disable colliders on edge trees")]
        public bool disableEdgeTreeColliders = true;

        #endregion

        #region Tilemap Settings

        [Header("Tilemap Settings")]
        [Tooltip("Array of TileBase variants to use for this arena (e.g., grass tiles, sand tiles, etc.).")]
        public UnityEngine.Tilemaps.TileBase[] tilesForThisArena = Array.Empty<UnityEngine.Tilemaps.TileBase>();

        [Tooltip("Width of the generated map")]
        [Min(1)]
        public int tilemapWidth = 10;

        [Tooltip("Height of the generated map")]
        [Min(1)]
        public int tilemapHeight = 10;

        #endregion
    }
}
