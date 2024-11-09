using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete;
using UnityEngine.UI;
public class HudManager : MonoBehaviour
{
    [SerializeField] private PlayerStockArea player1StockArea;
    [SerializeField] private PlayerStockArea player2StockArea;
    [SerializeField] private Complete.GameManager gameManager;

    private void HandleGameStateChanged(Complete.GameManager.GameState Current_GameState)//機能していた
    {
        switch (Current_GameState)
        {
            case Complete.GameManager.GameState.RoundStarting:
                SetHUDVisibility(false);
                break;
            case Complete.GameManager.GameState.RoundPlaying:
                SetHUDVisibility(true);
                break;
            case Complete.GameManager.GameState.RoundEnding:
                SetHUDVisibility(false);
                break;
        }
    }

    private void HandleWeaponStockChanged(int playerNumber, int shellStock)
    {
        Debug.Log("HandleWeaponStockChanged");
        if (playerNumber == 1)
        {
            player1StockArea.UpdatePlayerStockArea(shellStock);
        }
        else if (playerNumber == 2)
        {
            player2StockArea.UpdatePlayerStockArea(shellStock);
        }
    }
    private void SetHUDVisibility(bool isVisible)
    {
        player1StockArea.gameObject.SetActive(isVisible);
        player2StockArea.gameObject.SetActive(isVisible);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        foreach (var tank in gameManager.m_Tanks)
        {
            tank.OnWeaponStockChanged -= HandleWeaponStockChanged;
        }
    }
    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        foreach (var tank in gameManager.m_Tanks)
        {
            tank.OnWeaponStockChanged += HandleWeaponStockChanged;          //OnWeaponStockChangedへのリスナーの登録
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
