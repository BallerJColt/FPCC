namespace Robots
{
    using UnityEngine;

    /// <summary>
    /// Rises to the gas height and then moves to destination
    /// </summary>
    public class GasRiser : MonoBehaviour
    {
        /// <summary>
        /// Speed the gas is moving
        /// </summary>
        public float speed = 1f;

        /// <summary>
        /// Size of the gas particle system, used to make the size smaller when it exits the exhaust pipe
        /// </summary>
        [Header("Movement while rising")]
        public AnimationCurve sizeOverTime;
        
        public float spinFrequency = 1f;
        public AnimationCurve spinAmplitude;

        [Header("Movement at target height")]
        public float heightOscillationAmp = 1;
        public float heightOscillationFreq = 1f;
        public float sizeOscillationAmp = 1f;
        public float sizeOscillationFreq = 1f;
        
        [Header("Approach behavior")] public float horizontalApproach;
        public float verticalApproach;


        [Header("Lifetime and prefab settings")]
        public bool canExpire;
        public float maxLifeTime = 5f;
        public string prefabId;

        private float _lifeTime;

        private float _gasHeight;

        private Vector3 _destination;

        private ParticleSystem _particleSystem;

        private float _startHeight;

        private float _distanceToTravel;

        private bool _isRising;
        
        private float _amplitude;
        
        private float _spinOffset;

        private float _oscillationTimer;
        
        public void Initialize(Vector3 destination, float gasHeight)
        {
            _startHeight = transform.position.y;
            _destination = destination;
            _gasHeight = gasHeight;
            _distanceToTravel = _destination.y - _startHeight;
            _isRising = true;
            _lifeTime = 0f;

            _spinOffset = Random.Range(0f, 2 * Mathf.PI);
        }

        private void OnValidate()
        {
            //Important for the pool
            if (prefabId == "")
            {
                Debug.LogError(name + " has no prefab ID");
            }
        }

        private void OnEnable()
        {
            ResetScale();
        }

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
        }

        private void Update()
        {
            Tick();
        }

        private void Tick()
        {
            // If the gas can expire return it to the correct pool
            _lifeTime += Time.deltaTime;
            if (canExpire && _lifeTime > maxLifeTime)
            {
                GasRiserPool.Instance.ReturnToPool(this);
                return;
            }
            
            
            Vector3 ownPos = transform.position;

            //direction to the end destination
            Vector3 direction = (_destination - ownPos);

            float distanceToTarget = direction.magnitude;

            Vector3 dirNorm = direction.normalized;

            Vector3 velocity = dirNorm * speed * Time.deltaTime;
            velocity.y = 0; //Height is set in Rise() or at oscillation anyway
            
            //approach behavior for separation
            if (horizontalApproach > 0 && distanceToTarget < horizontalApproach)
            {
                velocity *= distanceToTarget / horizontalApproach;
            }

            Vector3 destination = ownPos + velocity;

            //get the ground position for the next step
            Ray ray = new Ray(ownPos + Vector3.up * 500f, Vector3.down);

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, GasOutputManager.Instance.terrainLayerMask))
            {
                return;
            }

            Vector3 groundPos = hit.point;
            destination.y = groundPos.y + _gasHeight;
            if (_isRising)
            {
                //we are below the desired destination, so move vertically
                Rise(ownPos, destination);

                float distanceToDest = destination.y - ownPos.y;

                //apply separation vertically if approach started, resulting in "blooming" gas effect
                if (distanceToDest < verticalApproach)
                {
                    velocity *= 1 - distanceToDest / verticalApproach;
                    transform.position += velocity;
                }

                return;
            }
            
            //apply separation (if it's large enough so the gas eventually stops moving)
            if (velocity.magnitude > Mathf.Epsilon)
            {
                transform.position += velocity;
            }

            OscillateScale(_oscillationTimer);

            //Calculate oscillation at target height
            if (heightOscillationFreq <= Mathf.Epsilon)
            {
                return;
            }
            float oscillation = heightOscillationAmp * Mathf.Cos(_oscillationTimer * heightOscillationFreq);
            Vector3 oscillationVector = new Vector3(0f,oscillation,0f);
            Vector3 oscillationVelocity = oscillationVector * Time.deltaTime;
            
            _oscillationTimer += Time.deltaTime;
            
            //apply oscillation
            transform.position += oscillationVelocity;
        }

        private void Rise(Vector3 ownPos, Vector3 dest)
        {
            Vector3 velVertical = Vector3.up * speed * Time.deltaTime;
            float magVelVertical = velVertical.magnitude;
            float distToDest = dest.y - ownPos.y;

            if (verticalApproach > 0 && distToDest < verticalApproach)
            {
                velVertical *= distToDest / verticalApproach;
            }
            else //when approaching, don't apply the spin movement anymore as the separation already started
            {
                //circular movement while rising
                _amplitude = EvaluateCurve(spinAmplitude);
                velVertical.x = _amplitude * Mathf.Cos(_spinOffset + Time.time * spinFrequency) * Time.deltaTime;
                velVertical.z = _amplitude * Mathf.Sin(_spinOffset + Time.time * spinFrequency) * Time.deltaTime;
            }

            if (magVelVertical < distToDest)
            {
                //move the gas upwards towards the destination
                transform.position += velVertical;
            }
            else
            {
                //one step further would take us beyond the vertical destination, to snap the position to the vertical destination
                ownPos.y = dest.y;
                transform.position = ownPos;
            }

            if (ownPos.y >= dest.y)
            {
                _isRising = false;
                ResetScale();
            }
            else
            {
                AdjustScale();
            }
        }

        private void AdjustScale()
        {
            float size = EvaluateCurve(sizeOverTime);

            transform.localScale = new Vector3(size, size, size);
        }

        private void ResetScale()
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }

        private void OscillateScale(float timer)
        {
            float oscillation = 1 - sizeOscillationAmp * (1 - Mathf.Cos(sizeOscillationFreq * timer));

            transform.localScale = new Vector3(oscillation, oscillation, oscillation);
        }

        //moved the curve evaluation out of the original method so I could reuse it
        private float EvaluateCurve(AnimationCurve curve)
        {
            float distanceTravelled = transform.position.y - _startHeight;

            float fraction = Mathf.Clamp(distanceTravelled / _distanceToTravel, 0f, 1f);

            return curve.Evaluate(fraction);
        }
    }
}