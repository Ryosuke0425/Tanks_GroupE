using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // シーン管理用
using WebSocketSharp;
using Complete;


public class LobbyManager : MonoBehaviour
{
    public Button readyButton;
    public List<Button> stampButtons;
    public Text statusText;

    private WebSocket ws;
    private bool isReady = false;

    // チャット関連のUI
    public InputField chatInputField;  // チャット入力欄
    public Button sendChatButton;      // チャット送信ボタン

    // PVPサーバーURLを保持
    private string pvpServerUrl;

    // イベント: 他スクリプトで受信したメッセージを処理するために利用
    public Action<string, string> OnChatMessageReceived;

    void Start()
    {
        try
        {
            // サーバーとのWebSocket接続
            ws = new WebSocket("ws://localhost:8000/"); // サーバーのURL
            ws.OnMessage += OnMessageReceived;
            ws.OnError += OnError;
            ws.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket connection error: {e.Message}");
        }

        // READYボタンの設定
        readyButton.onClick.AddListener(() =>
        {
            if (!isReady)
            {
                SendReadyStatus();
                isReady = true;
                UpdateStatusText("Waiting for opponent...");
            }
        });

        // スタンプボタンの設定
        for (int i = 0; i < stampButtons.Count; i++)
        {
            int stampId = i; // ローカルスコープで固定
            stampButtons[i].onClick.AddListener(() => SendStamp(stampId));
        }

        // チャット送信ボタンの設定
        sendChatButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(chatInputField.text))
            {
                SendChatMessage(chatInputField.text);
                chatInputField.text = ""; // 入力欄をクリア
            }
        });
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        try
        {
            Debug.Log("Received message: " + e.Data);

            // 空メッセージのチェック
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                Debug.LogWarning("Received an empty message.");
                return;
            }

            // JSON解析
            var data = JsonUtility.FromJson<ServerMessage>(e.Data);

            // typeに基づく処理
            switch (data.type)
            {
                case "status_update":
                    HandleStatusUpdate(data.status);
                    break;

                case "game_start":
                    HandleGameStart(data.pvp_server_url);
                    break;

                case "chat":
                    ProcessChatMessage(data.sender, data.text);
                    break;
                case "send_message": // サーバーが送信するチャットタイプにも対応
                    OnChatMessageReceived?.Invoke(data.sender, data.text);
                    break;

                case "stamp":
                    DisplayOpponentStamp(data.stamp_id);
                    break;

                case "ready_notice":
                    Debug.Log($"Opponent is ready: {data.sender} ");
                    UpdateStatusText("Oppent is ready !!");
                    break;
                case "assign_player_number":
                    HandleAssignPlayerNumber(data.player_number);
                    break;

                default:
                    Debug.LogWarning($"Unknown message type: {data.type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing message: {ex.Message}\nMessage content: {e.Data}");
        }
    }

    private void HandleStatusUpdate(string status)
    {
        // statusフィールドに基づく処理
        switch (status)
        {
            case "matching":
                UpdateStatusText("Matching...");
                break;

            case "matched":
                UpdateStatusText("Matched! Waiting for opponent to ready up...");
                break;

            case "ready":
                UpdateStatusText("Opponent is ready!");
                break;

            default:
                Debug.LogWarning($"Unknown status: {status}");
                break;
        }
    }

    private void HandleGameStart(string serverUrl)
    {
        // PVPサーバーのURLを保持
        pvpServerUrl = serverUrl;

        // メインスレッドでシーンをロード
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log($"Connecting to PVP server: {serverUrl}");
            SceneManager.LoadScene("_Complete-Game");
        });
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error: {e.Message}");
    }

    private void UpdateStatusText(string message)
    {
        // メインスレッドでUI更新を安全に実行
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            statusText.text = message;
        });
    }

    private void SendReadyStatus()
    {
        var message = new ClientMessage
        {
            type = "status_update",
            status = "ready"
        };
            SendToServer(message);
    }


    private void SendStamp(int stampId)
    {
        var message = new ClientMessage
        {
            type = "stamp",
            stamp_id = stampId
        };
        SendToServer(message);
    }


    public void SendChatMessage(string message)
    {
        var chatMessage = new ClientMessage
        {
            type = "send_message",
            sender = "You",
            text = message
        };
        SendToServer(chatMessage);
    }


    private void SendToServer(object message)
    {
        if (ws != null && ws.IsAlive)
        {
            var json = JsonUtility.ToJson(message);
            Debug.Log($"Sending message: {json}");
            ws.Send(json);
        }
        else
        {
            Debug.LogWarning("WebSocket connection is not alive. Message not sent.");
        }
    }
    private void ProcessChatMessage(string sender, string text)
    {
        // メッセージ処理をメインスレッドに移譲
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            // ChatManagerにメッセージを送信して表示
            ChatManager chatManager = FindObjectOfType<ChatManager>();
            if (chatManager != null)
            {
                chatManager.AddChatMessage(sender, text);
            }
            else
            {
                Debug.LogWarning("ChatManager not found in the scene.");
            }
        });
    }
    private void HandleAssignPlayerNumber(int playerNumber)
    {
        Debug.Log($"Assigned Player Number: {playerNumber}");

        // メインスレッドで処理を行う
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            PlayerPrefs.SetInt("PlayerId", playerNumber);
            PlayerPrefs.Save();
            UpdateStatusText($"Your player number is: {playerNumber}");
        });
    }





    private void DisplayOpponentStamp(int stampId)
    {
        Debug.Log($"Opponent sent stamp: {stampId}");
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }

    [Serializable]
    public class ServerMessage
    {
        public string type;
        public string status;
        public string pvp_server_url; // PVPサーバーURL
        public string sender;        // チャット送信者
        public string text;          // チャット内容
        public int stamp_id;         // スタンプID
        public int player_number;
    }

    [Serializable]
    public class ClientMessage
    {
        public string type;
        public string status;
        public string sender;
        public string text;
        public int stamp_id;
        public int player_number;
    }

}
