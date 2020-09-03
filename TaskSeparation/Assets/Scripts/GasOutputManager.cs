namespace Robots
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Random = Unity.Mathematics.Random;

    /// <summary>
    /// Manages the output and diffusion gasses into the atmosphere of the asteroid - received messages from e.g. the converter
    /// </summary>
    public class GasOutputManager : MonoBehaviour
    {
        public static GasOutputManager Instance;

        /// <summary>
        /// The height above the navmesh the gas is floating in
        /// </summary>
        public float gasHeight = 24f;

        /// <summary>
        /// The minimum separation between gas -> i.e. the gas systems a repulsing each other
        /// </summary>
        public float separationRadius = 4f;

        /// <summary>
        /// The frequency where new separation is calculated
        /// </summary>
        public float separationTickFrequency = 1f;

        public LayerMask terrainLayerMask;

        public bool logDebug;

        /// <summary>
        /// Next time the separation loop is run
        /// </summary>
        private float _nextSeparationTick = 0;

        /// <summary>
        /// List handling the registration of system
        /// </summary>
        private HashSet<GasRiser> _activeGas = new HashSet<GasRiser>();

        private Stopwatch _stopwatch;

        private Random _random;
        
        //Two native arrays handling the parallelized separation calculations
        //The buffer size can expand if there are more active gas systems than space
        private int _bufferSize = 64;
        private NativeArray<float3> positions;
        private NativeArray<float3> separations;


        private void Awake()
        {
            Instance = this;
            
            positions = new NativeArray<float3>(_bufferSize, Allocator.Persistent);
            separations = new NativeArray<float3>(_bufferSize, Allocator.Persistent);
            
            _stopwatch = new Stopwatch();
            
            //The Mathematics.Random's seed cannot be left empty. 
            uint seed = (uint) UnityEngine.Random.Range(0, int.MaxValue); 
            _random = new Random(seed);
        }

        private void Update()
        {
            Tick();
        }

        private void OnDisable()
        {
            positions.Dispose();
            separations.Dispose();
        }

        private void Tick()
        {
            SeparateGas();
        }

        public void OutputGas(Vector3 exhaustPoint, GasRiser gasPrefab)
        {
            Vector3 gasDestination = GetGasDestination(exhaustPoint);
            
            GasRiser gas = GasRiserPool.Instance.Get(gasPrefab);
            
            //should really only happen if there is a duplicate prefabID
            if (gas == null)
            {
                return;
            }
            
            Transform gasTransform = gas.transform;
            gasTransform.position = exhaustPoint;
            gas.gameObject.SetActive(true);
            gasTransform.localScale *= gas.sizeOverTime.Evaluate(0f);
            _activeGas.Add(gas);

            gas.Initialize(gasDestination, gasHeight);
        }

        public void RemoveFromActive(GasRiser gasRiser)
        {
            _activeGas.Remove(gasRiser);
        }

        private void SeparateGas()
        {
            _stopwatch.Start();
            
            if (_bufferSize < _activeGas.Count)
            {
                ResizeBuffer();
            }
            
            UpdatePositions();

            //initializing the job
            SeparationJob separationJob = new SeparationJob
            {
                separationVectors = separations,
                positionVectors = positions,
                separation = separationRadius,
                epsilon = Mathf.Epsilon,
                random = _random
            };

            JobHandle handle = separationJob.Schedule(separations.Length, 16);
            handle.Complete();

            int i = _activeGas.Count - 1;
            foreach (GasRiser gas in _activeGas)
            {
                Vector3 destination = positions[i] + separations[i];
                gas.SetDestination(destination);
                i--;
            }
            
            _stopwatch.Stop();

            if (logDebug)
                UnityEngine.Debug.Log("_stopwatch == " + _stopwatch.ElapsedMilliseconds + " " +
                                      _stopwatch.ElapsedTicks);
        }

        private Vector3 GetGasDestination(Vector3 exhaustPosition)
        {
            Vector3 pos = exhaustPosition;
            pos += Vector3.up * gasHeight;
            return pos;
        }

        private void ResizeBuffer()
        {
            positions.Dispose();
            separations.Dispose();

            _bufferSize = _activeGas.Count + 64;
            positions = new NativeArray<float3>(_bufferSize, Allocator.Persistent);
            separations = new NativeArray<float3>(_bufferSize, Allocator.Persistent);
        }

        private void UpdatePositions()
        {
            int i = _activeGas.Count - 1;
            foreach (GasRiser gas in _activeGas)
            {
                positions[i] = gas.transform.position;
                i--;
            }
        }
    }
}