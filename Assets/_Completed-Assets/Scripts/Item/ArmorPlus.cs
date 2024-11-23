using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArmorPlus
{
    public int quantity;     //アイテムの数
    public bool used;        //使用中かどうか

    public ArmorPlus(int quantity,bool used){
        this.quantity = quantity;
        this.used = used;
    }
    
}
