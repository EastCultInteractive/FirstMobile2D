using System;
using UnityEngine;

namespace Resources.Scripts.Entity.Player
{
    public class PlayerJoystick : MonoBehaviour
    {
        [Header("Joystick Handle Settings")]
        [SerializeField, Range(50f, 500f)] private float handleRange = 50f;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _handleRect;
        private RectTransform _backgroundRect;
        private RectTransform _joystickRect;
        
        private void Awake()
        {
            _canvas = transform.parent.GetComponent<Canvas>();
            _canvasRect = _canvas.GetComponent<RectTransform>();
            _handleRect = transform.Find("Handle").GetComponent<RectTransform>();
            _backgroundRect = transform.Find("Background").GetComponent<RectTransform>();
            _joystickRect = transform.GetComponent<RectTransform>();
            
            _handleRect.gameObject.SetActive(false);
            _backgroundRect.gameObject.SetActive(false);
        }

        private void Update()
        {
            foreach (var touch in Input.touches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        ActivateJoystick(touch.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleDrag(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        ReleaseJoystick();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private Vector2 TouchPositionToRect(RectTransform rect, Vector2 screenPosition)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect,
                    screenPosition,
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                    out var localPoint))
            {
                return localPoint;
            }

            return Vector2.zero;
        }

        private void ActivateJoystick(Vector2 screenPosition)
        {
            _handleRect.gameObject.SetActive(true);
            _backgroundRect.gameObject.SetActive(true);
            
            _joystickRect.anchoredPosition = TouchPositionToRect(_canvasRect, screenPosition);
        }

        private void HandleDrag(Vector2 screenPosition)
        {
            var localPosition = TouchPositionToRect(_joystickRect, screenPosition);
            InputVector = localPosition - _backgroundRect.anchoredPosition;
            
            _handleRect.anchoredPosition = Vector3.ClampMagnitude(InputVector, handleRange);
        }

        private void ReleaseJoystick()
        {
            _handleRect.gameObject.SetActive(false);
            _backgroundRect.gameObject.SetActive(false);
            InputVector = Vector2.zero;
        }

        public Vector2 InputVector { get; private set; }
    }
}
