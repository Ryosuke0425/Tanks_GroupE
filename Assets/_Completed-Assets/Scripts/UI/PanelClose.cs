using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelClose : MonoBehaviour
{
    public GameObject panel;

    public void HidePanel(){
        if(panel != null){
            panel.SetActive(false);
        }
    }
}
