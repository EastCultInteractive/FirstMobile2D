using System;
using UnityEngine;

namespace Resources.Scripts.Entity.Player
{
    public class PlayerJoystick : MonoBehaviour
    {
        [Header("Joystick Handle Settings")]
        [SerializeField, Range(50f, 500f)] private float handleRange = 50f;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform handleRect;
        private RectTransform backgroundRect;
        private RectTransform joystickRect;
        
        private void Awake()
        {
            canvas = transform.parent.GetComponent<Canvas>();
            canvasRect = canvas.GetComponent<RectTransform>();
            handleRect = transform.Find("Handle").GetComponent<RectTransform>();
            backgroundRect = transform.Find("Background").GetComponent<RectTransform>();
            joystickRect = transform.GetComponent<RectTransform>();
            
            handleRect.gameObject.SetActive(false);
            backgroundRect.gameObject.SetActive(false);
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
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out var localPoint))
            {
                return localPoint;
            }

            return Vector2.zero;
        }

        private void ActivateJoystick(Vector2 screenPosition)
        {
            handleRect.gameObject.SetActive(true);
            backgroundRect.gameObject.SetActive(true);
            
            joystickRect.anchoredPosition = TouchPositionToRect(canvasRect, screenPosition);
        }

        private void HandleDrag(Vector2 screenPosition)
        {
            var localPosition = TouchPositionToRect(joystickRect, screenPosition);
            InputVector = localPosition - backgroundRect.anchoredPosition;
            
            handleRect.anchoredPosition = Vector3.ClampMagnitude(InputVector, handleRange);
        }

        private void ReleaseJoystick()
        {
            handleRect.gameObject.SetActive(false);
            backgroundRect.gameObject.SetActive(false);
            InputVector = Vector2.zero;
        }

        public Vector2 InputVector { get; private set; }
    }
}
