using UnityEngine;
using UnityEngine.UI;

public class StampManager : MonoBehaviour
{
    public GridLayoutGroup stampGrid; // スタンプを並べるGridLayoutGroup
    public GameObject stampButtonPrefab; // スタンプボタンのPrefab

    void Start()
    {
        // サンプルスタンプIDの追加
        for (int i = 0; i < 10; i++)
        {
            CreateStampButton(i);
        }
    }

    void CreateStampButton(int stampNo)
    {
        GameObject stampButton = Instantiate(stampButtonPrefab, stampGrid.transform);
        Button button = stampButton.GetComponent<Button>();
        button.onClick.AddListener(() => OnStampClicked(stampNo));

        // スタンプ画像を設定（例: スタンプIDで画像選択）
        Image stampImage = stampButton.GetComponent<Image>();
        stampImage.sprite = GetStampSprite(stampNo);
    }

    void OnStampClicked(int stampNo)
    {
        Debug.Log($"Stamp clicked: {stampNo}");
        SendStampToServer(stampNo);
    }

    Sprite GetStampSprite(int stampNo)
    {
        // スタンプIDに応じた画像を取得（仮の実装）
        return Resources.Load<Sprite>($"Stamps/Stamp_{stampNo}");
    }

    void SendStampToServer(int stampNo)
    {
        Debug.Log($"Sending stamp to server: {stampNo}");
        // WebSocketまたはHTTP通信でサーバーへ送信
    }
}
