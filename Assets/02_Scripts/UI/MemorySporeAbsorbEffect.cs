using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushOut.UI
{
    public class MemorySporeAbsorbEffect : MonoBehaviour
    {
        [SerializeField] private GameObject memorySporeModelPrefab;
        [SerializeField] private float spawnHeight = 0.75f;
        [SerializeField] private float targetHeight = 1.35f;
        [SerializeField] private float popRadius = 0.65f;
        [SerializeField] private float popDuration = 0.22f;
        [SerializeField] private float flyDuration = 0.85f;
        [SerializeField] private float delayBetweenSpores = 0.075f;
        [SerializeField] private float arcHeight = 1.1f;
        [SerializeField] private float sporeScaleMultiplier = 1.8f;
        [SerializeField] private float cleanupDelay = 0.35f;

        private readonly List<Transform> _spores = new List<Transform>();
        private readonly List<Vector3> _originalScales = new List<Vector3>();

        public static MemorySporeAbsorbEffect Play(GameObject modelPrefab, Transform enemyRoot, Transform playerRoot)
        {
            if (modelPrefab == null || enemyRoot == null || playerRoot == null) return null;

            GameObject effectObject = new GameObject("MemorySporeAbsorbEffect");
            MemorySporeAbsorbEffect effect = effectObject.AddComponent<MemorySporeAbsorbEffect>();
            effect.memorySporeModelPrefab = modelPrefab;
            effect.Begin(enemyRoot, playerRoot);
            return effect;
        }

        public void Begin(Transform enemyRoot, Transform playerRoot)
        {
            if (memorySporeModelPrefab == null || enemyRoot == null || playerRoot == null)
            {
                Destroy(gameObject);
                return;
            }

            Transform enemyHead = FindHead(enemyRoot);
            Transform playerHead = FindHead(playerRoot);

            Vector3 source = (enemyHead != null ? enemyHead.position : enemyRoot.position) + Vector3.up * spawnHeight;
            Vector3 target = (playerHead != null ? playerHead.position : playerRoot.position) + Vector3.up * targetHeight;

            GameObject model = Instantiate(memorySporeModelPrefab, source, Quaternion.identity, transform);
            CollectSpores(model.transform);

            if (_spores.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(PlayRoutine(source, target));
        }

        private void CollectSpores(Transform root)
        {
            _spores.Clear();
            _originalScales.Clear();

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Transform spore = renderers[i].transform;
                _spores.Add(spore);
                _originalScales.Add(spore.localScale);
                spore.gameObject.SetActive(false);
            }
        }

        private IEnumerator PlayRoutine(Vector3 source, Vector3 target)
        {
            for (int i = 0; i < _spores.Count; i++)
            {
                StartCoroutine(AnimateSpore(i, source, target));
                yield return new WaitForSeconds(delayBetweenSpores);
            }

            yield return new WaitForSeconds(popDuration + flyDuration + cleanupDelay);
            Destroy(gameObject);
        }

        private IEnumerator AnimateSpore(int index, Vector3 source, Vector3 target)
        {
            Transform spore = _spores[index];
            Vector3 originalScale = _originalScales[index] * sporeScaleMultiplier;
            Vector3 popOffset = Random.onUnitSphere;
            popOffset.y = Mathf.Abs(popOffset.y) + 0.35f;
            popOffset = popOffset.normalized * Random.Range(popRadius * 0.45f, popRadius);

            Vector3 popPosition = source + popOffset;

            spore.gameObject.SetActive(true);
            spore.position = source;
            spore.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                float t = elapsed / popDuration;
                float eased = EaseOutBack(t);
                spore.position = Vector3.LerpUnclamped(source, popPosition, eased);
                spore.localScale = Vector3.LerpUnclamped(Vector3.zero, originalScale, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            spore.position = popPosition;
            spore.localScale = originalScale;

            elapsed = 0f;
            while (elapsed < flyDuration)
            {
                float t = elapsed / flyDuration;
                float eased = t * t * (3f - 2f * t);
                Vector3 line = Vector3.Lerp(popPosition, target, eased);
                line.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                spore.position = line;
                spore.localScale = Vector3.Lerp(originalScale, originalScale * 0.2f, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            spore.gameObject.SetActive(false);
        }

        private static Transform FindHead(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string childName = children[i].name;
                if (childName == "Head" || childName == "head")
                {
                    return children[i];
                }
            }

            return null;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
