using UnityEngine;

namespace Resources.Scripts.Entity.Obstacles
{

    public class Tumbleweed : MonoBehaviour
    {
        [Header("Movement & Rotation")] public float moveSpeed = 2f; // скорость движения
        public float rotationSpeed = 180f; // градусы в секунду

        [Header("Lifetime & Fade")] public float minLife = 3f; // min время жизни
        public float maxLife = 5f; // max время жизни
        public float fadeDuration = 1f; // длительность фейда

        [HideInInspector] public TumbleweedSpawner spawner;

        private Vector3 moveDirection;
        private float lifeTime;
        private SpriteRenderer spriteRenderer;
        private bool isFading;
        private float fadeTimer;

        void Start()
        {
            moveDirection = Random.value < 0.5f ? Vector3.left : Vector3.right;
            lifeTime = Random.Range(minLife, maxLife);
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogWarning("Tumbleweed: нет SpriteRenderer-а на объекте!");
        }

        void Update()
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);

            if (!isFading)
            {
                lifeTime -= Time.deltaTime;
                if (lifeTime <= 0f)
                    isFading = true;
            }

            if (isFading && spriteRenderer != null)
            {
                fadeTimer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);

                if (fadeTimer >= fadeDuration)
                {
                    // оповестим спавнер о смерти
                    if (spawner != null)
                        spawner.NotifyTumbleweedDeath();

                    Destroy(gameObject);
                }
            }
        }
    }
}