using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete;
public class CartridgeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject ShellCartridge;
    [SerializeField] private float spawnInterval = 2f;

    private Coroutine spawnCoroutine;
    private Complete.GameManager gameManager;

    [SerializeField] private CartridgeData shellCartridge;
    [SerializeField] private CartridgeData mineCartridge;
    private void OnEnable()
    {
        gameManager = FindObjectOfType<Complete.GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
        }
    }
    private void OnDisable()
    {
        gameManager = FindObjectOfType<Complete.GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(Complete.GameManager.GameState Current_GameState)
    {
        if (Current_GameState == Complete.GameManager.GameState.RoundPlaying)
        {
            if (spawnCoroutine == null)
            {
                spawnCoroutine = StartCoroutine(SpawnRoutine());
            }
        }
        else if (Current_GameState == Complete.GameManager.GameState.RoundEnding)
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }
    private void SpawnCartridge()
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(-10, 10),
            Random.Range(-10, 10),
            Random.Range(-10, 10)
        );

        // Instantiate(ShellCartridge, randomPosition, Quaternion.identity);
        Instantiate(shellCartridge.cartridgePrefab, shellCartridge.cartridgePosition, Quaternion.identity);
        Instantiate(mineCartridge.cartridgePrefab, mineCartridge.cartridgePosition, Quaternion.identity);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            Debug.Log("SpawnCartridge");
            SpawnCartridge();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(SpawnRoutine());
        OnEnable();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
