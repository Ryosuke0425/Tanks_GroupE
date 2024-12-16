using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Complete
{
    public class Minimap : MonoBehaviour
    {
        [HideInInspector] public Transform m_Target; // ローカルプレイヤーのタンクをターゲットに

        private Camera m_Camera;                     
        public Vector3 offset = new Vector3(0, 30, 0);

        private bool targetAssigned = false;

        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
            if (m_Camera == null)
            {
                Debug.LogError("Minimap: 子オブジェクトにCameraコンポーネントが見つかりません。");
            }
        }

        private void Start()
        {
            // タンクがシーンで生成・登録されるまで待つため、遅延でターゲット設定
            Invoke(nameof(AssignTarget), 1f);
        }

        /// <summary>
        /// NetworkManagerからローカルプレイヤーのタンクを取得し、ミニマップのターゲットとして設定します。
        /// </summary>
        private void AssignTarget()
        {
            if (NetworkManager.Instance != null)
            {
                int localPlayerNumber = NetworkManager.Instance.playerId;
                // NetworkManagerからローカルプレイヤーのタンクを取得
                GameObject localTank = NetworkManager.Instance.GetTank(localPlayerNumber);

                if (localTank != null)
                {
                    m_Target = localTank.transform;
                    targetAssigned = true;
                    Debug.Log($"Minimap: ローカルプレイヤーのタンク (Player {localPlayerNumber}) をターゲットに設定しました。");
                }
                else
                {
                    Debug.LogWarning("Minimap: ローカルプレイヤーのタンクが見つかりません。後ほど再試行します。");
                    // 再度試行したい場合は再度Invoke
                    Invoke(nameof(AssignTarget), 1f);
                }
            }
            else
            {
                Debug.LogWarning("Minimap: NetworkManager.Instance が null です。Minimapでターゲットを設定できません。");
                // 再度試行したい場合は再度Invoke
                Invoke(nameof(AssignTarget), 1f);
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
        /// ミニマップカメラをターゲットの位置に移動し、回転を合わせます。
        /// </summary>
        private void Move()
        {
            Vector3 desiredPosition = m_Target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.fixedDeltaTime * 5f);
            // ターゲットのY回転に合わせてミニマップを回転
            transform.rotation = Quaternion.Euler(0, m_Target.eulerAngles.y, 0f);
        }
    }
}
