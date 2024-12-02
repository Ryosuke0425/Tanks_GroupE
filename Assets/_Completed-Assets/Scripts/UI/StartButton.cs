using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
//using static UnityEngine.Rendering.DebugUI;
[System.Serializable]
public class CreateUserData
{
    public string type;
    public string username;
    public string user_id;
}
[System.Serializable]
public class ResponseData
{
    public string status;
    public string user_id;
    public string error;
}

public class StartButton : MonoBehaviour
{
    private WebSocket ws;
    private string userId;
    private string user_Name = "NoName";
    [SerializeField] private TextMeshProUGUI user_ID_show;//左上のTextMeshProに表示したい内容を保持するため
    //[SerializeField] private TMP_InputField usernameInputField;//ユーザ名を入力を保持するための
    [SerializeField] private Button Button;
    private bool isMessageReceived = false;

    // Start is called before the first frame update
    void Start()
    {
        // 特定のキーに関連するデータを削除
        //PlayerPrefs.DeleteKey("UserID");
        //PlayerPrefs.DeleteKey("UserName");
        ws = new WebSocket("ws://localhost:8765");//変更予定、異なるデバイスからでもサーバに通信できるようにしたい
        ws.OnMessage += OnMessageReceived;//OnMessageReceivedメソッドをイベントハンドラとして登録、メッセージ受信時発火
        ws.OnError += OnError;
        ws.Connect();

        //PlayerPrefs.DeleteAll();//create userの挙動を確認するときに使用する
        //PlayerPrefs.Save();

        // 前回保存したユーザーIDをロード
        if (PlayerPrefs.HasKey("UserID"))//データをローカルに保存するためのクラス、UserIDがあるときは以下の処理
        {
            userId = PlayerPrefs.GetString("UserID");
            Debug.Log("already Login:" + userId);
            DisplayUserId(userId); // タイトル画面左上に表示
        }
        else
        {
            Debug.Log("first Login");
        }
        //this.usernameInputField = GameObject.Find("UsernameInputField").GetComponent<TMP_InputField>();//ユーザの入力を可能にする
        Button.onClick.AddListener(OnTapToStart);
        //usernameInputField.onEndEdit.AddListener(OnInputEnd);
    }

    public void OnTapToStart()//startbuttonが押されたとき、startbuttonに呼び出す処理を追加予定
    {
        if (string.IsNullOrEmpty(userId))
        {
            CreateNewUser();
        }
        else
        {
            LoginUser();
        }
    }

    private void CreateNewUser()//データを送る操作
    {
        //user_Name = "Player" + Random.Range(1, 10000);
        //string username = string.IsNullOrEmpty(usernameInputField.text) ? "Player" + Random.Range(1, 10000) : usernameInputField.text;//下のusernameに入れる
        var createUserData = new CreateUserData{ type = "create_user", username = user_Name };//typeを指定してあげる事でサーバでの処理の分岐に役立つ、CreateUserDataクラスの属性すべてが送信される
        Debug.Log("success create user:"+createUserData);
        string JsonMessage = JsonUtility.ToJson(createUserData);
        Debug.Log("success convert userdata to Json:" + JsonMessage);
        ws.Send(JsonMessage);//ws.SendJson(Utility.ToJson(Data))でデータを送る
    }

    private void LoginUser()//データを送る操作
    {
        user_Name = PlayerPrefs.GetString("UserName");
        userId = PlayerPrefs.GetString("UserID");
        var loginData = new CreateUserData{ type = "login", username = user_Name ,user_id = userId};
        Debug.Log("success login:" + loginData);
        ws.Send(JsonUtility.ToJson(loginData));//データを送る
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)//サーバーからメッセージ受信時、サーバーから受信したJSONデータを解析して、ユーザIDを取得、それをローカルストレージPlayerPrefsに保存、PlayerPrefsは組み込み機能、ゲーム再起動時にもデータは保持されているらしい
    {
        Debug.Log(e.Data);
        var response = JsonUtility.FromJson<ResponseData>(e.Data);//responseはstatusとuser_idを持つ
        Debug.Log("In OnMessageReceived function display status:" + response.status);
        Debug.Log("In OnMessageReceived function display user_id:" + response.user_id);
        if (response.status == "success" && !string.IsNullOrEmpty(response.user_id))
        {
            Debug.Log("sucess receive message from server");
            userId = response.user_id;//メインスレッドでローカルでのデータ保持を行うため一時保存
            isMessageReceived = true;//メインスレッドで処理を行うため
            Debug.Log("tempSave OK");
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

    private void DisplayUserId(string userId)//すでにログインしていた場合
    {
        if (user_ID_show != null)
        {
            // タイトル画面のUIにユーザーIDを表示する処理を追加
            Debug.Log("OK DisplayUserID: " + userId);
            this.user_ID_show.text = "UserID:" + userId;//userIdを画面上のTextMeshProに表示、うまくできてない
        }
        else
        {
            Debug.LogError("user_ID GameObject not found!");
        }
    }
    private IEnumerator TransitionToNextScene()
    {
        Debug.Log("OK preScenetrans");
        // 必要な処理をここに挿入 (例えば、待機時間やアニメーション)
        yield return new WaitForSeconds(1); // 例えば1秒待機
        SceneManager.LoadScene(SceneNames.HomeScene); // 次のシーンへ遷移
    }

    private void OnDestroy()//MonoBehaviourのメソッド、オブジェクト破壊時に呼び出される
    {
        if (ws != null)
        {
            ws.Close();
        }
    }
    /*//ユーザー名をtitle画面でも入力する時に使用
    private void OnInputEnd(string username)//ユーザ名に関する仕様を考慮
    {
        Debug.Log("In OnInputEnd function:" + username);
        if (3 < username.Length && username.Length < 15) //&& ContainsInvalidCharacters(username))
        {
            user_Name = username;
            Debug.Log("In OnInputEnd function username OK:" + user_Name);
        }
        else
        {
            Debug.Log("In OnInputEnd function username No:3文字以上15文字以下、特殊記号なしで入力してください");
        }
    }
    bool ContainsInvalidCharacters(string input)
    {
        // 記号を含む場合は true を返す（アルファベット、数字、ひらがな、カタカナ、漢字以外）
        return Regex.IsMatch(input, @"[^a-zA-Z0-9ぁ-んァ-ン一-龯]");
    }
    */
    
    void Update()
    {
        if (isMessageReceived) //以下はメインスレッドで処理しないといけないのでここ
        {
            PlayerPrefs.SetString("UserID", userId.ToString());//ローカルにデータを保存、スコア何かも保存できる、PlayerPrefs.DeleteKey("UserID");によりデータの削除もできる
            PlayerPrefs.SetString("UserName", user_Name);
            PlayerPrefs.Save();//うまくできてない,WebSocketメッセージを受信している際に使えない
            Debug.Log("display resister UserID:" + PlayerPrefs.GetString("UserID"));
            Debug.Log("display resister UserName:" + PlayerPrefs.GetString("UserName"));
            DisplayUserId(userId); // タイトル画面左上に表示
            StartCoroutine(TransitionToNextScene());
            isMessageReceived = false;//一度のみ呼び出せる
        }
    }
}
