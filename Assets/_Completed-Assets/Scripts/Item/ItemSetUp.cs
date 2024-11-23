using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSetUp : MonoBehaviour
{
    public StaminaManager staminaManager;
    public ArmorPlusManager armorPlusManager;
    public StaminaUpManager staminaUpManager;
    public static ItemSetUp Instance { get; private set; }

    void Start(){
        staminaManager.AddStamina(3);
        armorPlusManager.AddArmorPlus(3,false);
        staminaUpManager.AddStaminaUp(3);
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