using UnityEngine;
using System.Collections;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1; 
        public float m_Speed = 12f;
        public float m_TurnSpeed = 180f;
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;
        public Transform turret;
        public float turretRotationSpeed = 100f;

        private Rigidbody m_Rigidbody;
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private bool IsControlledByLocalPlayer = false;

        // 前回送信時の状態
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Quaternion lastTurretRotation;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            if (turret == null)
            {
                turret = transform.Find("TankRenderers/TankTurret");
                if (turret == null)
                {
                    Debug.LogError("Turret not found. Please assign the turret in the inspector or check the object name.");
                }
            }
        }

        private void Start()
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastTurretRotation = turret.rotation;

            // 初期化完了後にIsControlledByLocalPlayerはNetworkManagerから設定される想定
        }

        private void Update()
        {
            if (IsControlledByLocalPlayer)
            {
                HandleInput();
                HandleTurretRotation();
            }
        }

        private void FixedUpdate()
        {
            if (IsControlledByLocalPlayer)
            {
                Move();
                Turn();
                SyncTankMovement();
            }
        }

        public void SetControlledByLocalPlayer(bool isControlled)
        {
            IsControlledByLocalPlayer = isControlled;
        }

        private void HandleInput()
        {
            // ここではPlayer1用固定、実際はプレイヤー番号ごとに軸名を変えるべき
            m_MovementInputValue = Input.GetAxis("Vertical1");
            m_TurnInputValue = Input.GetAxis("Horizontal1");
        }

        private void Move()
        {
            Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn()
        {
            float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        private void HandleTurretRotation()
        {
            if (Input.GetKey(KeyCode.Q))
            {
                turret.Rotate(Vector3.up, -turretRotationSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.E))
            {
                turret.Rotate(Vector3.up, turretRotationSpeed * Time.deltaTime);
            }
        }

        private void SyncTankMovement()
        {
            // 位置か回転が前回送信時から変わっている場合のみサーバーに送信
            if (NetworkManager.Instance != null)
            {
                if (transform.position != lastPosition || transform.rotation != lastRotation || turret.rotation != lastTurretRotation)
                {
                    NetworkManager.Instance.SendTankUpdate(m_PlayerNumber, transform.position, transform.rotation, turret.rotation);

                    lastPosition = transform.position;
                    lastRotation = transform.rotation;
                    lastTurretRotation = turret.rotation;
                }
            }
        }

        public void UpdateFromNetwork(Vector3 position, Quaternion rotation, Quaternion turretRotation)
        {
            // リモートからの更新を適用（補間処理などは必要に応じて変更可能）
            transform.position = Vector3.Lerp(transform.position, position, 0.5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 0.5f);
            turret.rotation = Quaternion.Lerp(turret.rotation, turretRotation, 0.5f);
        }

        private void OnEnable()
        {
            StartCoroutine(WaitForNetworkInitialization());
        }

        private IEnumerator WaitForNetworkInitialization()
        {
            while (NetworkManager.Instance == null || NetworkManager.Instance.playerId == -1)
            {
                yield return null;
            }

            IsControlledByLocalPlayer = (NetworkManager.Instance.playerId == m_PlayerNumber);
            Debug.Log($"Tank {m_PlayerNumber} - IsControlledByLocalPlayer: {IsControlledByLocalPlayer}, playerId: {NetworkManager.Instance.playerId}");
        }
    }
}
