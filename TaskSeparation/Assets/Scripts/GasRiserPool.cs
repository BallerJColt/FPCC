namespace Robots
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// System in place for instantiating and disabling gasses. If the gas risers are not set to expire,
    /// this should work exactly as the previous system. If they are set to expire, this will reuse
    /// the expired gas risers instead of deleting them.
    /// </summary>
    public class GasRiserPool : MonoBehaviour
    {
        public bool initializePool;
        public int initialPoolSize = 100;

        [Tooltip("Drag GasRiser prefabs here to initialize the pool before game start")]
        public List<GasRiser> prefabList = new List<GasRiser>(); //for authoring

        public static GasRiserPool Instance;

        /// <summary>
        /// Dictionary to make instantiation easier and faster, but dictionaries are not serialized in the editor
        /// so I'm using a List and transforming that in the Initialize() method.
        /// </summary>
        private Dictionary<string, GasRiser> prefabDict = new Dictionary<string, GasRiser>();

        private Dictionary<string, Queue<GasRiser>> poolDictionary = new Dictionary<string, Queue<GasRiser>>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!initializePool)
            {
                return;
            }
            
            if (prefabList.Count == 0)
            {
                Debug.LogWarning("The initialization list in " + gameObject.name + " is empty!");
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            foreach (GasRiser gas in prefabList)
            {
                if (IsDuplicateKey(gas))
                {
                    Debug.LogError(gas.name + " and " + prefabDict[gas.prefabId].name + " cannot have the same prefabID!");
                    continue;
                }
                prefabDict.Add(gas.prefabId, gas);
                poolDictionary.Add(gas.prefabId, new Queue<GasRiser>());
                AddToPool(gas.prefabId, initialPoolSize);
            }
        }

        private void AddToPool(string key, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                GasRiser gas = Instantiate(prefabDict[key]);
                gas.gameObject.SetActive(false);
                poolDictionary[key].Enqueue(gas);
            }
        }

        /// <summary>
        /// Two different prefabs cannot be entered in the dictionary with the same prefabID.
        /// This method returns true if the GasRiser we want to add has the same ID as an already added GasRiser
        /// </summary>
        private bool IsDuplicateKey(GasRiser gas)
        {
            return prefabDict.ContainsKey(gas.prefabId) && prefabDict[gas.prefabId] != gas;
        }

        public GasRiser Get(GasRiser gas)
        {
            if (!poolDictionary.ContainsKey(gas.prefabId))
            {
                prefabDict.Add(gas.prefabId, gas);
                poolDictionary.Add(gas.prefabId, new Queue<GasRiser>());
            }
            else if (IsDuplicateKey(gas))
            {
                Debug.LogError(gas.name + " and " + prefabDict[gas.prefabId].name + " cannot have the same prefabID!");
                return null;
            }

            if (poolDictionary[gas.prefabId].Count == 0)
            {
                AddToPool(gas.prefabId, 1);
            }

            return poolDictionary[gas.prefabId].Dequeue();
        }

        public void ReturnToPool(GasRiser gas)
        {
            GasOutputManager.Instance.RemoveFromActive(gas);
            gas.gameObject.SetActive(false);
            poolDictionary[gas.prefabId].Enqueue(gas);
        }
    }
}