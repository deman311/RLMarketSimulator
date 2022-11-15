using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StockManager : MonoBehaviour
{
    int maxLevel = 1;
    public readonly static List<string> prodNames = new List<string> { "Apple", "Shirt", "Phone", "GPU", "Rolex" };
    Dictionary<string, float> productToProduction = new Dictionary<string, float>
    {
        {prodNames[0], 1 },     // 1 - 20
        {prodNames[1], 15 },    // 15 - 100
        {prodNames[2], 90 },    // 90 - 500
        {prodNames[3], 300 },   // 300 - 2000
        {prodNames[4], 1500 }   // 1500 - 5000
    };
    Dictionary<string, float> productToMaxPrice = new Dictionary<string, float>
    {
        {prodNames[0], 20 },    // 1 - 20
        {prodNames[1], 100 },   // 15 - 100
        {prodNames[2], 500 },   // 90 - 500
        {prodNames[3], 2000 },  // 300 - 2000
        {prodNames[4], 5000 }   // 1500 - 5000
    };
    Dictionary<string, int> productToLevel = new Dictionary<string, int>
    {
        {prodNames[0], 1 },
        {prodNames[1], 1 },
        {prodNames[2], 2 },
        {prodNames[3], 2 },
        {prodNames[4], 3 }
    };

    public int GetMaxLevel()
    {
        return maxLevel;
    }

    public float GetProductionPrice(string prodName)
    {
        return productToProduction[prodName];
    }

    public float GetMaxPrice(string prodName)
    {
        return productToMaxPrice[prodName];
    }

    public List<string> BuyList(int level)
    {
        if (maxLevel < level)
            maxLevel = level;
        List<string> temp = new List<string>();
        foreach (KeyValuePair<string, int> kvp in productToLevel)
        {
            if (kvp.Value <= level)
                temp.Add(kvp.Key);
        }
        return temp;
    }
}