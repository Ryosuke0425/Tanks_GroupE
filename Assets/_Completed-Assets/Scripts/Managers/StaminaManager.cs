using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager Instance {get;private set;}
    public Stamina stamina;
    public Text staminaNumDisplay;
    
    private void Awake(){
        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
    }

    public bool AddStamina(int quantity){
        if(stamina != null){
            if(stamina.quantity > 4){
                return false;
            }
            stamina.quantity += quantity;
        }
        else{
            stamina = new Stamina(quantity);
        }
        staminaNumDisplay.text = "Stamina " + stamina.quantity.ToString();
        return true;
    }

    public bool UseStamina(){
        if(stamina != null && stamina.quantity > 0){
            stamina.quantity--;
            staminaNumDisplay.text = "Stamina " + stamina.quantity.ToString();
            return true;
        }
        return false;
    }
}