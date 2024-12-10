using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public GameObject chatContent;  // ScrollViewのContent部分
    public GameObject chatItemPrefab;  // チャットメッセージのPrefab
    public ScrollRect chatScroll;  // ScrollViewそのもの
    public InputField inputField;  // ユーザーがメッセージを入力するInputField
    public Button sendButton;  // メッセージ送信ボタン

    private LobbyManager lobbyManager;  // サーバー通信を担当するLobbyManager

    void Start()
    {
        // LobbyManagerを取得
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager not found in the scene.");
            return;
        }

        // サーバーからのメッセージ受信イベントを登録
        lobbyManager.OnChatMessageReceived += AddChatMessage;

        // 送信ボタンがクリックされた時にメッセージを送信
        sendButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
                SendMessage(inputField.text);
                inputField.text = ""; // 入力欄をクリア
            }
        });
    }

    public void SendMessage(string message)
    {
        // 自分のチャットエリアにメッセージを追加
        AddChatMessage("You", message);

        // サーバーに送信
        if (lobbyManager != null)
        {
            lobbyManager.SendChatMessage(message);
        }
        else
        {
            Debug.LogWarning("LobbyManager is not available. Message not sent to the server.");
        }
    }

    public void AddChatMessage(string sender, string message)
    {
        // Prefabから新しいチャットアイテムを生成
        GameObject chatItem = Instantiate(chatItemPrefab, chatContent.transform);

        // チャットアイテム内のTextコンポーネントを取得して設定
        Text messageText = chatItem.GetComponentInChildren<Text>();
        messageText.text = $"{sender}: {message}";

        // 自動スクロールを更新
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // 1フレーム待機してレイアウトを更新
        yield return null;

        // ScrollRectを一番下に設定
        Canvas.ForceUpdateCanvases();
        chatScroll.verticalNormalizedPosition = 0f;
    }
}
