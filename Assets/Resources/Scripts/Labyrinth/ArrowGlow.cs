using System.Collections;
using UnityEngine;

namespace Resources.Scripts.Labyrinth
{

    [RequireComponent(typeof(SpriteRenderer))]
    public class ArrowGlow : MonoBehaviour
    {
        [Header("Ссылки на стрелки (снизу вверх)")] [SerializeField]
        private SpriteRenderer[] arrows;

        [Header("Настройки свечения")] [SerializeField, Tooltip("Цвет покоя")]
        private Color baseColor = Color.gray;

        [SerializeField, Tooltip("Цвет свечения")]
        private Color glowColor = Color.green;

        [SerializeField, Tooltip("Время одного перехода (в секундах)")]
        private float glowDuration = 0.5f;

        [SerializeField, Tooltip("Задержка между стартами свечения соседних стрелок")]
        private float delayBetween = 0.2f;

        private void Start()
        {
            foreach (var sr in arrows)
                sr.color = baseColor;
            
            for (int i = 0; i < arrows.Length; i++)
            {
                StartCoroutine(WaveCoroutine(arrows[i], i));
            }
        }

        private IEnumerator WaveCoroutine(SpriteRenderer sr, int index)
        {
            yield return new WaitForSeconds(index * delayBetween);
            
            while (true)
            {
                // Плавно в светящийся (серый → зелёный)
                yield return StartCoroutine(FadeColor(sr, baseColor, glowColor, glowDuration));
                // Плавно обратно (зелёный → серый)
                yield return StartCoroutine(FadeColor(sr, glowColor, baseColor, glowDuration));

                // Перед следующим циклом нужно дождаться, пока волна «обойдет» все стрелки, 
                // чтобы не накладывать два цикла друг на друга.
                float fullWaveTime = delayBetween * (arrows.Length - 1) + glowDuration * 2;
                yield return new WaitForSeconds(fullWaveTime - index * delayBetween);
            }
        }

        private IEnumerator FadeColor(
            SpriteRenderer sr, Color from, Color to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                sr.color = Color.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            sr.color = to;
        }
    }
}