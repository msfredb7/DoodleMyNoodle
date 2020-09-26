﻿using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngineX;


// THIS CLASS SHOULD NOT BE SERIALIZABLE
public abstract class PawnControllerInputBase
{
    public Entity PawnController;

    protected PawnControllerInputBase(Entity pawnController)
    {
        PawnController = pawnController;
    }
}

/// <summary>
/// This system makes sure inputs sent from the previous frame don't leak to the next. 
/// This is a necessary precaution because inputs are not natively serialized in the sim world
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ClearPawnControllerInputSystem : SimComponentSystem
{
    private ExecutePawnControllerInputSystem _executeSys;

    protected override void OnCreate()
    {
        base.OnCreate();

        _executeSys = World.GetOrCreateSystem<ExecutePawnControllerInputSystem>();
    }

    protected override void OnUpdate()
    {
        foreach (PawnControllerInputBase input in _executeSys.Inputs)
        {
            Log.Warning($"The PawnControllerInput {input} seems to have been queued too late. " +
                $"Use [UpdateBefore({nameof(ExecutePawnControllerInputSystem)})] to make sure you deliver the input in time.");
        }
        _executeSys.Inputs.Clear();
    }
}

/// <summary>
/// This system executes the queued inputs
/// </summary>
[UpdateInGroup(typeof(InputSystemGroup))]
public class ExecutePawnControllerInputSystem : SimComponentSystem
{
    public readonly List<PawnControllerInputBase> Inputs = new List<PawnControllerInputBase>();

    protected override void OnUpdate()
    {
        foreach (var input in Inputs)
        {
            ExecuteInput(input);
        }
        Inputs.Clear();
    }

    private void ExecuteInput(PawnControllerInputBase input)
    {
        // Getting the Pawn from the input
        Entity pawn = Entity.Null;
        if (EntityManager.TryGetComponentData(input.PawnController, out ControlledEntity controlledEntity))
        {
            pawn = controlledEntity.Value;

            if (!EntityManager.Exists(pawn))
                pawn = Entity.Null;
        }

        // Handling different types of Sim Inputs
        switch (input)
        {
            case PawnControllerInputSetStartingInventory equipStartingInventoryInput:
                if (pawn != Entity.Null && equipStartingInventoryInput.KitNumber != 0)
                {
                    DynamicBuffer<InventoryItemPrefabReference> startingInventory = default;
                    Entities
                        .WithAll<ItemKitTag>()
                        .ForEach((DynamicBuffer<InventoryItemPrefabReference> inventoryItems, ref SimAssetId assetID) =>
                        {
                            if (equipStartingInventoryInput.KitNumber == assetID.Value)
                            {
                                startingInventory = inventoryItems;
                            }
                        });

                    if (startingInventory.IsCreated)
                    {
                        CommonWrites.InstantiateToEntityInventory(Accessor, pawn, startingInventory);
                    }
                }
                else
                {
                    Log.Warning($"Couldn't equip starting inventory {equipStartingInventoryInput.KitNumber}");
                }
                break;

            case PawnControllerInputSetPawnName nameInput:
                if (pawn != Entity.Null)
                {
                    EntityManager.SetOrAddComponentData(pawn, new Name() { Value = nameInput.Name });
                }

                break;

            case PawnControllerInputNextTurn pawnInputNextTurn:
                EntityManager.SetOrAddComponentData(pawnInputNextTurn.PawnController, new ReadyForNextTurn() { Value = pawnInputNextTurn.ReadyForNextTurn });
                break;

            case PawnControllerInputUseItem useItemInput:
                if (pawn != Entity.Null)
                    ExecuteUseItemInput(useItemInput, pawn);
                break;

            case PawnControllerInputUseInteractable useInteractableInput:
                if (pawn != Entity.Null)
                    ExecuteUseInteractableInput(useInteractableInput, pawn);
                break;

            case PawnControllerInputEquipItem pawnInputEquipItem:
                if (pawn != Entity.Null)
                    ExecuteEquipItemInput(pawnInputEquipItem, pawn);
                break;

            case PawnControllerInputDropItem pawnInputDropItem:
                if (pawn != Entity.Null)
                    ExecuteDropItemInput(pawnInputDropItem, pawn);
                break;
        }
    }

