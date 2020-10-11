﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineX;

public class ItemVisualInfoBank : GameSystem<ItemVisualInfoBank>
{
    [SerializeField] private ItemVisualInfo _fallbackVisualInfo;
    public List<ItemVisualInfo> ItemsVisualInfo = new List<ItemVisualInfo>();

    private Dictionary<SimAssetId, ItemVisualInfo> _idToItemInfo = new Dictionary<SimAssetId, ItemVisualInfo>();

    public override void OnGameAwake()
    {
        base.OnGameAwake();

        foreach (ItemVisualInfo ItemInfo in ItemsVisualInfo)
        {
            _idToItemInfo.Add(ItemInfo.ID.GetSimAssetId(), ItemInfo);
        }
    }

    public ItemVisualInfo GetItemInfoFromID(SimAssetId itemID)
    {
        if (_idToItemInfo.TryGetValue(itemID, out ItemVisualInfo result))
        {
            return result;
        }

        return _fallbackVisualInfo;
    }

    public SimAssetId GetIDFromItemInfo(ItemVisualInfo itemInfo)
    {
        if (_idToItemInfo.ContainsValue(itemInfo))
        {
            SimAssetId result = _idToItemInfo.FindFirstKeyWithValue(itemInfo);
            return result;
        }

        Log.Error($"Failed to find item ID from item info {itemInfo}");

        return SimAssetId.Invalid;
    }
}