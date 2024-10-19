using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class VersusPlayerButton : MonoBehaviour
{
    [SerializeField]
    private Button Button;
    void Onclicked()
    {
        SceneManager.LoadScene(SceneNames.CompleteGameScene);
    }
    // Start is called before the first frame update
    void Start()
    {
        Button.onClick.AddListener(Onclicked);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}