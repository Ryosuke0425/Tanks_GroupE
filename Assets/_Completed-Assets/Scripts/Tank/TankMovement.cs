using UnityEngine;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
        public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        public Transform turret;                     // 砲塔のTransform
        public float turretRotationSpeed = 100f;    // 砲塔の回転速度

        private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
        private string m_TurnAxisName;              // The name of the input axis for turning.
        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_MovementInputValue;         // The current value of the movement input.
        private float m_TurnInputValue;             // The current value of the turn input.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particles systems used by the Tanks

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            // 子オブジェクトから砲塔を探してアサイン
            if (turret == null)
            {
                Transform turretTransform = transform.Find("TankRenderers/TankTurret"); // 子オブジェクトのパスを指定
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
            m_Rigidbody.isKinematic = false;
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;

            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;

            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }

        private void Start()
        {
            m_MovementAxisName = "Vertical" + m_PlayerNumber;
            m_TurnAxisName = "Horizontal" + m_PlayerNumber;

            m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void Update()
        {
            // Store the value of both input axes.
            m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
            m_TurnInputValue = Input.GetAxis(m_TurnAxisName);

            // 砲塔の回転処理
            if (m_PlayerNumber == 1)
            {
                if (gameObject.GetComponent<TankHealth>().IsInvincible)
                {
                    return;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    turret.Rotate(Vector3.up, -turretRotationSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    turret.Rotate(Vector3.up, turretRotationSpeed * Time.deltaTime);
                }
            }
            else if (m_PlayerNumber == 2) // 2プレイヤー目のタンク
            {
                if (Input.GetKey(KeyCode.Comma)) // ','キー
                {
                    turret.Rotate(Vector3.up, -turretRotationSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.Period)) // '.'キー
                {
                    turret.Rotate(Vector3.up, turretRotationSpeed * Time.deltaTime);
                }
            }


            EngineAudio();
        }

        private void EngineAudio()
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        private void FixedUpdate()
        {
            Move();
            Turn();
        }

        private void Move()
        {
            if (gameObject.GetComponent<TankHealth>().IsInvincible)
            {
                return;
            }
            Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn()
        {
            if (gameObject.GetComponent<TankHealth>().IsInvincible)
            {
                return;
            }
            float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }
    }
}
