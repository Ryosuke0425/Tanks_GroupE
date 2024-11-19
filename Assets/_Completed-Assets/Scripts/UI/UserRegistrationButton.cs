using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using WebSocketSharp;
using UnityEditor.PackageManager;
public class UserRegistrationButton : MonoBehaviour
{
    [SerializeField] private Button userRegistrationButton;      // User Registrationボタン
    [SerializeField] private GameObject usernameDialog;          // ユーザー名変更ダイアログ（非アクティブで配置）
    [SerializeField] private TMP_InputField usernameInputField;  // ユーザー名の入力フィールド
    [SerializeField] private Button changeButton;                // 変更ボタン
    [SerializeField] private Button closeButton;                 // ダイアログを閉じるボタン
    [SerializeField] private TextMeshProUGUI displayedUsername;  // 画面に表示されるユーザー名

    private WebSocket ws;
    // Start is called before the first frame update
    private string currentUsername;
    private string currentUserID;
    void Start()
    {
        ws = new WebSocket("ws://localhost:8765");//変更予定、異なるデバイスからでもサーバに通信できるようにしたい
        ws.OnMessage += OnMessageReceived;//OnMessageReceivedメソッドをイベントハンドラとして登録、メッセージ受信時発火
        ws.OnError += OnError;
        ws.Connect();

        currentUsername = PlayerPrefs.GetString("UserName");

        userRegistrationButton.onClick.AddListener(OpenUsernameDialog);
        changeButton.onClick.AddListener(ChangeUsername);
        closeButton.onClick.AddListener(CloseUsernameDialog);

        // ダイアログを初期は非表示
        usernameDialog.SetActive(false);
    }
    private void modifyUser()
    {
        currentUserID = PlayerPrefs.GetString("UserID");
        var createUserData = new CreateUserData{ type = "modify_username", username = currentUsername, user_id = currentUserID};
        string JsonMessage = JsonUtility.ToJson(createUserData);
        ws.Send(JsonMessage);
    }
    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        var response = JsonUtility.FromJson<ResponseData>(e.Data);
        Debug.Log("In OnMessageReceived function display status:" + response.status);
        Debug.Log("In OnMessageReceived function display user_id:" + response.user_id);
    }
    private void OnError(object sender, WebSocketSharp.ErrorEventArgs e)//エラーハンドラー
    {
        Debug.LogError("WebSocket Error: " + e.Message);
        if (e.Exception != null)
        {
            Debug.LogError("Exception details: " + e.Exception.ToString());
        }
    }
    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close(); // Closeフレームを送信して適切に切断
        }
    }
    private void OpenUsernameDialog()
    {
        usernameDialog.SetActive(true);
        usernameInputField.text = currentUsername; // 現在のユーザー名をフィールドに表示
    }

    // ユーザー名を変更
    private void ChangeUsername()
    {
        string newUsername = usernameInputField.text;

        if (!string.IsNullOrEmpty(newUsername) && newUsername.Length >= 3 && newUsername.Length <= 15 && IsValidUsername(newUsername))
        {
            currentUsername = newUsername;
            PlayerPrefs.SetString("UserName", currentUsername);  // ローカルに保存
            PlayerPrefs.Save();
            displayedUsername.text = "Username: " + currentUsername;  // 各画面で新しいユーザー名を表示
            modifyUser();

            Debug.Log("ユーザー名が変更されました: " + currentUsername);
        }
        else
        {
            Debug.LogWarning("ユーザー名は3～15文字の範囲で入力してください。また記号は使わないでください");
            displayedUsername.text = "ユーザー名は3～15文字の範囲で入力してください。\nまた記号は使わないでください";
        }
    }
    private bool IsValidUsername(string input)
    {
        return Regex.IsMatch(input, @"^[a-zA-Z0-9ぁ-んァ-ン一-龯]{3,15}$");
    }
    // ユーザー名変更ダイアログを閉じる
    private void CloseUsernameDialog()
    {
        usernameDialog.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
