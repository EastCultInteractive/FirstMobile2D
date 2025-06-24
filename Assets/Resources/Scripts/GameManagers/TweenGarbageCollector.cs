using UnityEngine;
using DG.Tweening;

namespace Resources.Scripts.GameManagers
{
    [DefaultExecutionOrder(-10000)]
    public class TweenGarbageCollector : MonoBehaviour
    {
        [Tooltip("Убивать твины при OnDisable() (по умолчанию — да)")]
        public bool killOnDisable = true;

        private void OnDisable()
        {
            if (killOnDisable)
            {
                DOTween.Kill(gameObject, complete: false);
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(gameObject, complete: false);
        }
    }
}