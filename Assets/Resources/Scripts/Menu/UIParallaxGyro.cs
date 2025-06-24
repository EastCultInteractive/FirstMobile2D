using UnityEngine;
using System;

[RequireComponent(typeof(RectTransform))]
public class UIParallaxGyro : MonoBehaviour
{
    [Serializable]
    public struct Layer
    {
        [Tooltip("RectTransform слоя (картинка, фон и т.п.)")]
        public RectTransform rect;
        [Tooltip("Чем выше — сильнее смещение")]
        public float depthMultiplier;
        [Tooltip("Контейнер, за границы которого слой не должен выходить (необязательно)")]
        public RectTransform boundary;
    }

    [Header("Слои для параллакса")]
    public Layer[] layers;

    [Header("Общая чувствительность")]
    [Range(0.1f, 5f)]
    public float sensitivity = 1f;

    [Header("Максимальный угол наклона (в градусах)")]
    [Range(5f, 45f)]
    public float maxTiltAngle = 30f;

    [Header("Мёртвая зона (для устранения мелких дрожаний)")]
    [Range(0f, 0.2f)]
    public float deadZone = 0.02f;

    [Header("Скорость сглаживания Tilt (секунд)")]
    [Tooltip("Чем меньше — тем быстрее отклик, но возможны рывки")]
    public float tiltSmoothTime = 0.1f;

    [Header("Скорость сглаживания позиции (секунд)")]
    public float positionSmoothTime = 0.1f;

    [Header("Эмулировать гироскоп в Editor")]
    [Tooltip("Если включено, в Editor будет использоваться ввод стрелками")]
    public bool simulateInEditor = false;

    // --- внутренние поля ---
    private bool gyroSupported;
    private Quaternion correctionQuaternion;
    private Quaternion zeroAttitude;
    private bool zeroCalibrated = false;

    private Vector2 currentTilt = Vector2.zero;
    private Vector2 tiltVelocity = Vector2.zero;

    private Vector2[] posVelocity;  // для SmoothDamp каждой позиции

    void Awake()
    {
        posVelocity = new Vector2[layers.Length];
    }

    void Start()
    {
        gyroSupported = SystemInfo.supportsGyroscope;
        if (gyroSupported)
        {
            Input.gyro.updateInterval = 1f / 60f;
            Input.gyro.enabled = true;
            correctionQuaternion = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            Debug.LogWarning("Гироскоп не поддерживается на этом устройстве.");
        }
    }

    void Update()
    {
        // 1) Получаем «сырые» данные наклона
        Vector2 rawTilt = Vector2.zero;

        if (Application.isEditor && simulateInEditor)
        {
            rawTilt.x = Input.GetAxis("Horizontal");
            rawTilt.y = Input.GetAxis("Vertical");
        }
        else
        {
            if (!gyroSupported) return;

            Quaternion deviceAtt = Input.gyro.attitude;
            Quaternion adjusted = correctionQuaternion * new Quaternion(
                -deviceAtt.x, -deviceAtt.y, deviceAtt.z, deviceAtt.w);

            if (!zeroCalibrated)
            {
                zeroAttitude = adjusted;
                zeroCalibrated = true;
            }

            Quaternion relative = Quaternion.Inverse(zeroAttitude) * adjusted;
            // Преобразуем к углам Эйлера (градусы)
            Vector3 angles = relative.eulerAngles;
            // Unity возвращает [0..360], пересчитаем в [-180..+180]
            float tiltX = Mathf.DeltaAngle(0, angles.x);
            float tiltY = Mathf.DeltaAngle(0, angles.y);
            rawTilt = new Vector2(tiltX, tiltY) / maxTiltAngle;
        }

        // 2) Применяем мёртвую зону
        if (Mathf.Abs(rawTilt.x) < deadZone) rawTilt.x = 0f;
        if (Mathf.Abs(rawTilt.y) < deadZone) rawTilt.y = 0f;

        // 3) Сглаживаем Tilt (SmoothDamp)
        currentTilt = Vector2.SmoothDamp(currentTilt, rawTilt, ref tiltVelocity, tiltSmoothTime);

        // 4) Для каждого слоя рассчитываем целевую позицию и плавно переходим к ней
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.rect == null) continue;

            // targetOffset в пикселях
            Vector2 targetOffset = currentTilt * layer.depthMultiplier * sensitivity * 100f;

            // Clamp по границам, если задан boundary
            if (layer.boundary != null)
            {
                Rect b = layer.boundary.rect;
                Rect r = layer.rect.rect;
                float maxX = Mathf.Max(0f, (b.width  - r.width ) / 2f);
                float maxY = Mathf.Max(0f, (b.height - r.height) / 2f);
                targetOffset.x = Mathf.Clamp(targetOffset.x, -maxX, maxX);
                targetOffset.y = Mathf.Clamp(targetOffset.y, -maxY, maxY);
            }

            // SmoothDamp к целевой позиции
            Vector2 currentPos = layer.rect.anchoredPosition;
            layer.rect.anchoredPosition = Vector2.SmoothDamp(
                currentPos, targetOffset, ref posVelocity[i], positionSmoothTime);
        }
    }

    /// <summary>
    /// Ручная перекалибровка «нуля». Повесьте на UI-кнопку для сброса ориентации.
    /// </summary>
    public void RecalibrateZero()
    {
        if (!gyroSupported) return;
        Quaternion deviceAtt = Input.gyro.attitude;
        Quaternion adjusted = correctionQuaternion * new Quaternion(
            -deviceAtt.x, -deviceAtt.y, deviceAtt.z, deviceAtt.w);
        zeroAttitude = adjusted;
    }
}
