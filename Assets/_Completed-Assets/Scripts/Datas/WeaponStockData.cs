using System;
using UnityEngine;

[Serializable]
public class WeaponStockData
{
    [SerializeField] private int initialStock = 30;
    [SerializeField] private int maxStock = 60;
    [SerializeField] private int stockInCartridge = 30;

    private int currentStock;
    public int CurrentStock => currentStock;

    public void InitializeStock()
    {
        currentStock = initialStock;
    }

    public void AddStock(int amount)
    {
        currentStock += amount;
        if (currentStock > maxStock)
        {
            currentStock = maxStock;
        }
    }

    public void UseStock(int amount)
    {
        currentStock -= amount;
        if (currentStock < 0)
        {
            currentStock = 0;
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
