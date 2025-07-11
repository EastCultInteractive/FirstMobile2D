using UnityEngine;

namespace Resources.Scripts.Entity.Obstacles
{

    public class TumbleweedSpawner : MonoBehaviour
    {
        [Header("Prefab & Spawn Area")] public GameObject tumbleweedPrefab;

        [Tooltip("Нижняя‑левая точка области спавна (X, Y)")]
        public Vector2 minSpawnPos;

        [Tooltip("Верхняя‑правая точка области спавна (X, Y)")]
        public Vector2 maxSpawnPos;

        [Header("Spawn Control")] [Tooltip("Максимальное число одновременно активных перекати‑полей")]
        public int maxTumbleweeds = 3;

        private int currentCount;

        void Start()
        {
            // сразу создаём нужное количество
            for (int i = 0; i < maxTumbleweeds; i++)
                SpawnOne();
        }

        /// <summary>
        /// Создаёт один экземпляр перекати‑поля и считает его.
        /// </summary>
        private void SpawnOne()
        {
            Vector3 pos = new Vector3(
                Random.Range(minSpawnPos.x, maxSpawnPos.x),
                Random.Range(minSpawnPos.y, maxSpawnPos.y),
                0f
            );

            GameObject obj = Instantiate(tumbleweedPrefab, pos, Quaternion.identity, transform);
            var tw = obj.GetComponent<Tumbleweed>();
            if (tw != null)
                tw.spawner = this;
            else
                Debug.LogWarning("TumbleweedSpawner: у префаба нет компонента Tumbleweed!");

            currentCount++;
        }

        /// <summary>
        /// Вызывается из Tumbleweed перед Destroy: уменьшаем счётчик и (если нужно) спавним новый.
        /// </summary>
        public void NotifyTumbleweedDeath()
        {
            currentCount = Mathf.Max(0, currentCount - 1);

            // поддерживаем текущий count на уровне maxTumbleweeds
            if (currentCount < maxTumbleweeds)
                SpawnOne();
        }
    }
}