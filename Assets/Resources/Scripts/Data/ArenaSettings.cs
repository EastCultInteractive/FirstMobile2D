using System;
using UnityEngine;

namespace Resources.Scripts.Data
{
    [CreateAssetMenu(fileName = "ArenaSettings", menuName = "GameSettings/Arena Settings")]
    public sealed class ArenaSettings : ScriptableObject
    {
        [Header("Arena Stats")] public float survivalTime = 30f;
        
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
