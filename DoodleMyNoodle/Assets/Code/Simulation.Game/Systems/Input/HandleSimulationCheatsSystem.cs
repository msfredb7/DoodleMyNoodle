using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static fixMath;
using static Unity.Mathematics.math;

[NetSerializable]
public class SimInputCheatKillPlayerPawn : SimCheatInput
{
    public PersistentId PlayerId;
}

[NetSerializable]
public class SimInputCheatToggleInvincible : SimCheatInput
{
    public PersistentId PlayerId; // this should be an "Entity Pawn;" in the future
}

[NetSerializable]
public class SimInputCheatDamagePlayer : SimCheatInput
{
    public PersistentId PlayerId; // this should be an "Entity Pawn;" in the future
    public int Damage;
}

[NetSerializable]
public class SimInputCheatAddAllItems : SimCheatInput
{
    public PersistentId PlayerId; // this should be an "Entity Pawn;" in the future
}

[NetSerializable]
public class SimInputCheatNextTurn : SimCheatInput
{
}

[NetSerializable]
public class SimInputCheatInfiniteAP : SimCheatInput
{
    public PersistentId PlayerId; // this should be an "Entity Pawn;" in the future
}

[NetSerializable]
public class SimInputCheatTeleport : SimCheatInput
{
    public PersistentId PlayerId; // this should be an "Entity Pawn;" in the future
    public fix2 Destination;
}

public struct CheatsAllItemElement : IBufferElementData
{
    public Entity ItemPrefab;

    public static implicit operator Entity(CheatsAllItemElement val) => val.ItemPrefab;
    public static implicit operator CheatsAllItemElement(Entity val) => new CheatsAllItemElement() { ItemPrefab = val };
}

[UpdateInGroup(typeof(InputSystemGroup))]
public class HandleSimulationCheatsSystem : SimComponentSystem
{
    protected override void OnUpdate()
    {
        foreach (var input in World.TickInputs)
        {
            if (input is SimCheatInput cheatInput)
            {
                HandleCheat(cheatInput);
            }
        }
    }

    // Add new cheat classes here!

    public void HandleCheat(SimCheatInput cheat)
    {
        switch (cheat)
        {
            case SimInputCheatKillPlayerPawn killPlayerPawn:
            {
                Entity player = CommonReads.FindPlayerEntity(Accessor, killPlayerPawn.PlayerId);

                if (EntityManager.Exists(player) &&
                    EntityManager.TryGetComponentData(player, out ControlledEntity pawn) &&
                    EntityManager.HasComponent<Health>(pawn.Value))
                {
                    CommonWrites.SetStatInt(Accessor, pawn.Value, new Health() { Value = 0 });
                }

                break;
            }

            case SimInputCheatToggleInvincible toggleInvicible:
            {
                Entity player = CommonReads.FindPlayerEntity(Accessor, toggleInvicible.PlayerId);

                if (EntityManager.Exists(player) &&
                    EntityManager.TryGetComponentData(player, out ControlledEntity pawn))
                {
                    if (EntityManager.HasComponent<Invincible>(pawn))
                    {
                        EntityManager.RemoveComponent<Invincible>(pawn);
                    }
                    else
                    {
                        EntityManager.AddComponentData(pawn, new Invincible() { Duration = 99999 });
                    }
                }
                break;
            }

            case SimInputCheatDamagePlayer damagePlayer:
            {
                Entity player = CommonReads.FindPlayerEntity(Accessor, damagePlayer.PlayerId);

                if (EntityManager.Exists(player) &&
                    EntityManager.TryGetComponentData(player, out ControlledEntity pawn))
                {
                    if (damagePlayer.Damage > 0)
                    {
                        CommonWrites.RequestDamageOnTarget(Accessor, pawn, pawn, damagePlayer.Damage);
                    }
                    else
                    {
                        CommonWrites.RequestHealOnTarget(Accessor, pawn, pawn, -damagePlayer.Damage);
                    }
                }
                break;
            }

            case SimInputCheatAddAllItems addAllItems:
            {
                Entity player = CommonReads.FindPlayerEntity(Accessor, addAllItems.PlayerId);

                if (EntityManager.Exists(player) &&
                    EntityManager.TryGetComponentData(player, out ControlledEntity pawn))
                {
                    var allItems = EntityManager.GetBuffer<CheatsAllItemElement>(GetSingletonEntity<CheatsAllItemElement>()).ToNativeArray(Allocator.Temp);
                    NativeArray<Entity> itemInstances = new NativeArray<Entity>(allItems.Length, Allocator.Temp);

                    // Spawn items
                    for (int i = 0; i < allItems.Length; i++)
                    {
                        itemInstances[i] = EntityManager.Instantiate(allItems[i].ItemPrefab);
                    }

                    // set stacks to 999
                    for (int i = 0; i < itemInstances.Length; i++)
                    {
                        if (EntityManager.HasComponent<ItemStackableData>(itemInstances[i]))
                        {
                            EntityManager.SetComponentData(itemInstances[i], new ItemStackableData() { Value = 999 });
                        }
                    }

                    // Add item references into inventory
                    DynamicBuffer<InventoryItemReference> inventory = EntityManager.GetBuffer<InventoryItemReference>(pawn);

                    foreach (Entity itemInstance in itemInstances)
                    {
                        if (!CommonReads.IsInventoryFull(Accessor, pawn) ||
                            !CommonWrites.TryIncrementStackableItemInInventory(Accessor, pawn, itemInstance, inventory, count: 999))
                        {
                            inventory.Add(new InventoryItemReference() { ItemEntity = itemInstance });
                        }
                    }
                }
                break;
            }

            case SimInputCheatNextTurn nextTurn:
            {
                CommonWrites.RequestNextTurn(Accessor);
                break;
            }

            case SimInputCheatInfiniteAP infiniteAP:
            {
                Entity player = CommonReads.FindPlayerEntity(Accessor, infiniteAP.PlayerId);

                if (EntityManager.Exists(player) &&
                    EntityManager.TryGetComponentData(player, out ControlledEntity pawn))
                {
                    EntityManager.SetComponentData(pawn, new MaximumInt<ActionPoints>() { Value = 999999 });
                    EntityManager.SetComponentData(pawn, new ActionPoints() { Value = 999999 });
                }
                break;
            }

            case SimInputCheatTeleport teleport:
            {
                Entity player = CommonReads.FindPlayerEntity(Accessor, teleport.PlayerId);

                if (EntityManager.Exists(player) &&
                    EntityManager.TryGetComponentData(player, out ControlledEntity pawn))
                {
                    CommonWrites.RequestTeleport(Accessor, pawn, teleport.Destination);
                }
                break;
            }

            default:
                break;
        }
    }
}