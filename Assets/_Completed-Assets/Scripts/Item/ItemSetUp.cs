using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;

[System.Serializable]
public class LoginBonusUserData
{
    public string type;
    public string user_id;
}
[System.Serializable]
public class SendItemData
{
    public string type;
    public string user_id;
    public int stamina;
    public int staminaUp;
    public int armorPlus;
}


[System.Serializable]
public class LoginBonusResponseData
{
    public string status;
    public string user_id;
    public int stamina; //スタミナ数
    public int armorPlus; //アーマー＋数
    public int staminaUp; //スタミナアップ数
    public bool first;//当日初回ログインか否か
    public int consecutive_days;//総ログイン日数(-1,0,1,2,3...)
    public string error;
}



public class ItemSetUp : MonoBehaviour
{
    public StaminaManager staminaManager;
    public ArmorPlusManager armorPlusManager;
    public StaminaUpManager staminaUpManager;
    public static ItemSetUp Instance { get; private set; }
    private WebSocket ws;
    private string userId;
    public int startStaminaNum = 0;
    public int startStaminaUpNum = 0;
    public int startArmorPlusNum = 0;
    private bool isMessageReceived = false;
    private static TaskCompletionSource<string> tcs;
    public GameObject loginBonusPanel;
    public Text loginBonusText;
    public Text loginBonusDay;
    private SynchronizationContext mainThreadContext;

    void Start(){
        // メインスレッドのコンテキストを保存
        ws = new WebSocket("ws://localhost:8765");//変更予定、異なるデバイスからでもサーバに通信できるようにしたい
        ws.OnMessage += OnMessageReceived;//OnMessageReceivedメソッドをイベントハンドラとして登録、メッセージ受信時発火
        ws.OnError += OnError;
        ws.Connect();
        LoginBonus();
    }

    private void LoginBonus(){
        //user_Name = PlayerPrefs.GetString("UserName");
        userId = PlayerPrefs.GetString("UserID");
        var loginBonusData = new LoginBonusUserData{ type = "login_bonus",user_id = userId};
        ws.Send(JsonUtility.ToJson(loginBonusData));//データを送る
    }


    private void OnMessageReceived(object sender, MessageEventArgs e)//サーバーからメッセージ受信時、サーバーから受信したJSONデータを解析して、ユーザIDを取得、それをローカルストレージPlayerPrefsに保存、PlayerPrefsは組み込み機能、ゲーム再起動時にもデータは保持されているらしい
    {
        //Debug.Log(e.Data);
        var response = JsonUtility.FromJson<LoginBonusResponseData>(e.Data);//responseはstatusとuser_idを持つ
        //Debug.Log("In OnMessageReceived function display status:" + response.status);
        //Debug.Log("In OnMessageReceived function display user_id:" + response.user_id);
        if (response.status == "success")
        {
            //Debug.Log("sucess receive message from server");
            userId = response.user_id;//メインスレッドでローカルでのデータ保持を行うため一時保存
            isMessageReceived = true;//メインスレッドで処理を行うため
            startArmorPlusNum = response.armorPlus;
            startStaminaNum = response.stamina;
            startStaminaUpNum = response.staminaUp;
            Debug.Log(response.consecutive_days);
            Debug.Log(response.first);
            Debug.Log("tempSave OK");
            // 別スレッドで非同期処理
            Task.Run(() =>
            {
            // 非同期処理が終わった後にUnity APIを使いたい場合
                mainThreadContext.Post(_ =>
                {
             // メインスレッドでの処理
            staminaManager.AddStamina(startStaminaNum);
            armorPlusManager.AddArmorPlus(startArmorPlusNum,false);
            staminaUpManager.AddStaminaUp(startStaminaUpNum);
            if(response.first){
                loginBonusPanel.SetActive(true);
                loginBonusDay.text = "Day " + ((response.consecutive_days+1) % 7 + 1).ToString() + " / 7";
                loginBonusText.text = "Get\nArmorPlus * " + ((response.consecutive_days+1) % 7 + 1).ToString() + "\nStaminaUp * " + ((response.consecutive_days+1) % 7 + 1).ToString();
            }
                }, null);
            });
        }
        else if(response.status == "success_item")
        {
            Debug.Log("success item");
        }
        else
        {
            Debug.LogError("Failed to create or login user: " + response.error);
        }
    }
    private void OnError(object sender, WebSocketSharp.ErrorEventArgs e)//エラーハンドラー
    {
        Debug.LogError("WebSocket Error: " + e.Message);
        if (e.Exception != null)
        {
            Debug.LogError("Exception details: " + e.Exception.ToString());
        }
    }






  public void SendItem(){
            //user_Name = PlayerPrefs.GetString("UserName");
        userId = PlayerPrefs.GetString("UserID");
        var sendItemData = new SendItemData{ type = "send_item",user_id = userId,
        stamina=staminaManager.stamina.quantity,staminaUp=staminaUpManager.staminaUp.quantity,armorPlus=armorPlusManager.armorPlus.quantity};
        //Debug.Log("success login:" + loginData);
        ws.Send(JsonUtility.ToJson(sendItemData));//データを送る
  }



    private void Awake(){
        mainThreadContext = SynchronizationContext.Current;

        if(Instance==null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
        }
    }
    
}