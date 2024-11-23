using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArmorPlusManager : MonoBehaviour
{
    public static ArmorPlusManager Instance {get;private set;}
    public ArmorPlus armorPlus;
    public Text armorPlusNumDisplay;
    public Text armorStateDisplay;
    
    private void Awake(){
        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
    }

    public void AddArmorPlus(int quantity,bool used){
        if(armorPlus != null){
            armorPlus.quantity += quantity;
        }
        else{
            armorPlus = new ArmorPlus(quantity,used);
        }
        armorPlusNumDisplay.text = "ArmorPlus\nNum : " + armorPlus.quantity.ToString();
    }

    public bool UseArmorPlus(){
        if(armorPlus != null && armorPlus.quantity > 0 && !armorPlus.used){
            armorPlus.quantity--;
            armorPlus.used = true;
            armorPlusNumDisplay.text = "ArmorPlus\nNum : " + armorPlus.quantity.ToString();
            armorStateDisplay.text = "Armor *2";
            return true;
        }
        return false;
    }
}