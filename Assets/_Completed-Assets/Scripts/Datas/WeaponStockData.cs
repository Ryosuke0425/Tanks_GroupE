using System;
using UnityEngine;

[Serializable]
public class WeaponStockData
{
    [SerializeField] private int initialStock = 0;
    [SerializeField] private int maxStock = 3;
    [SerializeField] private int stockInCartridge = 1;
    public int StockInCartridge { get => stockInCartridge; }
    private int currentStock = 0;
    public int CurrentStock { get => currentStock; }

    public WeaponStockData(int initialStock, int maxStock, int stockInCartridge)
    {
        this.initialStock = initialStock;
        this.maxStock = maxStock;
        this.stockInCartridge = stockInCartridge;
        currentStock = initialStock;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddStock(int amount)
    {
        currentStock += amount;
        if (currentStock > maxStock)
        {
            currentStock = maxStock;
        }
    }

    public void ConsumeStock(int amount)
    {
        currentStock -= amount;
        if (currentStock < 0)
        {
            currentStock = 0;
        }
    }
}
