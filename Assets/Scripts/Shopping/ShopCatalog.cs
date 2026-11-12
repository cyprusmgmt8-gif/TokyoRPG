using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One product that can be bought. The Japanese text stays in the vocabulary catalog.
/// This only stores the vocabulary id and the price in yen.
/// </summary>
[Serializable]
public class ShopListing
{
    [Tooltip("Vocabulary id, for example konbini.water.")]
    public string vocabularyId;

    [Tooltip("Price in Japanese yen. Zero is allowed. A negative price is ignored.")]
    public int priceYen;
}

/// <summary>
/// Prices for one shop. Add another asset later for another location.
/// </summary>
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Tokyo RPG/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    [Tooltip("Location id, for example konbini. This does not load a scene.")]
    [SerializeField] string locationId = "";

    [SerializeField] List<ShopListing> listings = new List<ShopListing>();

    public string LocationId => locationId == null ? "" : locationId;

    public IReadOnlyList<ShopListing> Listings => listings;

    public bool TryGetPrice(string vocabularyId, out int priceYen)
    {
        priceYen = 0;
        ShopListing listing = Find(vocabularyId);
        if (listing == null || listing.priceYen < 0)
        {
            return false;
        }

        priceYen = listing.priceYen;
        return true;
    }

    public bool Contains(string vocabularyId)
    {
        return TryGetPrice(vocabularyId, out _);
    }

    ShopListing Find(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId) || listings == null)
        {
            return null;
        }

        for (int i = 0; i < listings.Count; i++)
        {
            ShopListing listing = listings[i];
            if (listing != null && string.Equals(listing.vocabularyId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                return listing;
            }
        }

        return null;
    }
}
