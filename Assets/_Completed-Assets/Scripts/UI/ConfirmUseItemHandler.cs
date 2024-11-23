using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmUseStaminaUpHandler : MonoBehaviour
{
    public GameObject confirmUseItemPanel;
    public Text confimUseItemText;
    public GameObject resultUseItemPanel;
    public StaminaUpManager staminaUpManager;
    public ArmorPlusManager armorPlusManager;
    public Text resultText;
    public Button yesUseItem;
    public static ConfirmUseStaminaUpHandler Instance {get; private set; }

    //「Use ~ ? を表示する」
    public void ShowConfirmUseItem(string itemName){
        confirmUseItemPanel.SetActive(true);
        confimUseItemText.text = "Use " + itemName + " ?";
        yesUseItem.onClick.RemoveAllListeners();
        yesUseItem.onClick.AddListener(() => OnConfirmButtonClicked(itemName));
    }

    //「use ~ ?」 => 「はい」のクリックイベント
    public void OnConfirmButtonClicked(string itemName){
        bool success = false;
        if(itemName == "StaminaUp"){
            success = staminaUpManager.UseStaminaUp();
        }else if(itemName == "ArmorPlus"){
            success = armorPlusManager.UseArmorPlus();
        }

        if(success){
            ShowResultUseItem("Success to use "+ itemName);
        }else{
            ShowResultUseItem("Failed to use " + itemName);
        }
        confirmUseItemPanel.SetActive(false);
    }

    //「No」ボタンのクリックイベント
    public void OnCancelButtonClicked(){
        confirmUseItemPanel.SetActive(false);
    }

    //「Success to use ~/Failed to use ~」 の表示
    public void ShowResultUseItem(string text){
        resultUseItemPanel.SetActive(true);
        resultText.text = text;
    }

    //「Success to use ~ / Failed to use ~」 -> 「OK」ボタンのクリックイベント
    public void OnResultUseItemClicked(){
        resultUseItemPanel.SetActive(false);
    }

    private void Awake(){
        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
    }
}