    private void ExecuteDropItemInput(PawnControllerInputDropItem pawnInputDropItem, Entity pawn)
    {
        FixTranslation pawnTranslation = EntityManager.GetComponentData<FixTranslation>(pawn);
        int2 pawnTile = Helpers.GetTile(pawnTranslation);
        var tileWorld = CommonReads.GetTileWorld(Accessor);
        if (!tileWorld.IsValid(pawnTile))
        {
            return;
        }

        // Does the player have an inventory with a valid item at 'ItemIndex' ?
        if (!EntityManager.TryGetBuffer(pawn, out DynamicBuffer<InventoryItemReference> p) ||
            p.Length <= pawnInputDropItem.ItemIndex)
        {
            return;
        }

        // Searching for a chest entity
        DynamicBuffer<InventoryItemReference> chestInventory = default;

        Entity chestEntity = CommonReads.FindFirstTileActorWithComponent<InventoryItemReference, Interactable>(Accessor, tileWorld, pawnTile);
        if (chestEntity != Entity.Null)
        {
            chestInventory = EntityManager.GetBuffer<InventoryItemReference>(chestEntity);
        }

        // Didn't found an inventory, let's spawn one
        if (!chestInventory.IsCreated)
        {
            InteractableInventoryPrefabReferenceSingletonComponent chestPrefab = GetSingleton<InteractableInventoryPrefabReferenceSingletonComponent>();
            chestEntity = EntityManager.Instantiate(chestPrefab.Prefab);
            EntityManager.SetComponentData(chestEntity, pawnTranslation);

            chestInventory = Accessor.GetBuffer<InventoryItemReference>(chestEntity);
        }

        // Move item from player's inventory to chest inventory
        if (!CommonReads.IsInventoryFull(Accessor, chestEntity))
        {
            DynamicBuffer<InventoryItemReference> pawnInventory = EntityManager.GetBuffer<InventoryItemReference>(pawn);

            Entity itemToMove = pawnInventory[pawnInputDropItem.ItemIndex].ItemEntity;
            if (CommonWrites.TryIncrementStackableItemInInventory(Accessor, chestEntity, itemToMove, chestInventory))
            {
                CommonWrites.DecrementStackableItemInInventory(Accessor, pawn, itemToMove);
            }
            else
            {
                Entity entityToGiveToChest;

                // We did not find any stackable in destination inventory, but we want to transfer only one if stackable
                if (EntityManager.HasComponent<ItemStackableData>(itemToMove))
                {
                    entityToGiveToChest = EntityManager.Instantiate(itemToMove);
                    EntityManager.SetComponentData(entityToGiveToChest, new ItemStackableData() { Value = 1 });
                    CommonWrites.DecrementStackableItemInInventory(Accessor, pawn, itemToMove);
                }
                else
                {
                    entityToGiveToChest = itemToMove;
                    pawnInventory.RemoveAt(pawnInputDropItem.ItemIndex);
                }

                chestInventory = EntityManager.GetBuffer<InventoryItemReference>(chestEntity);
                chestInventory.Add(new InventoryItemReference() { ItemEntity = entityToGiveToChest });
            }
        }
    }

    private void ExecuteEquipItemInput(PawnControllerInputEquipItem pawnInputEquipItem, Entity pawn)
    {
        int2 itemEntityTile = pawnInputEquipItem.ItemEntityPosition;
        var tileWorld = CommonReads.GetTileWorld(Accessor);
        if (!tileWorld.IsValid(itemEntityTile))
        {
            return;
        }

        // Pawn has inventory ?
        if (!EntityManager.HasComponent<InventoryItemReference>(pawn))
            return;

        // Find chest inventory
        Entity chestEntity = CommonReads.FindFirstTileActorWithComponent<InventoryItemReference, Interactable>(Accessor, itemEntityTile);
        if (chestEntity == Entity.Null)
        {
            return;
        }

        // Get item buffer
        DynamicBuffer<InventoryItemReference> chestInventory = EntityManager.GetBuffer<InventoryItemReference>(chestEntity);
        if (chestInventory.Length <= pawnInputEquipItem.ItemIndex)
        {
            return;
        }

        // Get item to move
        Entity item = chestInventory[pawnInputEquipItem.ItemIndex].ItemEntity;

        if (!CommonReads.IsInventoryFull(Accessor, pawn))
        {
            // Trying to increment stack on pawn and if succeed decrement original item on chest
            if (CommonWrites.TryIncrementStackableItemInInventory(Accessor, pawn, item, EntityManager.GetBuffer<InventoryItemReference>(pawn)))
            {
                CommonWrites.DecrementStackableItemInInventory(Accessor, chestEntity, item);
            }
            else
            {
                Entity itemToGiveToPawn;

                // We did not find any stackable in destination inventory, but we want to transfer only one if stackable
                if (EntityManager.HasComponent<ItemStackableData>(item))
                {
                    itemToGiveToPawn = EntityManager.Instantiate(item);
                    EntityManager.SetComponentData(itemToGiveToPawn, new ItemStackableData() { Value = 1 });
                    CommonWrites.DecrementStackableItemInInventory(Accessor, chestEntity, item);
                }
                else
                {
                    chestInventory.RemoveAt(pawnInputEquipItem.ItemIndex);
                    itemToGiveToPawn = item;
                }

                // Move item from chest inventory to player's inventory
                EntityManager.GetBuffer<InventoryItemReference>(pawn).Add(new InventoryItemReference() { ItemEntity = itemToGiveToPawn });
            }
        }
    }

