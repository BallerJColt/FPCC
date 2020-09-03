namespace Robots
{
    using System.Collections;
    using UnityEngine;

    public class GasSpawner : MonoBehaviour
    {
        public float delay = 1f;

        public float timeBetweenSpawn = 0.25f;

        public float spawnRadius = 1f;

        public int count = 100;

        public Transform exhaustPoint;

        public GasRiser gasPrefab;
        private void Start()
        {
            StartCoroutine(nameof(RunSpawn));
        }

        private IEnumerator RunSpawn()
        {
            yield return new WaitForSeconds(delay);

            while (count-- > 0)
            {
                Vector3 dir = Random.insideUnitSphere;
                dir.y = 0f;
                dir *= spawnRadius;
                Vector3 pos = exhaustPoint.position + dir;
                GasOutputManager.Instance.OutputGas(pos, gasPrefab);
                yield return new WaitForSeconds(timeBetweenSpawn);
            }
        }
    }
}