using UnityEngine;

namespace Complete
{
    public class CameraControl : MonoBehaviour
    {
        public float m_DampTime = 0.2f;                 // Approximate time for the camera to refocus.
        public float m_ScreenEdgeBuffer = 4f;           // Space between the top/bottom most target and the screen edge.
        public float m_MinSize = 6.5f;                  // The smallest orthographic size the camera can be.
        //[HideInInspector] public Transform[] m_Targets; // All the targets the camera needs to encompass. TPS課題:不要になった
        [HideInInspector] public Transform m_Target;    // TPS課題:カメラのターゲットとなるプレイヤー

        private Camera m_Camera;                        // Used for referencing the camera.
        private float m_ZoomSpeed;                      // Reference speed for the smooth damping of the orthographic size.
        private Vector3 m_MoveVelocity;                 // Reference velocity for the smooth damping of the position.
        private Vector3 m_DesiredPosition;              // The position the camera is moving towards.
        public Vector3 offset = new Vector3(0, 2, -3);  // TPS課題:カメラオフセット
        private Quaternion rotationTankTurret;          // TPS課題:砲塔の回転を取得

        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
        }

        private void Start()
        {
            Invoke(nameof(AssignTarget), 1f);
        }

        /// <summary>
        /// NetworkManagerからローカルプレイヤーのタンクを取得し、カメラのターゲットとして設定します。
        /// </summary>
        private void AssignTarget()
        {
            if (NetworkManager.Instance != null)
            {
                // ローカルプレイヤーのプレイヤー番号を取得
                int localPlayerNumber = NetworkManager.Instance.playerId;

                // ローカルプレイヤーのタンクを取得
                GameObject localTank = NetworkManager.Instance.GetTank(localPlayerNumber);

                if (localTank != null)
                {
                    m_Target = localTank.transform;
                    Debug.Log($"CameraControl: ローカルプレイヤーのタンク (Player {localPlayerNumber}) をターゲットに設定しました。");
                }
                else
                {
                    Debug.LogWarning("CameraControl: ローカルプレイヤーのタンクが見つかりません。");
                }
            }
            else
            {
                Debug.LogWarning("CameraControl: NetworkManager.Instance が null です。CameraControlでターゲットを設定できません。");
            }
        }

        private void FixedUpdate()
        {
            if (m_Target != null)
            {
                Move();
            }
        }

        /// <summary>
        /// カメラをターゲットの位置に移動し、ターゲットを見続けます。
        /// </summary>
        private void Move()
        {
            // ターゲットの砲塔の回転を取得
            Transform turretTransform = m_Target.Find("TankRenderers/TankTurret");
            if (turretTransform != null)
            {
                rotationTankTurret = turretTransform.rotation; // TPS課題:砲塔の回転を取得

                // カメラの望ましい位置を計算
                Vector3 desiredPosition = m_Target.position + rotationTankTurret * offset;

                // カメラの位置をスムーズに移動
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref m_MoveVelocity, m_DampTime);

                // カメラがターゲットを常に見るように
                transform.LookAt(m_Target);
            }
            else
            {
                Debug.LogWarning("CameraControl: TankTurretがターゲット内に見つかりません。");
            }
        }

        // TPS課題:不要になったメソッドたち
    }
}
