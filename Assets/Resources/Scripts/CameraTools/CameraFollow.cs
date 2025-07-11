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

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            UpdatePosition();
            UpdateProjectionSize();
        }

        private void UpdatePosition()
        {
            var newPos = Vector3.Lerp(transform.position, target.position, Time.deltaTime * followSpeed);
            newPos.z = zOffset;
            transform.position = newPos;
        }

        private void UpdateProjectionSize()
        {
            var targetSize = Input.GetKey(zoomKey) ? projMax : projMin;
            _mainCamera.orthographicSize = Mathf.Lerp(_mainCamera.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
        }
    }
}
