using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Versuscpu : MonoBehaviour
{
    [SerializeField]
    private Button Button;
    public StaminaManager staminaManager;
    public GameObject noStaminaPanel;

    void Onclicked()
    {
        bool success = staminaManager.UseStamina();
        if (!success)
        {
            noStaminaPanel.SetActive(true);
            return;
        }

        // スタミナがある場合、LobbySceneをロード
        SceneManager.LoadScene("_Complete-Game");
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
