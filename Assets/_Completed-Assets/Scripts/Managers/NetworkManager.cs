using System;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    private WebSocket ws;
    private string matchId;
    public int playerId;

    // プレイヤー番号とタンクのGameObjectをマッピング
    private Dictionary<int, GameObject> playerTanks = new Dictionary<int, GameObject>();

    // タンク登録順
    private int tankCount = 0;

    private bool isReconnecting = false;

    // スレッドセーフなキュー
    private ConcurrentQueue<Action> messageQueue = new ConcurrentQueue<Action>();

    // タンク生成関連（必要なければコメントアウト可）
    [Header("タンクの設定")]
    public Transform[] spawnPoints;
    public GameObject tankPrefab;

    // タンク検出用タイマー
    private float tankCheckInterval = 1f;
    private float tankCheckTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            string savedMatchId = PlayerPrefs.GetString("MatchId", "");
            if (!string.IsNullOrEmpty(savedMatchId))
            {
                InitializeConnection(savedMatchId);
            }
            else
            {
                Debug.LogError("MatchIdがPlayerPrefsに設定されていません。");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
    }

    public void RegisterTank(GameObject tank)
    {
        if (tank == null)
        {
            Debug.LogWarning("RegisterTankにnullのタンクが渡されました。");
            return;
        }

        if (playerTanks.ContainsValue(tank))
        {
            Debug.LogWarning("このタンクは既に登録されています。");
            return;
        }

        tankCount++;
        int assignedPlayerNumber = tankCount; // 1,2 と割り当てる想定

        if (assignedPlayerNumber > 2)
        {
            Debug.LogWarning($"プレイヤー番号{assignedPlayerNumber}はサポートされていません。");
            return;
        }

        playerTanks[assignedPlayerNumber] = tank;
        Debug.Log($"プレイヤー{assignedPlayerNumber}のタンクが登録されました。");

        var tankManager = tank.GetComponent<Complete.TankMovement>();
        if (tankManager != null)
        {
            // ローカルプレイヤーかどうか判定（playerIdとassignedPlayerNumberの一致で判断）
            bool isLocal = (assignedPlayerNumber == playerId);
            tankManager.SetControlledByLocalPlayer(isLocal);
        }
        else
        {
            Debug.LogWarning("TankMovementコンポーネントがタンクにアタッチされていません。");
        }
    }

    public GameObject GetTank(int playerNumber)
    {
        if (playerTanks.ContainsKey(playerNumber))
        {
            return playerTanks[playerNumber];
        }
        else
        {
            return null;
        }
    }

    public void InitializeConnection(string matchId)
    {
        this.matchId = matchId;
        playerId = PlayerPrefs.GetInt("PlayerId", -1);

        Debug.Log($"PlayerPrefsから取得したPlayer ID: {playerId}");
        Debug.Log($"InitializeConnectionが呼び出されました。matchId: {matchId}");

        if (playerId == -1)
        {
            Debug.LogError("PlayerIdが設定されていません。ロビーシーンで正しく設定してください。");
            return;
        }

        ConnectWebSocket();
    }

    private void ConnectWebSocket()
    {
        string url = $"ws://localhost:8080/match/{matchId}/{playerId}";
        Debug.Log($"WebSocket URLに接続します: {url}");

        try
        {
            ws = new WebSocket(url);

            ws.OnOpen += (sender, e) =>
            {
                Debug.Log("WebSocket接続が確立されました。");
                isReconnecting = false;
            };

            ws.OnMessage += (sender, e) =>
            {
                Debug.Log($"受信メッセージ: {e.Data}");
                HandleIncomingMessage(e.Data);
            };

            ws.OnClose += (sender, e) =>
            {
                Debug.LogWarning("WebSocket接続が閉じられました。");
                if (!isReconnecting)
                {
                    isReconnecting = true;
                    Invoke(nameof(Reconnect), 5f);
                }
            };

            ws.OnError += (sender, e) => Debug.LogError($"WebSocketエラー: {e.Message}");

            ws.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket接続中に例外が発生しました: {ex.Message}");
        }
    }

    public void SendTankUpdate(int playerNumber, Vector3 position, Quaternion rotation, Quaternion turretRotation)
    {
        var message = new ClientMessage<PositionData>
        {
            type = "position_update",
            player = playerNumber,
            data = new PositionData
            {
                position = new float[] { position.x, position.y, position.z },
                rotation = new float[] { rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z },
                turretRotation = new float[] { turretRotation.eulerAngles.x, turretRotation.eulerAngles.y, turretRotation.eulerAngles.z }
            },
            match_id = matchId
        };

        SendToServer(message);
    }

    public void SendFireEvent(int playerNumber, Vector3 position, Quaternion turretRotation, float launchForce)
    {
        var message = new ClientMessage<FireData>
        {
            type = "fire",
            player = playerNumber,
            data = new FireData
            {
                position = new float[] { position.x, position.y, position.z },
                turretRotation = new float[] { turretRotation.eulerAngles.x, turretRotation.eulerAngles.y, turretRotation.eulerAngles.z },
                launchForce = launchForce
            },
            match_id = matchId
        };

        SendToServer(message);
    }

    private void SendToServer(object message)
    {
        if (ws != null && ws.IsAlive)
        {
            string json = JsonConvert.SerializeObject(message);
            // 必要ならログを減らせる
            // Debug.Log($"送信メッセージ: {json}");
            ws.Send(json);
        }
        else
        {
            Debug.LogWarning("WebSocket接続がありません。メッセージは送信されません。");
        }
    }

    private void HandleIncomingMessage(string jsonData)
{
    try
    {
        JObject json = JObject.Parse(jsonData);
        string type = json["type"]?.ToString();

        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning("typeフィールドが存在しないメッセージを受信しました。");
            return;
        }

        switch (type)
        {
            case "position_update":
                var positionMessage = json.ToObject<ClientMessage<PositionData>>();
                messageQueue.Enqueue(() => UpdateTankMovement(positionMessage.player, positionMessage.data));
                break;
            case "fire":
                var fireMessage = json.ToObject<ClientMessage<FireData>>();
                messageQueue.Enqueue(() => UpdateTankFire(fireMessage.player, fireMessage.data));
                break;
            case "mine":
                var mineMessage = json.ToObject<ClientMessage<MineStateData>>();
                messageQueue.Enqueue(() => UpdateMineEvent(mineMessage.player, mineMessage.data));
                break;
            case "round_end":
                // サーバーからのround_endを受信したらGameManagerに知らせる
                messageQueue.Enqueue(() => NotifyGameManagerRoundEnd());
                break;
            default:
                Debug.Log($"未知のメッセージタイプ: {type}");
                break;
        }
    }
    catch (JsonException jsonEx)
    {
        Debug.LogError($"JSONパースエラー: {jsonEx.Message}");
        Debug.LogError($"スタックトレース: {jsonEx.StackTrace}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"予期せぬエラー: {ex.Message}");
        Debug.LogError($"スタックトレース: {ex.StackTrace}");
    }
}

