using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StampManager : MonoBehaviour
{
    [Header("Stamp Buttons")]
    [Tooltip("インスペクターでスタンプボタンをすべてアサインしてください。")]
    public Button[] stampButtons; // 例: Stamp0Button〜Stamp5Button

    [Header("Stamp Prefabs")]
    [Tooltip("インスペクターでスタンプPrefabをすべてアサインしてください。")]
    public GameObject[] stampPrefabs; // 例: StampPrefab0〜StampPrefab5

    [Header("Stamp Display Parent")]
    [Tooltip("スタンプPrefabをインスタンス化する親オブジェクトをアサインしてください。")]
    public Transform stampParent; // 通常はCanvasの子オブジェクト

    [Header("References")]
    [Tooltip("StampManager にアタッチされた LobbyManager をアサインしてください。")]
    public LobbyManager lobbyManager;

    void Start()
    {
        // LobbyManagerがアサインされているか確認
        if (lobbyManager == null)
        {
            Debug.LogError("StampManager: LobbyManagerがアサインされていません。");
            return;
        }

        // スタンプボタンの配列が正しくアサインされているか確認
        if (stampButtons == null || stampButtons.Length == 0)
        {
            Debug.LogError("StampManager: stampButtonsがアサインされていません。");
            return;
        }

        // スタンプPrefabの配列が正しくアサインされているか確認
        if (stampPrefabs == null || stampPrefabs.Length != stampButtons.Length)
        {
            Debug.LogError("StampManager: stampPrefabs 配列に正しい数のPrefabがアサインされていません。");
            return;
        }

        // 各ボタンにクリックリスナーを設定
        for (int i = 0; i < stampButtons.Length; i++)
        {
            int stampId = i; // スタンプIDを保持
            stampButtons[i].onClick.AddListener(() => SendStamp(stampId));
        }

        // stampParentがアサインされているか確認
        if (stampParent == null)
        {
            Debug.LogError("StampManager: stampParentがアサインされていません。");
        }
    }

    /// <summary>
    /// スタンプボタンがクリックされた際に呼び出されるメソッド
    /// </summary>
    /// <param name="stampId">送信するスタンプのID</param>
    private void SendStamp(int stampId)
    {
        Debug.Log($"StampManager: Stamp button clicked: Stamp{stampId}");
        lobbyManager.SendStamp(stampId);
    }

    /// <summary>
    /// サーバーから受信したスタンプを表示するメソッド
    /// </summary>
    /// <param name="stampId">表示するスタンプのID</param>
    public void DisplayStamp(int stampId)
    {
        Debug.Log($"StampManager: DisplayStamp called with stampId: {stampId}");
        StartCoroutine(DisplayStampRoutine(stampId));
    }

    /// <summary>
    /// スタンプの表示と非表示を管理するコルーチン
    /// </summary>
    /// <param name="stampId">表示するスタンプのID</param>
    /// <returns></returns>
    private IEnumerator DisplayStampRoutine(int stampId)
    {
        if (stampPrefabs == null || stampParent == null)
        {
            Debug.LogError("StampManager: stampPrefabs または stampParent がアサインされていません。");
            yield break;
        }

        // スタンプIDが範囲内か確認
        if (stampId < 0 || stampId >= stampPrefabs.Length)
        {
            Debug.LogError($"StampManager: stampId {stampId} は範囲外です。");
            yield break;
        }

        // Prefabをインスタンス化
        GameObject stampInstance = Instantiate(stampPrefabs[stampId], stampParent);
        Debug.Log($"StampManager: Stamp{stampId} instantiated.");

        // スタンプを表示する時間
        float displayDuration = 5f;

        // 5秒待機
        yield return new WaitForSeconds(displayDuration);

        if (stampInstance != null)
        {
            Destroy(stampInstance);
            Debug.Log($"StampManager: Stamp{stampId} destroyed after {displayDuration} seconds.");
        }
    }
}
