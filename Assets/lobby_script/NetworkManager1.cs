using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // シーン管理用
using WebSocketSharp;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    public Button readyButton;
    public Text statusText;
    public InputField chatInputField;  // チャット入力欄
    public Button sendChatButton;      // チャット送信ボタン

    // StampManagerへの参照を追加
    public StampManager stampManager;

    private WebSocket ws;
    private bool isReady = false;

    private string pvpServerUrl;
    private string matchId;

    // イベント: 他スクリプトで受信したメッセージを処理するために利用
    public Action<string, string> OnChatMessageReceived;

    void Start()
    {
        InitializeWebSocket();
        SetupReadyButton();
        SetupChatButton();

        // StampManagerがアサインされているか確認
        if (stampManager == null)
        {
            Debug.LogError("LobbyManager: StampManagerがアサインされていません。");
        }
    }

    private void InitializeWebSocket()
    {
        try
        {
            ws = new WebSocket("ws://localhost:8000/");
            ws.OnMessage += OnMessageReceived;
            ws.OnError += OnError;
            ws.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyManager: WebSocket connection error: {e.Message}");
        }
    }

    private void SetupReadyButton()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(() =>
            {
                if (!isReady)
                {
                    SendReadyStatus();
                    isReady = true;
                    UpdateStatusText("Waiting for opponent...");
                }
            });
        }
        else
        {
            Debug.LogError("LobbyManager: Ready Buttonがアサインされていません。");
        }
    }

    private void SetupChatButton()
    {
        if (sendChatButton != null && chatInputField != null)
        {
            sendChatButton.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(chatInputField.text))
                {
                    SendChatMessage(chatInputField.text);
                    chatInputField.text = "";
                }
            });
        }
        else
        {
            Debug.LogError("LobbyManager: Chat ButtonまたはChat Input Fieldがアサインされていません。");
        }
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        // メッセージの処理をメインスレッドに移動
        UnityMainThreadDispatcher.Enqueue(() => ProcessMessage(e.Data));
    }

    private void ProcessMessage(string message)
    {
        try
        {
            Debug.Log("LobbyManager: Received message: " + message);

            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("LobbyManager: Received an empty message.");
                return;
            }

            var data = JsonUtility.FromJson<ServerMessage>(message);

            switch (data.type)
            {
                case "status_update":
                    HandleStatusUpdate(data.status, data.player_number, data.match_id);
                    break;

                case "game_start":
                    HandleGameStart(data.pvp_server_url, data.match_id);
                    break;

                case "chat":
                    ProcessChatMessage(data.sender, data.text);
                    break;

                case "stamp":
                    DisplayOpponentStamp(data.stampId);
                    break;

                case "ready_notice":
                    Debug.Log($"LobbyManager: Opponent is ready: {data.sender}");
                    UpdateStatusText("Opponent is ready!");
                    break;

                default:
                    Debug.LogWarning($"LobbyManager: Unknown message type: {data.type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LobbyManager: Error processing message: {ex.Message}\nMessage content: {message}");
        }
    }

    private void HandleStatusUpdate(string status, int playerNumber, string receivedMatchId)
    {
        matchId = receivedMatchId;

        switch (status)
        {
            case "matching":
                UpdateStatusText("Matching...");
                break;

            case "matched":
                UpdateStatusText("Matched! Waiting for opponent to ready up...");
                AssignPlayerNumber(playerNumber);
                break;

            case "ready":
                UpdateStatusText("Opponent is ready!");
                break;

            default:
                Debug.LogWarning($"LobbyManager: Unknown status: {status}");
                break;
        }
    }

    private void HandleGameStart(string serverUrl, string receivedMatchId)
    {
        pvpServerUrl = serverUrl;
        matchId = receivedMatchId;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log($"LobbyManager: Saving PVP server URL: {pvpServerUrl} and matchId: {matchId}");

            PlayerPrefs.SetString("MatchId", matchId);
            PlayerPrefs.SetString("PVPServerUrl", pvpServerUrl);
            PlayerPrefs.Save();

            // WebSocket接続をクローズ
            if (ws != null && ws.IsAlive)
            {
                ws.Close();
                Debug.Log("LobbyManager: WebSocket connection closed before loading PVP scene.");
            }

            // PVPシーンに遷移
            Debug.Log($"LobbyManager: Connecting to PVP server: {pvpServerUrl} with matchId: {matchId}");
            SceneManager.LoadScene("_Complete-Game");
        });
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogError($"LobbyManager: WebSocket error: {e.Message}");
    }

    private void UpdateStatusText(string message)
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            statusText.text = message;
        });
    }

    /// <summary>
    /// スタンプメッセージを送信するメソッド
    /// </summary>
    /// <param name="stampId">送信するスタンプのID</param>
    public void SendStamp(int stampId)
    {
        if (ws != null && ws.IsAlive)
        {
            var stampMessage = new ClientMessage
            {
                type = "stamp",
                stampId = stampId,
                match_id = matchId
            };
            string json = JsonUtility.ToJson(stampMessage);
            ws.Send(json);
            Debug.Log($"LobbyManager: Sent stamp: {stampId}");
        }
        else
        {
            Debug.LogWarning("LobbyManager: WebSocket is not open. Stamp not sent.");
        }
    }

    private void SendReadyStatus()
    {
        var message = new ClientMessage
        {
            type = "status_update",
            status = "ready",
            match_id = matchId
        };
        SendToServer(message);
    }

    public void SendChatMessage(string message)
    {
        var chatMessage = new ClientMessage
        {
            type = "send_message",
            sender = "You",
            text = message,
            match_id = matchId
        };
        SendToServer(chatMessage);
    }

    private void SendToServer(object message)
    {
        if (ws != null && ws.IsAlive)
        {
            var json = JsonUtility.ToJson(message);
            Debug.Log($"LobbyManager: Sending message: {json}");
            ws.Send(json);
        }
        else
        {
            Debug.LogWarning("LobbyManager: WebSocket connection is not alive. Message not sent.");
        }
    }

    private void ProcessChatMessage(string sender, string text)
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            ChatManager chatManager = FindObjectOfType<ChatManager>();
            if (chatManager != null)
            {
                chatManager.AddChatMessage(sender, text);
            }
            else
            {
                Debug.LogWarning("LobbyManager: ChatManager not found in the scene.");
            }
        });
    }

    private void AssignPlayerNumber(int playerNumber)
    {
        Debug.Log($"LobbyManager: Assigned Player Number: {playerNumber}");

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            PlayerPrefs.SetInt("PlayerId", playerNumber);
            PlayerPrefs.Save();
            UpdateStatusText(playerNumber == 1 ? "Your Tank color is blue" : "Your Tank color is red");
        });
    }

    /// <summary>
    /// サーバーから受信したスタンプメッセージをStampManagerで表示する
    /// </summary>
    /// <param name="stampId">表示するスタンプのID</param>
    private void DisplayOpponentStamp(int stampId)
    {
        Debug.Log($"LobbyManager: Opponent sent stamp: {stampId}");
        if (stampManager != null)
        {
            stampManager.DisplayStamp(stampId);
        }
        else
        {
            Debug.LogError("LobbyManager: StampManagerがアサインされていません。");
        }
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

    [Serializable]
    public class ServerMessage
    {
        public string type;
        public string status;
        public string pvp_server_url;
        public string match_id;
        public string sender;
        public string text;
        public int stampId; // キャメルケースに変更
        public int player_number;
    }

    [Serializable]
    public class ClientMessage
    {
        public string type;
        public string status;
        public string sender;
        public string text;
        public int stampId; // キャメルケースに変更
        public string match_id;
    }
}
