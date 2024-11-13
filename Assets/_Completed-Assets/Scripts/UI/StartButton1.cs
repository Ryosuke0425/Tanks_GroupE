using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartButton1 : MonoBehaviour
{
    [SerializeField]
    private Button Button;
    // Start is called before the first frame update
    void OnClicked()
    {
        SceneManager.LoadScene(SceneNames.HomeScene);
    }
    void Start()
    {
        Button.onClick.AddListener(OnClicked);
    }

    // Update is called once per frame
    void Update()
    {

    }
}