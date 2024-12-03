using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete;
public class CartridgeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject ShellCartridge;
    Vector3 spawnAreaMin = new Vector3(-10, 1, -10);         //ê∂ê¨îÕàÕÇÃéwíË
    Vector3 spawnAreaMax = new Vector3(10, 1, 10);
    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private CartridgeData cartridgeData;

    private Coroutine spawnCoroutine;
    private Complete.GameManager gameManager;
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
        Instantiate(ShellCartridge, cartridgeData.spawnPosition, Quaternion.identity);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
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
