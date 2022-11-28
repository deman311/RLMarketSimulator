using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class StoreController : MonoBehaviour
{
    [SerializeField] Canvas uiBalance;

    int level = 1;
    float balance = 200f;
    int maxStock = 20;
    int currentStock = 0;

    float timer = 0f;
    StockManager sm;

    Dictionary<string, Product> products = new Dictionary<string, Product>();
    List<CustomerController> queue = new List<CustomerController>();
    public static object _QueueLock = new object();

    Dictionary<string, int> prodToITbeta = new Dictionary<string, int>
    {
        { StockManager.prodNames[0], 1 },
        { StockManager.prodNames[1], 1 },
        { StockManager.prodNames[2], 1 },
        { StockManager.prodNames[3], 1 },
        { StockManager.prodNames[4], 1 }
    };

    [SerializeField] TextMeshProUGUI cash, apple, shirt, phone, gpu, rolex; // UI Amounts

    void Awake()
    {
        sm = GameObject.Find("SimulationController").GetComponent<StockManager>();
        InitPricesAndIT();
        UpdateUIPrices(); // inital price set
        phone.transform.parent.gameObject.SetActive(false);
        gpu.transform.parent.gameObject.SetActive(false);
        rolex.transform.parent.gameObject.SetActive(false);

        //PrintShop();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 0.5f)
        {
            UpdateUIPrices();
            timer = 0;
        }

        // make the text face the camera
        cash.text = "" + balance.ToString("#.##") + "$";
        uiBalance.transform.LookAt(Camera.main.transform.position);
        uiBalance.transform.Rotate(new Vector3(0, 180, 0));

        if (SafeDequeue(out CustomerController cc))
            Transaction(cc);
    }

    private void UpdateUIPrices()
    {
        apple.text = products.TryGetValue("Apple", out Product val) ? "" + val.amount : "0";
        shirt.text = products.TryGetValue("Shirt", out Product val2) ? "" + val2.amount : "0";
        phone.text = products.TryGetValue("Phone", out Product val3) ? "" + val3.amount : "0";
        gpu.text = products.TryGetValue("GPU", out Product val4) ? "" + val4.amount : "0";
        rolex.text = products.TryGetValue("Rolex", out Product val5) ? "" + val5.amount : "0";
    }

    public float GetBalance()
    {
        return balance;
    }

    public void LevelUp()
    {
        level++;
        transform.localScale *= 1.2f;
        if (level == 2)
        {
            GetComponent<Renderer>().material.color = Color.green;
            phone.transform.parent.gameObject.SetActive(true);
            gpu.transform.parent.gameObject.SetActive(true);
            maxStock = 50;
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
            rolex.transform.parent.gameObject.SetActive(true);
            maxStock = 100;
        }
    }

    public int GetLevel()
    {
        return level;
    }

    public void SafeEnqueue(CustomerController cc)
    {
        lock (_QueueLock)
        {
            queue.Add(cc);
        }
    }

    public bool SafeDequeue(out CustomerController cc)
    {
        lock (_QueueLock)
        {
            if (queue.Count > 0)
            {
                cc = queue[0];
                queue.Remove(queue[0]);
                return true;
            }
            else
            {
                cc = null;
                return false;
            }
        }
    }

    private void PrintShop()
    {
        StringBuilder sb = new();
        if (products.Count > 0)
            sb.Append("[S]" + name + " ");
        foreach (Product p in products.Values)
            sb.Append(p + ", ");
        sb.Append("Balance: " + balance);

        if (!string.IsNullOrEmpty(sb.ToString()))
            Debug.Log(sb.ToString());
    }

    public void Restock()
    {
        foreach (string prodName in sm.BuyList(level))
            BuyProduct(new Product(prodName, GetRestockAmount(prodName)));
    }

    private int GetRestockAmount(string prodName)
    {
        int total_IT = 0;
        products.Values.ToList().ForEach(p => total_IT += p.Invest_tend);

        if (total_IT == 0)
            total_IT = maxStock;

        if (products.TryGetValue(prodName, out Product p))
            return (int)Mathf.Round((float)p.Invest_tend / total_IT * (maxStock - currentStock));

        return (int)Mathf.Round((float)Random.Range(1, 11) / total_IT * (maxStock - currentStock));
    }


    private bool IsStockEmpty()
    {
        return products.ToListPooled().TrueForAll(kvp => kvp.Value.amount == 0);
    }

    public void Tax(int TAX)
    {
        int finalTax = TAX * level * level;
        foreach (Product product in products.Values.Where(p => p.amount > 0))
            finalTax += sm.GetProductTax(product.name) * product.amount;
        balance -= finalTax;

        if (balance < 0)
            GameObject.Find("SimulationController").GetComponent<PathfindingManager>().SafeRemove(gameObject);
    }

    public void Transaction(CustomerController cc)
    {
        bool hasBoughtSomething = false;
        Dictionary<string, bool> hasLooked = new Dictionary<string, bool>();
        foreach (string prodName in StockManager.prodNames)
            hasLooked.Add(prodName, false);

        foreach (Product p in cc.GetProducts())
        {
            SellProduct(p, cc, ref hasBoughtSomething);
            hasLooked[p.name] = true;
            prodToITbeta[p.name] = 1;
        }

        // interate over all the products that were overlooked and exist in the shop and decrease IT.
        foreach (KeyValuePair<string, bool> kvp in hasLooked.Where(kvp => !kvp.Value))
            if (products.TryGetValue(kvp.Key, out Product p))
            {
                p.Invest_tend -= prodToITbeta[p.name];
                prodToITbeta[p.name]++;
            }

        cc.CompleteTransaction(hasBoughtSomething);
    }

    public void RandomOffsetPrice(Product product, int alpha)
    {
        float price = sm.GetProductionPrice(product.name);
        float offset = sm.GetMaxPrice(product.name) * Random.Range(0, alpha) / 100f;
        product.Price = price + offset;
    }

    public Dictionary<string, float> GetProductPrices()
    {
        Dictionary<string, float> temp = new Dictionary<string, float>();
        foreach (KeyValuePair<string, Product> kvp in products)
            temp.Add(kvp.Key, kvp.Value.Price);
        return temp;
    }

    void InitPricesAndIT()
    {
        foreach (string prodName in sm.BuyList(level))
            BuyProduct(new Product(prodName, GetRestockAmount(prodName)));
    }

    /// <summary>
    /// Update balance and add new products to the list,
    /// if they don't exist initiallize them.
    /// </summary>
    public void BuyProduct(Product product)
    {
        float price = sm.GetProductionPrice(product.name);
        if (products.TryGetValue(product.name, out Product existingProd))
        {
            //product.amount = existingProd.Invest_tend;
            if (balance - product.amount * price < 0)
                product.amount = Mathf.FloorToInt((balance > 0 ? balance : 1) / price);
            balance -= product.amount * price;
            existingProd.amount += product.amount;
            Debug.Log(product.amount);
        }
        else
        {
            RandomOffsetPrice(product, 10);
            //product.amount = product.Invest_tend;
            if (balance - product.amount * price < 0)
                product.amount = Mathf.FloorToInt((balance > 0 ? balance : 1) / price);
            balance -= product.amount * price;
            products.Add(product.name, product);
        }
        currentStock += product.amount;
    }

    /// <summary>
    /// Takes in a product that its values represent:
    ///     name - product name.
    ///     amount - how many to sell.
    ///     price / invest_rate - change for a transaction
    ///  returns the amount sold.
    /// </summary>
    public void SellProduct(Product product, CustomerController cc, ref bool hasBoughtSomething)
    {
        float price_delta =
            GameObject.Find("SimulationController").GetComponent<StockManager>().GetMaxPrice(product.name)
            * (0.05f + Random.Range(-0.03f, 0.05f)); // 5% + alpha(-3%,5%) of max price

        if (products.TryGetValue(product.name, out Product existingProd) && existingProd.amount > 0)
        {
            int sold = 0;
            if (existingProd.amount < product.amount)
                sold = existingProd.amount;
            else
                sold = product.amount;

            if (existingProd.Price <= product.Price)
            {
                existingProd.Price += price_delta * sold;
                existingProd.amount -= sold;
                //existingProd.Invest_tend += sold;
                product.amount -= sold;

                if (!hasBoughtSomething)
                    hasBoughtSomething = true;

                balance += sold * existingProd.Price;
                currentStock -= sold;
            }
            else // price too high
            {
                // Store changes
                float bankruptPanic = Mathf.Clamp(existingProd.Price / (balance > 0 ? balance : 1), 0, 0.2f * existingProd.Price); // can only range from 0 to 3x
                existingProd.Price -= price_delta + bankruptPanic; // 10% + delta + (20% to epsilon)
                existingProd.Invest_tend -= 1;

                // Customer changes
                product.Price += price_delta * (CustomerManager.GetMaxTTL() / cc.ttl);
            }
            existingProd.Invest_tend += product.amount;
        }
        else // product does not exist in store
        {
            product.Price += price_delta * (CustomerManager.GetMaxTTL() / cc.ttl);
            if (existingProd != null)
                existingProd.Invest_tend += product.amount / 2; // buy half of wanted
        }
    }

    public void OnDrawGizmosSelected()
    {
        PrintShop();
    }
}
