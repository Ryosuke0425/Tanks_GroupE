using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager;
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
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 5f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Minimap m_Minimap;                   //1-6,カメラの対象
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public Text[] numWin;                       //HUD:ラウンドの勝利数の表示
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control.
        public TankManager[] m_Tanks;               // A collection of managers for enabling and disabling different aspects of the tanks.
        public ArmorPlusManager armorPlusManager;   //アイテム課題:ゲーム開始時にプレイヤーのArmorPlus使用状況を確認する
        
        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

        private WebSocket ws;
        //[SerializeField] private TextMeshProUGUI Top_Info;//Top10の情報を表示//2_UserManagement
        [SerializeField] private TextMeshProUGUI My_Info;//自分の情報を表示
        [SerializeField] private TextMeshProUGUI user_rank;
        [SerializeField] private TextMeshProUGUI user_id;
        [SerializeField] private TextMeshProUGUI username;
        [SerializeField] private TextMeshProUGUI wins;
        [SerializeField] private TextMeshProUGUI losses;
        [SerializeField] private TextMeshProUGUI win_rate;
        //private string Top_users;//一時保存するための変数
        private string My_user;//一時保存するための変数
        private string user_Rank;
        private string user_Id;
        private string userName;
        private string Wins;
        private string Losses;
        private string win_Rate;
        private string my_pre_Rank;
        private string my_current_Rank;
        private float floatArmorPlus = 1f;         //アイテム課題:ArmorPlusの使用状況・1(未使用),2(使用中)

        //[SerializeField] private GameObject HPUIyour;//ゲーム修了時HUDを非表示にする
        //[SerializeField] private GameObject HPUIenemy;
        private bool show_text = false;//メインスレッドで処理するために使う変数
        private string currentUsername;
        private string currentUserID;

        [System.Serializable]
        public class UserInfo
        {
            public string user_rank;
            public string user_id;
            public string username;
            public int wins;
            public int losses;
            public float win_rate;
            public string pre_rank;
        }
        [System.Serializable]
        public class RankingResponse
        {
            public string status;
            public UserInfo[] top_users;
            public UserInfo player_info;
        }
        public enum GameState
        { 
            RoundStarting,
            RoundPlaying,
            RoundEnding
        }
        public GameState Current_GameState;
        public event Action<GameState> OnGameStateChanged;

        [SerializeField] private Button closeButton;//ダイアログのクローズボタン、パネルオブジェクトの下に配置
        [SerializeField] private GameObject userwinDialog;//ダイアログ、Panelオブジェクト
        private void SetGameState(GameState newGameState)
        {
            if (Current_GameState != newGameState)
            {
                Current_GameState = newGameState;
                OnGameStateChanged?.Invoke(Current_GameState);
            }
        }
        private void Start()
        {   //サーバー関連
            ws = new WebSocket("ws://localhost:8765");//変更予定、異なるデバイスからでもサーバに通信できるようにしたい
            ws.OnMessage += OnMessageReceived;//OnMessageReceivedメソッドをイベントハンドラとして登録、メッセージ受信時発火
            ws.OnError += OnError;
            ws.Connect();

            currentUserID = PlayerPrefs.GetString("UserID");
            currentUsername = PlayerPrefs.GetString("UserName");
            
            userwinDialog.SetActive(false);

            closeButton.onClick.AddListener(CloseWinDialog);
            // Create the delays so they only have to be made once.
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);

            //アイテム課題
            GameObject armorPlusManagerObject = GameObject.Find("ArmorPlusManager");
            if(armorPlusManagerObject != null){
                armorPlusManager = armorPlusManagerObject.GetComponent<ArmorPlusManager>(); //アイテム課題
            }
            SpawnAllTanks();
            //TPS課題,MiniMap課題 :スタート時にカメラの対象を決める
            SetCameraTarget();

            // Once the tanks have been created and the camera is using them as targets, start the game.
            StartCoroutine (GameLoop ());
        }
        private void CloseWinDialog()
        {
            userwinDialog.SetActive(false);
            SceneManager.LoadScene(SceneNames.HomeScene);
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
        private void OnMessageReceived(object sender, MessageEventArgs e)//リスト型等でデータが送られてくるので適宜修正、update_winer,update_loserについては返信は気にしなくてよい
        {
            var response = JsonUtility.FromJson<RankingResponse>(e.Data);
            Debug.Log("In OnMessageReceived function display status:" + response.status);
            Debug.Log("In OnMessageReceived function display top_users:" + response.top_users);
            Debug.Log("In OnMessageReceived function display player_info:" + response.player_info);
            if (response.top_users != null && response.player_info != null)
            {/*
                Top_users = "rank\tuser_ID\t\t\t\t\t\t\tuser_name\twins\tlosses\twin_rate\n";
                for (int i = 0; i < response.top_users.Length; i++)
                {
                    Top_users += response.top_users[i].user_rank.ToString() +"\t" + response.top_users[i].user_id.ToString() + "\t" + response.top_users[i].username.ToString() + "\t" + response.top_users[i].wins.ToString() + "\t" + response.top_users[i].losses.ToString() + "\t" + response.top_users[i].win_rate.ToString() + "\n";
                }
                */
                My_user = response.player_info.user_rank.ToString() + "\t"+ response.player_info.user_id.ToString()+"\t" + response.player_info.username.ToString()+"\t" + response.player_info.wins.ToString()+"\t" + response.player_info.losses.ToString()+"\t" + response.player_info.win_rate.ToString();
                my_pre_Rank = response.player_info.pre_rank.ToString();
                my_current_Rank = response.player_info.user_rank.ToString();

                user_Rank = "rank\n";//項目の初期化
                user_Id = "ID\n";
                userName = "name\n";
                Wins = "wins\n";
                Losses = "losses\n";
                win_Rate = "win_rate\n";
                for (int i = 0; i < response.top_users.Length; i++)//
                {
                    user_Rank += response.top_users[i].user_rank.ToString() + "\n";
                    user_Id += response.top_users[i].user_id.ToString() + "\n";
                    userName += response.top_users[i].username.ToString() + "\n";
                    Wins += response.top_users[i].wins.ToString() + "\n";
                    Losses += response.top_users[i].losses.ToString() + "\n";
                    win_Rate += response.top_users[i].win_rate.ToString() + "\n";
                }

                show_text = true;
            }
            //Debug.Log("In OnMessageReceived function display user_id:" + response.user_id);
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

        private void SpawnAllTanks()
        {
            // For all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... create them, set their player number and references needed for control.
                m_Tanks[i].m_Instance =
                    Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
                m_Tanks[i].m_PlayerNumber = i + 1;
                //HUD:戦車を出現させるときにどの戦車がどのHP表示と対応させるかを決める
                if (i == 0)
                {
                    m_Tanks[i].m_Instance.GetComponent<TankHealth>().m_CurrentHealthDisplay = GameObject.Find("YourHP").GetComponent<Text>();
                    m_Tanks[i].m_Instance.GetComponent<TankHealth>().hpSlider[0] = GameObject.Find("YourHP1").GetComponent<Slider>();
                    m_Tanks[i].m_Instance.GetComponent<TankHealth>().hpSlider[1] = GameObject.Find("YourHP2").GetComponent<Slider>();
                    //アイテム課題:ArmorPlus使用しているなら対象オブジェクトのHPを2倍にする
                    if(armorPlusManager != null){
                        if(armorPlusManager.armorPlus.used){
                            m_Tanks[i].m_Instance.GetComponent<TankHealth>().m_StartingHealth = 200.0f;
                            floatArmorPlus = 2f;
                        }
                    }
                }
                else
                {
                    m_Tanks[i].m_Instance.GetComponent<TankHealth>().m_CurrentHealthDisplay = GameObject.Find("EnemyHP").GetComponent<Text>();
                    m_Tanks[i].m_Instance.GetComponent<TankHealth>().hpSlider[0] = GameObject.Find("EnemyHP1").GetComponent<Slider>();
                    m_Tanks[i].m_Instance.GetComponent<TankHealth>().hpSlider[1] = GameObject.Find("EnemyHP2").GetComponent<Slider>();
                }
                m_Tanks[i].Setup();
            }
        }


        //TPS課題,MiniMap課題:カメラを一人のユーザーに向けるメソッド
        private void SetCameraTarget()
        {
            Transform target = m_Tanks[0].m_Instance.transform;         //TPS,ミニマップ:カメラ対象のユーザーを決める
            m_CameraControl.m_Target = target;                          //TPS:メインカメラをユーザーに向ける
            m_Minimap.m_Target = target;                                //ミニマップカメラをユーザーに向ける
        }
//TPS課題:不要になった
//        private void SetCameraTargets()
//        {
//            // Create a collection of transforms the same size as the number of tanks.
//            Transform[] targets = new Transform[m_Tanks.Length];
//
//            // For each of these transforms...
//            for (int i = 0; i < targets.Length; i++)
//            {
//                // ... set it to the appropriate tank transform.
//                targets[i] = m_Tanks[i].m_Instance.transform;
//            }
//
//            // These are the targets the camera should follow.
//            m_CameraControl.m_Targets = targets;
//        }


        // This is called from start and will run each phase of the game one after another.
        private IEnumerator GameLoop ()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundStarting ());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
            if (m_GameWinner != null)
            {
                // If there is a game winner, restart the level.
                userwinDialog.SetActive(true);
                //SceneManager.LoadScene (SceneNames.TitleScene);
                Show_winers();
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                StartCoroutine (GameLoop ());
            }
        }


        private IEnumerator RoundStarting ()
        {
            SetGameState(GameState.RoundStarting);
            // As soon as the round starts reset the tanks and make sure they can't move.
            ResetAllTanks ();
            DisableTankControl ();

            // Snap the camera's zoom and position to something appropriate for the reset tanks.
            //m_CameraControl.SetStartPositionAndSize (); //TPS課題:不要になった

            // Increment the round number and display text showing the players what round it is.
            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            //HUD:ラウンドが始まる時にHP表示を元に戻す
            m_Tanks[0].m_Instance.GetComponent<TankHealth>().m_CurrentHealthDisplay.text = "HP:" + ((int)floatArmorPlus * 100).ToString();
            m_Tanks[1].m_Instance.GetComponent<TankHealth>().m_CurrentHealthDisplay.text = "HP:100";
            m_Tanks[0].m_Instance.GetComponent<TankHealth>().hpSlider[0].value = floatArmorPlus - 1.0f;
            m_Tanks[0].m_Instance.GetComponent<TankHealth>().hpSlider[1].value = 1.0f;
            m_Tanks[1].m_Instance.GetComponent<TankHealth>().hpSlider[1].value = 1.0f;
            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_StartWait;
        }


        private IEnumerator RoundPlaying ()
        {
            SetGameState(GameState.RoundPlaying);
            // As soon as the round begins playing let the players control the tanks.
            EnableTankControl ();

            // Clear the text from the screen.
            m_MessageText.text = string.Empty;

            // While there is not one tank left...
            while (!OneTankLeft())
            {
                // ... return on the next frame.
                yield return null;
            }
        }


        private IEnumerator RoundEnding ()
        {
            SetGameState(GameState.RoundEnding);
            // Stop tanks from moving.
            DisableTankControl ();

            // Clear the winner from the previous round.
            m_RoundWinner = null;

            // See if there is a winner now the round is over.
            m_RoundWinner = GetRoundWinner ();

            // If there is a winner, increment their score.
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;
            //HUD:ラウンド終了時に画面上の勝利数更新
            numWin[0].text = "Win:" + m_Tanks[0].m_Wins.ToString();
            numWin[1].text = "Win:" + m_Tanks[1].m_Wins.ToString();
            // Now the winner's score has been incremented, see if someone has one the game.
            m_GameWinner = GetGameWinner ();

            // Get a message based on the scores and whether or not there is a game winner and display it.
            string message = EndMessage ();
            m_MessageText.text = message;
            //この部分にダイアログの処理を書く
            //userwinDialog.SetActive(true);
            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_EndWait;//終了時少し待つ

            //アイテム課題:勝者が確定したらarmorPlusの使用を終わらせる
            if(m_GameWinner != null){
                armorPlusManager.CompleteUseArmorPlus();
            }

            if (m_GameWinner != null)//勝者が確定したのち、処理を分ける、PvPの仕様が分からないのでとりあえず自身のみの更新を想定
            {
                //for (int i = 0; i < m_Tanks.Length; i++)
                //{
                    Debug.Log("OK:" + m_Tanks[0]);//自身が更新される状態にしているのでfalseになったときにupdateloseが呼ばれることになる
                    if (m_Tanks[0] == m_GameWinner)//tankは接続順に0,1..で管理しているので、
                    {
                        Update_Win();
                    }
                    else
                    {
                        Update_Lose();
                    }
                //}
            }
        }

        // This is used to check if there is one or fewer tanks remaining and thus the round should end.
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... and if they are active, increment the counter.
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }
        
        
        // This function is to find out if there is a winner of the round.
        // This function is called with the assumption that 1 or fewer tanks are currently active.
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... and if one of them is active, it is the winner so return it.
                if (m_Tanks[i].m_Instance.activeSelf)
                    return m_Tanks[i];
            }

            // If none of the tanks are active it is a draw so return null.
            return null;
        }


        // This function is to find out if there is a winner of the game.
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it.
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                    return m_Tanks[i];
            }
                // If no tanks have enough rounds to win, return null.
                return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw.
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message.
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that.
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties.
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
            if (show_text == true)//以下の処理はメインスレッド内で行う必要があるためここ
            {
                Debug.Log("OK show_text");
                //Top_Info.text = Top_users;
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
    }
}