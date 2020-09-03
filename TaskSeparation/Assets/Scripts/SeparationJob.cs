namespace Robots
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    /// <summary>
    /// Job responsible for calculating the separation vectors.
    /// Same as the GetSeparationVector method, but parallelized,
    /// so we avoid the double foreach loop on the _activeGas set.
    /// </summary>
    [BurstCompile]
    public struct SeparationJob : IJobParallelFor
    {
        public NativeArray<float3> separationVectors;
        [ReadOnly] public NativeArray<float3> positionVectors;
        [ReadOnly] public float separation;
        [ReadOnly] public float epsilon; //we don't have access to Mathf.Epsilon in the struct so we supply it from outside
        [ReadOnly] public Random random;

        public void Execute(int index)
        {
            if (separationVectors.Length == 0)
            {
                return;
            }

            int separatorCount = 0;
            separationVectors[index] = float3.zero;

            for (int j = 0; j < positionVectors.Length; j++)
            {
                //If the position we're checking against is the same
                if (j == index)
                {
                    continue;
                }

                float3 directionToOther = positionVectors[index] - positionVectors[j];
                float magnitude = math.length(directionToOther);

                //If the other object is farther than the minimum separation
                if (magnitude > separation)
                {
                    continue;
                }

                // The systems are located at the same position so they need to separate
                if (magnitude < epsilon)
                {
                    float2 randomDirection = random.NextFloat2Direction();
                    directionToOther = new float3(randomDirection.x, 0f, randomDirection.y);
                }

                separationVectors[index] += math.normalize(directionToOther) * (separation - magnitude);
                separatorCount++;
            }

            if (separatorCount > 0)
            {
                separationVectors[index] /= separatorCount;
            }
        }
    }
}