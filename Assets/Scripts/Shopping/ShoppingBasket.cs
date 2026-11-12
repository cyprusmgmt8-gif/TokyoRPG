using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// One product in the basket. The name and price are looked up elsewhere.
/// </summary>
[Serializable]
public class BasketLine
{
    public string vocabularyId;
    public int quantity;
}

[Serializable]
public class BasketSaveData
{
    public int version = 1;
    public List<BasketLine> items = new List<BasketLine>();
}

public interface IBasketStore
{
    BasketSaveData Load();
    void Save(BasketSaveData data);
}

/// <summary>
/// Saves the basket in the active profile folder.
/// </summary>
public class LocalBasketStore : IBasketStore
{
    public const string FileName = "tokyo-rpg-basket.json";

    public static string SavePath => SaveSlots.FilePath(FileName);

    public BasketSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new BasketSaveData();
        }

        try
        {
            BasketSaveData data = JsonUtility.FromJson<BasketSaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new BasketSaveData();
            }

            if (data.items == null)
            {
                data.items = new List<BasketLine>();
            }

            return data;
        }
        catch (Exception)
        {
            return new BasketSaveData();
        }
    }

    public void Save(BasketSaveData data)
    {
        if (data == null)
        {
            data = new BasketSaveData();
        }

        if (data.items == null)
        {
            data.items = new List<BasketLine>();
        }

        string folder = Path.GetDirectoryName(SavePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}

/// <summary>
/// The products the player is carrying to the counter.
/// Put this on the Konbini Player and assign the shop catalog.
/// </summary>
public class ShoppingBasket : MonoBehaviour
{
    public static ShoppingBasket Instance { get; private set; }

    public event Action Changed;

    [Tooltip("Prices for this shop. Konbini prices are in Assets/Data/Shopping.")]
    [SerializeField] ShopCatalog catalog;

    IBasketStore store;
    BasketSaveData data = new BasketSaveData();

    public ShopCatalog Catalog => catalog;

    void Awake()
    {
        Instance = this;
        store = new LocalBasketStore();
        data = Sanitize(store.Load(), out bool changed);
        if (changed)
        {
            store.Save(data);
        }

        if (GetComponent<BasketPanel>() == null)
        {
            gameObject.AddComponent<BasketPanel>();
        }

        if (GetComponent<CheckoutPanel>() == null)
        {
            gameObject.AddComponent<CheckoutPanel>();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public IReadOnlyList<BasketLine> Items
    {
        get
        {
            List<BasketLine> copy = new List<BasketLine>();
            if (data.items == null)
            {
                return copy;
            }

            for (int i = 0; i < data.items.Count; i++)
            {
                BasketLine line = data.items[i];
                if (line == null || line.quantity <= 0 || string.IsNullOrWhiteSpace(line.vocabularyId))
                {
                    continue;
                }

                copy.Add(new BasketLine
                {
                    vocabularyId = line.vocabularyId,
                    quantity = line.quantity
                });
            }

            return copy;
        }
    }

    public int QuantityOf(string vocabularyId)
    {
        BasketLine line = Find(vocabularyId);
        return line == null ? 0 : line.quantity;
    }

    public int TotalYen
    {
        get
        {
            if (catalog == null || data.items == null)
            {
                return 0;
            }

            long total = 0;
            for (int i = 0; i < data.items.Count; i++)
            {
                BasketLine line = data.items[i];
                if (line == null || line.quantity <= 0)
                {
                    continue;
                }

                if (!catalog.TryGetPrice(line.vocabularyId, out int price) || price < 0)
                {
                    continue;
                }

                long amount = (long)price * line.quantity;
                if (amount < 0 || total > int.MaxValue - amount)
                {
                    return int.MaxValue;
                }

                total += amount;
            }

            return (int)total;
        }
    }

    /// <summary>
    /// Adds one of a known product. Adding it again increases the quantity.
    /// </summary>
    public bool Add(string vocabularyId)
    {
        return Add(vocabularyId, 1);
    }

    /// <summary>
    /// Adds a positive quantity of a known product.
    /// </summary>
    public bool Add(string vocabularyId, int quantity)
    {
        if (quantity <= 0 || catalog == null || !catalog.TryGetPrice(vocabularyId, out _))
        {
            return false;
        }

        if (data.items == null)
        {
            data.items = new List<BasketLine>();
        }

        BasketLine line = Find(vocabularyId);
        if (line == null)
        {
            line = new BasketLine
            {
                vocabularyId = vocabularyId.Trim(),
                quantity = 0
            };
            data.items.Add(line);
        }

        if (line.quantity > int.MaxValue - quantity)
        {
            return false;
        }

        line.quantity += quantity;
        store.Save(data);
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes that product completely. A missing product is left unchanged.
    /// </summary>
    public bool Remove(string vocabularyId)
    {
        if (data.items == null)
        {
            return false;
        }

        for (int i = 0; i < data.items.Count; i++)
        {
            BasketLine line = data.items[i];
            if (line == null || !string.Equals(line.vocabularyId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            data.items.RemoveAt(i);
            store.Save(data);
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Empties the basket after a purchase and saves that empty list.
    /// </summary>
    public void Clear()
    {
        if (data.items == null)
        {
            data.items = new List<BasketLine>();
        }
        else
        {
            data.items.Clear();
        }

        store.Save(data);
        Changed?.Invoke();
    }

    BasketSaveData Sanitize(BasketSaveData source, out bool changed)
    {
        changed = false;
        BasketSaveData clean = new BasketSaveData();
        if (source == null || source.items == null)
        {
            changed = source == null || source.items == null;
            return clean;
        }

        for (int i = 0; i < source.items.Count; i++)
        {
            BasketLine line = source.items[i];
            if (line == null || string.IsNullOrWhiteSpace(line.vocabularyId) || line.quantity <= 0)
            {
                changed = true;
                continue;
            }

            if (catalog != null && !catalog.TryGetPrice(line.vocabularyId, out _))
            {
                changed = true;
                continue;
            }

            BasketLine existing = null;
            for (int j = 0; j < clean.items.Count; j++)
            {
                if (string.Equals(clean.items[j].vocabularyId, line.vocabularyId, StringComparison.OrdinalIgnoreCase))
                {
                    existing = clean.items[j];
                    break;
                }
            }

            if (existing == null)
            {
                clean.items.Add(new BasketLine
                {
                    vocabularyId = line.vocabularyId.Trim(),
                    quantity = line.quantity
                });
                if (!string.Equals(line.vocabularyId, line.vocabularyId.Trim(), StringComparison.Ordinal))
                {
                    changed = true;
                }

                continue;
            }

            changed = true;
            if (existing.quantity > int.MaxValue - line.quantity)
            {
                existing.quantity = int.MaxValue;
            }
            else
            {
                existing.quantity += line.quantity;
            }
        }

        if (source.items.Count != clean.items.Count)
        {
            changed = true;
        }

        return clean;
    }

    BasketLine Find(string vocabularyId)
    {
        if (data.items == null || string.IsNullOrWhiteSpace(vocabularyId))
        {
            return null;
        }

        for (int i = 0; i < data.items.Count; i++)
        {
            BasketLine line = data.items[i];
            if (line != null && string.Equals(line.vocabularyId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        return null;
    }
}
