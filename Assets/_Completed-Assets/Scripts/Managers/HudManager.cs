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
    [SerializeField] private GameObject HPUIyour;//ゲーム修了時HUDを非表示にする
    [SerializeField] private GameObject HPUIenemy;
    [SerializeField] private GameObject VStext;
    //public static Complete.TankShooting shooting;
    public static Complete.TankShooting shooting = new Complete.TankShooting();
    private int firstHold = shooting.Bullets_start_hold;
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

    private void HandleWeaponStockChanged(int playerNumber, int shellStock, WeaponStockData mineStock)//通信対戦で変える必要があるかも
    {
        Debug.Log("HandleWeaponStockChanged");
        if (playerNumber == 1)
        {
            player1StockArea.UpdatePlayerStockArea(shellStock, mineStock);
        }
        else if (playerNumber == 2)
        {
            player2StockArea.UpdatePlayerStockArea(shellStock, mineStock);
        }
    }
    private void SetHUDVisibility(bool isVisible)
    {
        player1StockArea.gameObject.SetActive(isVisible);
        player2StockArea.gameObject.SetActive(isVisible);
        HPUIyour.gameObject.SetActive(isVisible);
        HPUIenemy.gameObject.SetActive(isVisible);
        VStext.gameObject.SetActive(isVisible);
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
    private void FirstHoldState() //最初の弾数を表示する
    {
        player1StockArea.UpdatePlayerStockArea(firstHold, new WeaponStockData(0, 3, 1));
        player2StockArea.UpdatePlayerStockArea(firstHold, new WeaponStockData(0, 3, 1));   
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(firstHold);
        FirstHoldState();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
