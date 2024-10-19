using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VersusButton : MonoBehaviour
{
    [SerializeField]
    private Button Button;

    void OnClicked()
    {
        SceneManager.LoadScene(SceneNames.CompleteGameScene);
    }
    // Start is called before the first frame update
    void Start()
    {
        Button.onClick.AddListener(OnClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
