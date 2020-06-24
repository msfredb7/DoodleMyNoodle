using System;
using Unity.Entities;
using UnityEngine;
using static fixMath;

public struct EntityDeathEventData : IComponentData
{
    public Entity Entity;
}

public class DeathOnEmptyHealthSystem : SimComponentSystem
{
    EntityQuery _eventsEntityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        _eventsEntityQuery = EntityManager.CreateEntityQuery(typeof(EntityDeathEventData));
    }

    protected override void OnUpdate()
    {
        // destroy events
        EntityManager.DestroyEntity(_eventsEntityQuery);

        Entities.ForEach((Entity entity, ref Health health, ref FixTranslation translation) =>
        {
            if (health.Value <= 0)
            {
                // Create Event
                EntityManager.CreateEventEntity(new EntityDeathEventData() { Entity = entity });

                // If the entity was an addon, Remove it from grid system
                Entity tile = CommonReads.GetTileEntity(Accessor, roundToInt(translation.Value).xy);
                CommonWrites.RemoveEntityOnTile(Accessor, entity, tile);

                // If the entity was a character, we need to remove that he is occupying the tile
                Entity occupiedEntity = CommonReads.GetSingleTileAddonOfType<Occupied>(Accessor, tile);
                CommonWrites.RemoveEntityOnTile(Accessor, occupiedEntity, tile);
                PostUpdateCommands.DestroyEntity(occupiedEntity);

                PostUpdateCommands.DestroyEntity(entity);
            }
        });
    }
}
