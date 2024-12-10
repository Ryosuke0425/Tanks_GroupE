using UnityEngine;

public class TabController : MonoBehaviour
{
    public GameObject commentView; // コメントビュー
    public GameObject stampView;   // スタンプビュー

    public void ShowCommentView()
    {
        commentView.SetActive(true);
        stampView.SetActive(false);
    }

    public void ShowStampView()
    {
        commentView.SetActive(false);
        stampView.SetActive(true);
    }
}
