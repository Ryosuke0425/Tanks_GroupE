using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;
using System.Collections.Generic;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 5f;
        public CameraControl m_CameraControl;
        public Minimap m_Minimap;
        public Text m_MessageText;
        public Text[] numWin;
        public GameObject m_TankPrefab;
        public TankManager[] m_Tanks;
        public ArmorPlusManager armorPlusManager;

        private int m_RoundNumber;
        private WaitForSeconds m_StartWait;
        private WaitForSeconds m_EndWait;
        private TankManager m_RoundWinner;
        private TankManager m_GameWinner;

        private WebSocket ws;
        [SerializeField] private TextMeshProUGUI My_Info;
        [SerializeField] private TextMeshProUGUI user_rank;
        [SerializeField] private TextMeshProUGUI user_id;
        [SerializeField] private TextMeshProUGUI username;
        [SerializeField] private TextMeshProUGUI wins;
        [SerializeField] private TextMeshProUGUI losses;
        [SerializeField] private TextMeshProUGUI win_rate;
        private string My_user;
        private string user_Rank;
        private string user_Id;
        private string userName;
        private string Wins;
        private string Losses;
        private string win_Rate;
        private string my_pre_Rank;
        private string my_current_Rank;
        private float floatArmorPlus = 1f;

        private bool show_text = false;
        private string currentUsername;
        private string currentUserID;

        public enum GameState
        {
            RoundStarting,
            RoundPlaying,
            RoundEnding
        }
        public GameState Current_GameState;
        public event Action<GameState> OnGameStateChanged;

        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject userwinDialog;

        private void SetGameState(GameState newGameState)
        {
            if (Current_GameState != newGameState)
            {
                Current_GameState = newGameState;
                OnGameStateChanged?.Invoke(Current_GameState);
            }
        }

        private void Start()
        {
            ws = new WebSocket("ws://localhost:8765");
            ws.OnMessage += OnMessageReceived;
            ws.OnError += OnError;
            ws.Connect();

            currentUserID = PlayerPrefs.GetString("UserID");
            currentUsername = PlayerPrefs.GetString("UserName");

            userwinDialog.SetActive(false);
            closeButton.onClick.AddListener(CloseWinDialog);

            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            GameObject armorPlusManagerObject = GameObject.Find("ArmorPlusManager");
            if (armorPlusManagerObject != null)
            {
                armorPlusManager = armorPlusManagerObject.GetComponent<ArmorPlusManager>();
            }

            SpawnAllTanks();
            SetCameraTarget();

            StartCoroutine(GameLoop());
        }

        private void CloseWinDialog()
        {
            userwinDialog.SetActive(false);
            SceneManager.LoadScene(SceneNames.TitleScene);
        }

        public void Update_Win()
        {
            var createUserData = new CreateUserData { type = "update_winer", username = currentUsername, user_id = currentUserID };
            string JsonMessage = JsonUtility.ToJson(createUserData);
            ws.Send(JsonMessage);
        }

        public void Update_Lose()
        {
            var createUserData = new CreateUserData { type = "update_loser", username = currentUsername, user_id = currentUserID };
            string JsonMessage = JsonUtility.ToJson(createUserData);
            ws.Send(JsonMessage);
        }

        public void Show_winers()
        {
            var createUserData = new CreateUserData { type = "show_winers", username = currentUsername, user_id = currentUserID };
            string JsonMessage = JsonUtility.ToJson(createUserData);
            ws.Send(JsonMessage);
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Debug.Log("Received message from server: " + e.Data);
            var json = JsonUtility.FromJson<ServerMessage>(e.Data);
            if (json != null)
            {
                if (json.type == "round_end")
                {
                    // サーバーからround_endを受け取ったらRoundEndingへ
                    StartCoroutine(RoundEnding());
                }
                else if (json.type == "game_end")
                {
                    // ゲーム終了処理など
                }
            }
        }

        private void OnError(object sender, WebSocketSharp.ErrorEventArgs e)
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
                ws.Close();
            }
        }

        private void SpawnAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                Debug.Log($"Spawning Tank {i + 1}");

                if (m_TankPrefab == null)
                {
                    Debug.LogError("m_TankPrefab is null!");
                    return;
                }

                if (m_Tanks[i] == null)
                {
                    Debug.LogError($"m_Tanks[{i}] is null!");
                    return;
                }

                if (m_Tanks[i].m_SpawnPoint == null)
                {
                    Debug.LogError($"m_Tanks[{i}].m_SpawnPoint is null!");
                    return;
                }

                m_Tanks[i].m_Instance = Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation);

                if (m_Tanks[i].m_Instance != null)
                {
                    Debug.Log($"Tank {i + 1} spawned successfully at position: {m_Tanks[i].m_SpawnPoint.position}");
                }
                else
                {
                    Debug.LogError($"Failed to spawn Tank {i + 1}");
                }

                m_Tanks[i].m_PlayerNumber = i + 1;
                m_Tanks[i].Setup();
            }
        }

        private void SetCameraTarget()
        {
            if (NetworkManager.Instance != null)
            {
                int localPlayerNumber = NetworkManager.Instance.playerId;

                for (int i = 0; i < m_Tanks.Length; i++)
                {
                    if (m_Tanks[i].m_PlayerNumber == localPlayerNumber && m_Tanks[i].m_Instance != null)
                    {
                        Transform target = m_Tanks[i].m_Instance.transform;
                        m_CameraControl.m_Target = target;
                        m_Minimap.m_Target = target;
                        Debug.Log($"SetCameraTarget: プレイヤー{localPlayerNumber}のタンクをカメラのターゲットに設定しました。");
                        return;
                    }
                }
            }

            Debug.LogWarning("SetCameraTarget: ローカルプレイヤーのタンクが見つかりません。");
        }

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            if (m_GameWinner != null)
            {
                userwinDialog.SetActive(true);
                Show_winers();
            }
            else
            {
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator RoundStarting()
        {
            SetGameState(GameState.RoundStarting);
            ResetAllTanks();
            DisableTankControl();

            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            // ローカルプレイヤーID取得
            int localPlayerNumber = -1;
            if (NetworkManager.Instance != null)
            {
                localPlayerNumber = NetworkManager.Instance.playerId;
            }
            else
            {
                Debug.LogWarning("NetworkManager.Instanceがnullです。ローカルプレイヤーIDが取得できません。");
            }

            TankManager localPlayerTank = null;
            TankManager enemyPlayerTank = null;

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_PlayerNumber == localPlayerNumber)
                {
                    localPlayerTank = m_Tanks[i];
                }
                else
                {
                    enemyPlayerTank = m_Tanks[i];
                }
            }

            if (localPlayerTank == null || localPlayerTank.m_Instance == null)
            {
                Debug.LogError("ローカルプレイヤーのタンクが見つからないか、m_Instanceがnullです。");
                yield return m_StartWait;
                yield break;
            }

            // ローカルプレイヤーのタンクのHP表示更新
            TankHealth localTankHealth = localPlayerTank.m_Instance.GetComponent<TankHealth>();
            if (localTankHealth == null)
            {
                Debug.LogError("ローカルプレイヤーのタンクにTankHealthがアタッチされていません。");
            }
            else
            {
                int armorHP = (int)(floatArmorPlus * 100);
                if (localTankHealth.m_CurrentHealthDisplay != null)
                {
                    localTankHealth.m_CurrentHealthDisplay.text = "HP:" + armorHP.ToString();
                }
                else
                {
                    Debug.LogWarning("localTankHealth.m_CurrentHealthDisplayがnullです。");
                }

                if (localTankHealth.hpSlider != null && localTankHealth.hpSlider.Length >= 2)
                {
                    localTankHealth.hpSlider[0].value = floatArmorPlus - 1.0f;
                    localTankHealth.hpSlider[1].value = 1.0f;
                }
                else
                {
                    Debug.LogWarning("localTankHealth.hpSliderがnull、または要素数が足りません。");
                }
            }

            // 敵プレイヤー側のHP表示は常に100HPにリセット（任意）
            if (enemyPlayerTank != null && enemyPlayerTank.m_Instance != null)
            {
                TankHealth enemyTankHealth = enemyPlayerTank.m_Instance.GetComponent<TankHealth>();
                if (enemyTankHealth != null && enemyTankHealth.m_CurrentHealthDisplay != null)
                {
                    enemyTankHealth.m_CurrentHealthDisplay.text = "HP:100";
                    if (enemyTankHealth.hpSlider != null && enemyTankHealth.hpSlider.Length >= 2)
                    {
                        enemyTankHealth.hpSlider[1].value = 1.0f;
                    }
                }
            }

            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            SetGameState(GameState.RoundPlaying);
            EnableTankControl();

            m_MessageText.text = string.Empty;

            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            SetGameState(GameState.RoundEnding);
            DisableTankControl();

            m_RoundWinner = null;
            m_RoundWinner = GetRoundWinner();

            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            numWin[0].text = "Win:" + m_Tanks[0].m_Wins.ToString();
            numWin[1].text = "Win:" + m_Tanks[1].m_Wins.ToString();

            m_GameWinner = GetGameWinner();
            string message = EndMessage();
            m_MessageText.text = message;

            yield return m_EndWait;

            if (m_GameWinner != null && armorPlusManager != null)
            {
                armorPlusManager.CompleteUseArmorPlus();
            }

            if (m_GameWinner != null)
            {
                Debug.Log("OK:" + m_Tanks[0]);
                if (m_Tanks[0] == m_GameWinner)
                {
                    Update_Win();
                }
                else
                {
                    Update_Lose();
                }
            }
        }

        private bool OneTankLeft()
        {
            int numTanksLeft = 0;

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            return numTanksLeft <= 1;
        }

        private TankManager GetRoundWinner()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                    return m_Tanks[i];
            }
            return null;
        }

        private TankManager GetGameWinner()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                    return m_Tanks[i];
            }
            return null;
        }

        private string EndMessage()
        {
            string message = "DRAW!";

            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            message += "\n\n\n\n";

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }

        private void ResetAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].Reset();
            }
        }

        private void EnableTankControl()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].EnableControl();
            }
        }

        private void DisableTankControl()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].DisableControl();
            }
        }

        void Update()
        {
            if (show_text == true)
            {
                Debug.Log("OK show_text");
                user_rank.text = user_Rank;
                user_id.text = user_Id;
                username.text = userName;
                wins.text = Wins;
                losses.text = Losses;
                win_rate.text = win_Rate;

                if (my_current_Rank != "圏外")
                {
                    if (1 <= int.Parse(my_current_Rank) && int.Parse(my_current_Rank) <= 10 && my_pre_Rank == "圏外")
                    {
                        My_Info.text = $"<color=#00FF00>{My_user}</color>";
                    }
                    else if (int.Parse(my_current_Rank) < int.Parse(my_pre_Rank))
                    {
                        My_Info.text = $"<color=#00FF00><b>{My_user}</b></color>";
                    }
                    else
                    {
                        My_Info.text = My_user;
                    }
                }
                else
                {
                    My_Info.text = My_user;
                }
                show_text = false;
            }
        }

        public void NotifyRoundEnd()
        {
            var message = new { type = "round_end" };
            string json = JsonUtility.ToJson(message);
            Debug.Log("Sending round_end to server: " + json);
            ws.Send(json);
        }

        [Serializable]
        public class ServerMessage
        {
            public string type;
            public string status;
            public string opponent;
        }

        [Serializable]
        public class CreateUserData
        {
            public string type;
            public string username;
            public string user_id;
        }
    }
}
