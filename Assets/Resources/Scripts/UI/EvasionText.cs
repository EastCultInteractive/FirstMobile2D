using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Resources.Scripts.Entity.UI
{
    /// <summary>
    /// Плавающий и исчезающий текст уклонения.
    /// Реализован через DOTween для плавности.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class EvasionText : MonoBehaviour
    {
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeTime = 1f;

        private TMP_Text tmpText;

        private void Awake()
        {
            tmpText = GetComponent<TMP_Text>();
            if (tmpText == null)
                Debug.LogError($"[{nameof(EvasionText)}] Не найден компонент TMP_Text на {gameObject.name}");
        }

        private void Start()
        {
            // Поднятие и исчезновение
            Vector3 endPos = transform.position + Vector3.up * floatSpeed * fadeTime;
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(endPos, fadeTime).SetEase(Ease.Linear).SetUpdate(true));
            seq.Join(tmpText.DOFade(0f, fadeTime).SetUpdate(true));
            seq.OnComplete(() => Destroy(gameObject));
        }
    }
}