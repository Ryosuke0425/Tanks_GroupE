using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class PlayerStockArea : MonoBehaviour
{
    [SerializeField] private Image[] shell1s;       //砲弾のストック数
    [SerializeField] private Image[] shell10s;
    [SerializeField] private Image[] mines;
    public void UpdatePlayerStockArea(int stockCount, WeaponStockData mineStock) //HudManager.csで使用,弾の表示非表示
    {
        for (int i = 0; i < shell1s.Length; i++)
        {
            if ((i < stockCount%10))
            {
                shell1s[i].gameObject.SetActive(true);
            }
            else
            {
                shell1s[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < shell10s.Length; i++)
        {
            if ((i + 1) * 10 <= stockCount)
            {
                shell10s[i].gameObject.SetActive(true);
            }
            else
            {
                shell10s[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < mines.Length; i++)
        {
            if (i < mineStock.CurrentStock)
            {
                mines[i].gameObject.SetActive(true);
            }
            else
            {
                mines[i].gameObject.SetActive(false);
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
