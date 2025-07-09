using UnityEngine;

namespace Resources.Scripts.CameraTools
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;

        [Header("Movement Settings")]
        [SerializeField, Range(1, 20)] private int followSpeed = 5;
        [SerializeField] private float zOffset = -10f;

        [Header("Camera Projection Settings")]
        [SerializeField, Range(1, 20)] private float projMin = 2f;
        [SerializeField, Range(1, 20)] private float projMax = 10f;
        [SerializeField] private KeyCode zoomKey = KeyCode.Space;
        [SerializeField] private float zoomSpeed = 5f;

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            UpdatePosition();
            UpdateProjectionSize();
        }

        /// <summary>
        /// Smoothly moves the camera towards the target position.
        /// </summary>
        private void UpdatePosition()
        {
            if (target == null)
            {
                // If the target is missing (e.g., destroyed), exit early.
                return;
            }
            var newPos = Vector3.Lerp(transform.position, target.position, Time.deltaTime * followSpeed);
            newPos.z = zOffset;
            transform.position = newPos;
        }

        /// <summary>
        /// Smoothly adjusts the camera's orthographic size based on input.
        /// </summary>
        private void UpdateProjectionSize()
        {
            if (mainCamera == null)
            {
                return;
            }
            var targetSize = Input.GetKey(zoomKey) ? projMax : projMin;
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
        }
    }
}
