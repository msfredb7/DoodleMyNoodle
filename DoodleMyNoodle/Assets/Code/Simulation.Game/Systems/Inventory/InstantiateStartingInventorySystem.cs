﻿
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class InstantiateStartingInventorySystem : SimComponentSystem
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<InventoryItemReference>()
            .ForEach((Entity entity, DynamicBuffer<StartingInventoryItem> startingInventoryBuffer) =>
            {
                NativeArray<StartingInventoryItem> startingInventory = startingInventoryBuffer.ToNativeArray(Allocator.Temp);
                NativeArray<Entity> itemInstances = new NativeArray<Entity>(startingInventory.Length, Allocator.Temp);

                // Spawn items
                for (int i = 0; i < startingInventory.Length; i++)
                {
                    itemInstances[i] = EntityManager.Instantiate(startingInventory[i].ItemEntityPrefab);
                }

                // Add item references into inventory
                DynamicBuffer<InventoryItemReference> inventory = EntityManager.GetBuffer<InventoryItemReference>(entity);

                foreach (Entity itemInstance in itemInstances)
                {
                    if (!CommonReads.IsInventoryFull(Accessor, entity) || !CommonWrites.TryIncrementStackableItemInInventory(Accessor, entity, itemInstance, inventory))
                    {
                        inventory.Add(new InventoryItemReference() { ItemEntity = itemInstance });
                    }
                }

                // Remove starting inventory buffer
                EntityManager.RemoveComponent<StartingInventoryItem>(entity);

                itemInstances.Dispose();
                startingInventory.Dispose();
            });
    }
}