    private void ExecuteUseItemInput(PawnControllerInputUseItem inputUseItem, Entity pawn)
    {
        void LogDiscardReason(string str)
        {
            Log.Info($"Discarding {inputUseItem.PawnController}'s input : {str}");
        }

        if (!EntityManager.TryGetBuffer(pawn, out DynamicBuffer<InventoryItemReference> inventory))
        {
            LogDiscardReason($"Pawn has no {nameof(DynamicBuffer<InventoryItemReference>)}.");
            return;
        }

        if (inputUseItem.ItemIndex < 0 || inputUseItem.ItemIndex >= inventory.Length)
        {
            LogDiscardReason($"Item {inputUseItem.ItemIndex} is out of range ({inventory.Length}).");
            return;
        }

        Entity item = inventory[inputUseItem.ItemIndex].ItemEntity;

        GameAction gameAction = GetGameActionFromEntity(item);

        if (gameAction == null)
        {
            LogDiscardReason($"Item {item}'s gameActionId is invalid.");
            return;
        }

        GameAction.UseContext useContext = new GameAction.UseContext()
        {
            InstigatorPawn = pawn,
            InstigatorPawnController = inputUseItem.PawnController,
            Entity = item
        };

        if (!gameAction.TryUse(Accessor, useContext, inputUseItem.GameActionData, out string debugReason))
        {
            LogDiscardReason($"Can't Trigger {gameAction} because: {debugReason}");
            return;
        }
    }

    private void ExecuteUseInteractableInput(PawnControllerInputUseInteractable inputUseInteractable, Entity pawn)
    {
        void LogDiscardReason(string str)
        {
            Log.Info($"Discarding input {inputUseInteractable} : {str}");
        }

        Entity interactableEntity = CommonReads.FindFirstTileActorWithComponent<Interactable>(Accessor, inputUseInteractable.InteractablePosition);
        if (interactableEntity == Entity.Null)
        {
            return;
        }

        fix interactableTileDistance = Accessor.GetComponentData<Interactable>(interactableEntity).Range;
        FixTranslation pawnPosition = EntityManager.GetComponentData<FixTranslation>(pawn);
        fix3 interactablePosition = Helpers.GetTileCenter(inputUseInteractable.InteractablePosition);

        int tilesBetween = fix.RoundToInt(fix.Abs((interactablePosition.x - pawnPosition.Value.x) + (interactablePosition.y - pawnPosition.Value.y)));
        if (tilesBetween > interactableTileDistance)
        {
            return;
        }

        GameAction gameAction = GetGameActionFromEntity(interactableEntity);

        if (gameAction == null)
        {
            LogDiscardReason($"Interactable {interactableEntity}'s gameActionId is invalid.");
            return;
        }

        GameAction.UseContext useContext = new GameAction.UseContext()
        {
            InstigatorPawn = pawn,
            InstigatorPawnController = inputUseInteractable.PawnController,
            Entity = interactableEntity
        };

        // currently no use parameters
        if (!gameAction.TryUse(Accessor, useContext, null))
        {
            LogDiscardReason($"Can't Trigger {gameAction}");
            return;
        }
    }

    private GameAction GetGameActionFromEntity(Entity entity)
    {
        if (EntityManager.TryGetComponentData(entity, out GameActionId gameActionId) && gameActionId.IsValid)
        {
            return GameActionBank.GetAction(gameActionId);
        }

        return null;
    }
}


internal partial class CommonWrites
{
    public static void QueuePawnControllerInput(ISimWorldReadWriteAccessor accessor, PawnControllerInputBase input, Entity pawnController)
    {
        input.PawnController = pawnController;
        QueuePawnControllerInput(accessor, input);
    }

    public static void QueuePawnControllerInput(ISimWorldReadWriteAccessor accessor, PawnControllerInputBase input)
    {
        ExecutePawnControllerInputSystem system = accessor.GetOrCreateSystem<ExecutePawnControllerInputSystem>();

        system.Inputs.Add(input);
    }

    public static bool TryInputUseItem<T>(ISimWorldReadWriteAccessor accessor, Entity entityController, int2 tile) where T : GameAction
    {
        if (!accessor.TryGetComponentData(entityController, out ControlledEntity pawn))
            return false;

        if (pawn == Entity.Null)
            return false;

        // get pawn's item
        Entity item = CommonReads.FindFirstItemWithGameAction<T>(accessor, pawn, out int itemIndex);
        if (item == Entity.Null)
            return false;

        // check item can be used
        var gameAction = GameActionBank.GetAction<T>();
        if (gameAction == null || !gameAction.CanBeUsedInContext(accessor, new GameAction.UseContext(entityController, pawn, item)))
            return false;

        // create game action's use data
        var useData = GameAction.UseParameters.Create(
            new GameActionParameterTile.Data(0, tile));

        // create input
        var input = new PawnControllerInputUseItem(entityController, itemIndex, useData);

        // queue input
        CommonWrites.QueuePawnControllerInput(accessor, input);

        return true;
    }
}