using System;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              
        public Rigidbody m_Shell;                   
        public Transform m_FireTransform;           
        public Transform turret;                    
        public Slider m_AimSlider;                  
        public AudioSource m_ShootingAudio;         
        public AudioClip m_ChargingClip;            
        public AudioClip m_FireClip;                
        public float m_MinLaunchForce = 15f;        
        public float m_MaxLaunchForce = 30f;        
        public float m_MaxChargeTime = 0.75f;       

        private float m_CurrentLaunchForce;         
        private float m_ChargeSpeed;                
        private bool m_Fired;                       

        public int Bullets_start_hold = 10;         
        private int m_Bullets_hold;
        private int m_Bullets_max_hold = 50;
        private int m_Bullets_supply = 10;

        // イベントが2つ定義されていたようですが、typeが重複しているため、統一します
        // PVP対応: (int bullets, WeaponStockData mineStock)を引数にしたイベントを使用
        public event Action<int, WeaponStockData> OnShellStockChanged;

        private bool isIncreasing = true;

        // PVP対応: 初期値を(0,3,1)としている
        private WeaponStockData mineStock = new WeaponStockData(0, 3, 1);
        [SerializeField] private GameObject minePrefab;
        private string putMineButton;

        // ネットワーク関連
        private bool isLocalPlayer = false;

        private void Awake()
        {
            if (turret == null)
            {
                Transform turretTransform = transform.Find("TankRenderers/TankTurret");
                if (turretTransform != null)
                {
                    turret = turretTransform;
                }
                else
                {
                    Debug.LogError("Turret not found. Please assign the turret in the inspector or check the object name.");
                }
            }
        }

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            if (m_AimSlider != null)
            {
                m_AimSlider.value = m_MinLaunchForce;
            }
        }

        private void Start()
        {
            m_Bullets_hold = Bullets_start_hold;
            putMineButton = "PutMine" + m_PlayerNumber;
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

            if (NetworkManager.Instance != null)
            {
                isLocalPlayer = (m_PlayerNumber == NetworkManager.Instance.playerId);
            }
        }

        private void Update()
        {
            if (turret != null && m_AimSlider != null)
            {
                Vector3 aimSliderPosition = turret.position + turret.forward * 6.0f;
                m_AimSlider.transform.position = aimSliderPosition + turret.up * 0.5f;
                m_AimSlider.transform.rotation = turret.rotation * Quaternion.Euler(90, 0, 0);
            }

            if (isLocalPlayer)
            {
                if (m_Bullets_hold > 0)
                {
                    // Spaceキーでチャージ＆発射
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        m_Fired = false;
                        m_CurrentLaunchForce = m_MinLaunchForce;
                        m_ShootingAudio.clip = m_ChargingClip;
                        m_ShootingAudio.Play();
                        isIncreasing = true;
                    }
                    else if (Input.GetKey(KeyCode.Space) && !m_Fired)
                    {
                        if (isIncreasing)
                        {
                            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                            if (m_CurrentLaunchForce >= m_MaxLaunchForce)
                            {
                                m_CurrentLaunchForce = m_MaxLaunchForce;
                                isIncreasing = false;
                            }
                        }
                        else
                        {
                            m_CurrentLaunchForce -= m_ChargeSpeed * Time.deltaTime;
                            if (m_CurrentLaunchForce <= m_MinLaunchForce)
                            {
                                m_CurrentLaunchForce = m_MinLaunchForce;
                                isIncreasing = true;
                            }
                        }

                        if (m_AimSlider != null)
                            m_AimSlider.value = m_CurrentLaunchForce;
                    }
                    else if (Input.GetKeyUp(KeyCode.Space) && !m_Fired)
                    {
                        Fire();
                        if (m_AimSlider != null)
                            m_AimSlider.value = m_MinLaunchForce;
                    }
                }

                if (Input.GetButtonDown(putMineButton))
                {
                    PutMine();
                }
            }
        }

        public void Fire()
        {
            if (!isLocalPlayer) return;

            if (gameObject.GetComponent<TankHealth>().IsInvincible)
            {
                return;
            }

            m_Fired = true;
            m_Bullets_hold -= 1;
            OnShellStockChanged?.Invoke(m_Bullets_hold, mineStock);

            Vector3 firePosition = m_FireTransform.position + new Vector3(0, 0.85f, 0);
            if (m_Shell == null) Debug.LogError("Shell is null");
            if (m_FireTransform == null) Debug.LogError("FireTransform is null");
            if (turret == null) Debug.LogError("Turret is null");

            if (isLocalPlayer)
            {
                Rigidbody shellInstance = Instantiate(m_Shell, firePosition, turret.rotation);
                shellInstance.velocity = m_CurrentLaunchForce * turret.forward;

                m_ShootingAudio.clip = m_FireClip;
                m_ShootingAudio.Play();

                NetworkManager.Instance.SendFireEvent(m_PlayerNumber, firePosition, turret.rotation, m_CurrentLaunchForce);

                m_CurrentLaunchForce = m_MinLaunchForce;
            }
        }

        private void PutMine()
        {
            if (!isLocalPlayer) return;

            if (gameObject.GetComponent<TankHealth>().IsInvincible)
            {
                return;
            }

            if (mineStock.CurrentStock > 0)
            {
                mineStock.ConsumeStock(1);
                OnShellStockChanged?.Invoke(m_Bullets_hold, mineStock);
                Vector3 minePosition = gameObject.transform.position + turret.forward * 1.5f;
                Instantiate(minePrefab, minePosition, turret.rotation);

                // ネットワーク経由でmine設置イベントを送る場合は以下を呼ぶ
                NetworkManager.Instance.SendMineEvent(m_PlayerNumber, minePosition, turret.rotation);
            }
        }

        public void ReceiveFireEvent(Vector3 position, Quaternion turretRotation, float launchForce)
        {
            // リモートプレイヤーからの発射同期
            Rigidbody shellInstance = Instantiate(m_Shell, position, turretRotation);
            shellInstance.velocity = (turretRotation * Vector3.forward) * launchForce;
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }

        public void ReceiveMineState(Vector3 position, Quaternion rotation)
        {
            // リモートプレイヤーが設置した地雷生成
            Instantiate(minePrefab, position, rotation);
        }

        private void Add_Bullets()
        {
            if (!isLocalPlayer) return;

            int increment = Mathf.Min(m_Bullets_supply, m_Bullets_max_hold - m_Bullets_hold);
            if (increment > 0)
            {
                m_Bullets_hold += increment;
                OnShellStockChanged?.Invoke(m_Bullets_hold, mineStock);
                Debug.Log("Bullets increased! Current: " + m_Bullets_hold);
            }
            else
            {
                Debug.Log("Cannot add bullets. Already at max or no supply.");
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // カートリッジと衝突した場合に弾薬追加処理
            Debug.Log("OnCollisionEnter with " + collision.gameObject.name);
            if (collision.gameObject.CompareTag("ShellCartridge"))
            {
                Debug.Log("ShellCartridge collided!");
                Add_Bullets();
                Destroy(collision.gameObject);
            }

            // MineCartridgeにも対応するなら以下を追加
            if (collision.gameObject.CompareTag("MineCartridge"))
            {
                mineStock.AddStock(mineStock.StockInCartridge);
                OnShellStockChanged?.Invoke(m_Bullets_hold, mineStock);
                Destroy(collision.gameObject);
            }
        }
    }
}
