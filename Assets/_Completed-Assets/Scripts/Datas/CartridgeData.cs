using System;
using UnityEngine;

// CartridgeSpawnner - CartridgeData - MineCartridgePrefab
//                                   \ ShellCartridgePrefab

// CartridgeSpawnner - CartridgeData - MineCartridgePrefab
//                   \ CartridgeData - ShellCartridgePrefab

[Serializable]
public class CartridgeData : MonoBehaviour
{
    [SerializeField] public GameObject cartridgePrefab;
    public Vector3 cartridgePosition;
    public float generateFrequency;

    private Vector3 spawnAreaMin = new Vector3(-10, 1, -10);
    private Vector3 spawnAreaMax = new Vector3(10, 1, 10);

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 randomPosition = new Vector3(
            UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y),
            UnityEngine.Random.Range(spawnAreaMin.z, spawnAreaMax.z)
        );
        cartridgePosition = randomPosition;
    }
}
