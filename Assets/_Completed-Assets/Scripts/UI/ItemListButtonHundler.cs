using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemListButtonHundler : MonoBehaviour
{
    public static ItemListButtonHundler Instance {get;private set;}
    public GameObject itemListPanel;

    private void Awake(){
        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
    }

    public void OnItemListButtonClicked(){
        itemListPanel.SetActive(true);
    }

    public void OnCloseItemListButtonClicked(){
        itemListPanel.SetActive(false);
    }


}
