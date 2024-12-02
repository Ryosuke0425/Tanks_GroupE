using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUpManager : MonoBehaviour
{
    public static StaminaUpManager Instance {get;private set;}
    public StaminaUp staminaUp;
    public StaminaManager staminaManager;
    public Text staminaUpNumDisplay;
    public ItemSetUp itemSetUp;
    
    private void Awake(){
        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
    }

    public void AddStaminaUp(int quantity){
        if(staminaUp != null){
            staminaUp.quantity += quantity;
        }
        else{
            staminaUp = new StaminaUp(quantity);
        }
        staminaUpNumDisplay.text = "StaminaUp\nNum : " + staminaUp.quantity.ToString();
    }

    public bool UseStaminaUp(){
        if(staminaUp != null && staminaUp.quantity > 0 && staminaManager.AddStamina(1)){
            staminaUp.quantity--;
            staminaUpNumDisplay.text = "StaminaUp\nNum : " + staminaUp.quantity.ToString();
            itemSetUp.SendItem();
            return true;
        }
        return false;
    }
}