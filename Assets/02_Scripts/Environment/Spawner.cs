using System.Collections;
using UnityEngine;

namespace MushOut.Environment
{
    /// <summary>
    /// 주기적으로 지정된 범위 내에 프리팹을 소환하는 스크립트입니다.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("소환할 대상 프리팹입니다.")]
        public GameObject prefabToSpawn;

        [Tooltip("소환 주기(초)입니다.")]
        public float spawnInterval = 5f;

        [Tooltip("소환 주기의 오차 범위(초)입니다. (예: 5초 ± 1초 = 4~6초 랜덤)")]
        public float spawnIntervalVariance = 1f;

        [Tooltip("한 번에 소환할 최소 개수입니다.")]
        public int minSpawnCount = 1;

        [Tooltip("한 번에 소환할 최대 개수입니다.")]
        public int maxSpawnCount = 3;

        [Tooltip("현재 오브젝트를 중심으로 한 소환 반경입니다.")]
        public float spawnRadius = 5f;

        [Tooltip("소환될 오브젝트의 각 축(X, Y, Z)별 최대 무작위 회전 각도입니다. (예: Y에 180을 넣으면 Y축으로 무작위 방향을 봅니다)")]
        public Vector3 maxRotationVariance = Vector3.zero;

        [Header("Lifetime Settings")]
        [Tooltip("소환된 오브젝트가 몇 초 뒤에 사라질지 설정합니다. (0 이하면 사라지지 않음)")]
        public float destroyTime = 10f;

        private void Start()
        {
            if (prefabToSpawn != null)
            {
                StartCoroutine(SpawnRoutine());
            }
            else
            {
                Debug.LogWarning($"[Spawner] {gameObject.name}의 Spawner 컴포넌트에 소환할 프리팹이 할당되지 않았습니다!");
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                // 1. 소환 주기 대기 (주기 ± 오차)
                float waitTime = spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
                waitTime = Mathf.Max(0.1f, waitTime); // 대기 시간이 음수가 되는 것 방지
                
                yield return new WaitForSeconds(waitTime);

                // 2. 소환 개수 결정 (min ~ max)
                int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

                for (int i = 0; i < spawnCount; i++)
                {
                    // 3. 소환 위치 결정 (X, Z 평면 기준 원형 범위 내 랜덤)
                    Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                    Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                    // 4. 소환 회전값 결정
                    float rotX = Random.Range(-maxRotationVariance.x, maxRotationVariance.x);
                    float rotY = Random.Range(-maxRotationVariance.y, maxRotationVariance.y);
                    float rotZ = Random.Range(-maxRotationVariance.z, maxRotationVariance.z);
                    Quaternion spawnRot = Quaternion.Euler(rotX, rotY, rotZ);

                    // 5. 소환
                    GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);

                    // 6. 소멸 시간 예약
                    if (destroyTime > 0f)
                    {
                        Destroy(spawnedObj, destroyTime);
                    }
                }
            }
        }

        /// <summary>
        /// 씬 뷰(에디터)에서 소환 범위를 시각적으로 확인하기 위해 원을 그립니다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 반투명한 시안색
            Gizmos.DrawSphere(transform.position, spawnRadius);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
