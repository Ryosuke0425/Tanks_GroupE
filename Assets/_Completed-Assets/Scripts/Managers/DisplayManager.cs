using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour
{
    public GameObject targetObject;
    public static DisplayManager Instance { get; private set; }

    private void Awake(){
        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene,LoadSceneMode mode){
        if(scene.name == "HomeScene"){
            if(targetObject != null){
                targetObject.SetActive(true);
            }
        }else{
            if(targetObject != null){
                targetObject.SetActive(false);
            }
        }
    }

}
