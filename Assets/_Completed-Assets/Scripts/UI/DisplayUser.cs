using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DisplayUser : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedUser;
    private string currentUserName;
    private string currentUserId;
    // Start is called before the first frame update
    void Start()
    {
    
    }
    private void DisplayUserNameID()
    {
        currentUserName = PlayerPrefs.GetString("UserName");
        currentUserId = PlayerPrefs.GetString("UserID");
        displayedUser.text = "Username:" + currentUserName + "\n" + "UserID:" + currentUserId;
    }
    // Update is called once per frame
    void Update()
    {
        DisplayUserNameID();
    }
}