private void NotifyGameManagerRoundEnd()
{
    // GameManagerのRoundEndingを呼び出す
    Complete.GameManager gm = FindObjectOfType<Complete.GameManager>();
    if (gm != null)
    {
        // サーバーからround_end通知を受けたらRoundEndingコルーチンを開始
        gm.StartCoroutine("RoundEnding");
    }
    else
    {
        Debug.LogWarning("GameManagerがシーン内に見つかりませんでした。");
    }
}


    private void Update()
    {
        // メッセージキューの処理
        while (messageQueue.TryDequeue(out Action action))
        {
            action.Invoke();
        }

        // タンク割り当てチェック
        tankCheckTimer += Time.deltaTime;
        if (tankCheckTimer >= tankCheckInterval)
        {
            tankCheckTimer = 0f;
            AssignTanks();
        }
    }

    private void AssignTanks()
    {
        for (int i = 1; i <= 2; i++)
        {
            if (!playerTanks.ContainsKey(i))
            {
                Debug.Log($"[NetworkManager] プレイヤー{i}にタンクが割り当てられていません。");
                GameObject[] tanks = GameObject.FindGameObjectsWithTag("CompleteTank");
                foreach (GameObject tank in tanks)
                {
                    if (!playerTanks.ContainsValue(tank))
                    {
                        Debug.Log($"[NetworkManager] 未登録のタンクを発見: {tank.name}。プレイヤー{i}に割り当てます。");
                        RegisterTank(tank);
                        break;
                    }
                }
            }
        }
    }

    private void UpdateTankMovement(int playerNumber, PositionData data)
    {
        if (playerNumber == playerId)
        {
            // 自分自身の位置更新は無視
            return;
        }

        GameObject tank = GetTank(playerNumber);
        if (tank != null)
        {
            var tankMovement = tank.GetComponent<Complete.TankMovement>();
            if (tankMovement != null)
            {
                Vector3 position = new Vector3(data.position[0], data.position[1], data.position[2]);
                Quaternion rotation = Quaternion.Euler(data.rotation[0], data.rotation[1], data.rotation[2]);
                Quaternion turretRotation = Quaternion.Euler(data.turretRotation[0], data.turretRotation[1], data.turretRotation[2]);

                tankMovement.UpdateFromNetwork(position, rotation, turretRotation);
            }
        }
    }
    public void SendMineEvent(int playerNumber, Vector3 position, Quaternion rotation)
    {
        var message = new ClientMessage<MineStateData>
        {
            type = "mine",
            player = playerNumber,
            data = new MineStateData
            {
                position = new float[]{position.x, position.y, position.z},
                rotation = new float[]{rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z}
            },
            match_id = matchId
        };

        SendToServer(message);
    }




    private void UpdateTankFire(int playerNumber, FireData data)
    {
        if (playerNumber == playerId)
        {
            // 自分自身の発射イベントは無視
            return;
        }

        GameObject tank = GetTank(playerNumber);
        if (tank != null)
        {
            var tankShooting = tank.GetComponent<Complete.TankShooting>();
            if (tankShooting != null)
            {
                Vector3 position = new Vector3(data.position[0], data.position[1], data.position[2]);
                Quaternion turretRotation = Quaternion.Euler(data.turretRotation[0], data.turretRotation[1], data.turretRotation[2]);
                float launchForce = data.launchForce;
                // FireFromNetworkなどの受信用メソッドが必要
                tankShooting.ReceiveFireEvent(position, turretRotation, launchForce);
            }
        }
    }
    private void UpdateMineEvent(int playerNumber, MineStateData data)
{
    Debug.Log($"[NetworkManager] プレイヤー{playerNumber}の地雷イベントを更新します。");

    GameObject tank = GetTank(playerNumber);
    if (tank != null)
    {
        var tankShooting = tank.GetComponent<Complete.TankShooting>();
        if (tankShooting != null && playerNumber != playerId) // ローカルプレイヤーのタンクは無視
        {
            Vector3 position = new Vector3(data.position[0], data.position[1], data.position[2]);
            Quaternion rotation = Quaternion.Euler(data.rotation[0], data.rotation[1], data.rotation[2]);

            // ネットワークから受信した地雷の状態をタンクに適用
            tankShooting.ReceiveMineState(position, rotation);
        }
    }
    else
    {
        Debug.LogWarning($"[NetworkManager] プレイヤー{playerNumber}のタンクが見つかりません。");
    }
}


    private void HandleStatusUpdate(string status, int playerNumber, string matchId)
    {
        Debug.Log($"[HandleStatusUpdate] ステータス: {status}, プレイヤー: {playerNumber}, マッチID: {matchId}");
    }

    private void Reconnect()
    {
        Debug.Log("WebSocket再接続を試みます...");
        ConnectWebSocket();
    }

    [Serializable]
    public class ClientMessage<T>
    {
        public string type;
        public int player;
        public T data;
        public string match_id;
    }

    [Serializable]
    public class PositionData
    {
        public float[] position;
        public float[] rotation;
        public float[] turretRotation;
    }

    [Serializable]
    public class FireData
    {
        public float[] position;
        public float[] turretRotation;
        public float launchForce;
    }

    [Serializable]
    public class StatusUpdateMessage
    {
        public string type;
        public string status;
        public int player_number;
        public string match_id;
    }
    [Serializable]
    public class MineStateData
    { 
        public float[] position;
        public float[] rotation;
    }
}