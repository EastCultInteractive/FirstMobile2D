using System;
using UnityEngine;

namespace Resources.Scripts.Entity.Data
{
    [CreateAssetMenu(
        fileName = "StageData",
        menuName = "GameSettings/Stage Data",
        order = 1
    )]
    [Serializable]
    public class StageData : ScriptableObject
    {
        #region General

        [Header("General")]
        [Tooltip("Name of the stage")]
        public string stageName = string.Empty;

        [Tooltip("Image representing the stage in the menu")]
        public Sprite stageImage = null!;

        #endregion

        #region Scenes

        [Header("Scenes")]
        [Tooltip("Names of the arena scenes for this stage (1–5)")]
        public string[] arenaSceneNames = Array.Empty<string>();

        [Tooltip("Arena settings for each arena scene (one per scene)")]
        public ArenaSettings[] arenaSettingsList = Array.Empty<ArenaSettings>();

        [Tooltip("Name of the labyrinth scene for this stage")]
        public string labyrinthSceneName = string.Empty;

        #endregion

        #region Settings

        [Header("Settings")]
        [Tooltip("Labyrinth settings for this stage")]
        public LabyrinthSettings labyrinthSettings = null!;

        #endregion
    }
}