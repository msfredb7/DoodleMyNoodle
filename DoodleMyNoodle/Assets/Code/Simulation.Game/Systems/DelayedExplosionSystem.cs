using CCC.Fix2D;
using Unity.Entities;
using Unity.Mathematics;
using static fixMath;
using static Unity.Mathematics.math;

public class DelayedExplosionSystem : SimSystemBase
{
    protected override void OnUpdate()
    {

        var deltaTime = Time.DeltaTime;

        Entities.ForEach((Entity entity, ref DelayedExplosion delayedExplosion) =>
        {
            bool readyToExplode = false;

            if (delayedExplosion.UseTime)
            {
                delayedExplosion.TimeDuration -= deltaTime;

                readyToExplode = delayedExplosion.TimeDuration <= 0;
            }
            else
            {
                if (HasSingleton<NewTurnEventData>())
                {
                    delayedExplosion.TurnDuration--;

                    readyToExplode = delayedExplosion.TurnDuration <= 0;
                }
            }

            if (readyToExplode)
            {
                var entityTilePos = EntityManager.GetComponentData<FixTranslation>(entity);
                CommonWrites.RequestExplosion(Accessor, entity, entityTilePos, delayedExplosion.Radius, delayedExplosion.Damage, true);
                EntityManager.DestroyEntity(entity);
            }
        })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();
    }
}