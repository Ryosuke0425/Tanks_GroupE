using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using WebSocketSharp;
using UnityEditor.PackageManager;
public class RankingDialog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Top_Info;//Top10の情報を表示
    [SerializeField] private TextMeshProUGUI My_Info;//自分の情報を表示
    private string currentUsername;
    private string currentUserID;

    private WebSocket ws;
    // Start is called before the first frame update
    void Start()
    {
        ws = new WebSocket("ws://localhost:8765");//変更予定、異なるデバイスからでもサーバに通信できるようにしたい
        ws.OnMessage += OnMessageReceived;//OnMessageReceivedメソッドをイベントハンドラとして登録、メッセージ受信時発火
        ws.OnError += OnError;
        ws.Connect();

        currentUserID = PlayerPrefs.GetString("UserID");
        Update_Win_Lose();//処理が非同期なのでここで大丈夫か？
    }
    private void Update_Win_Lose()
    { 
    
    }

    private void Print_Top10_MY()
    { 
        
    }
    private void OnMessageReceived(object sender, MessageEventArgs e)//リスト型等でデータが送られてくるので適宜修正
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
    // Update is called once per frame
    void Update()
    {
        
    }
}
