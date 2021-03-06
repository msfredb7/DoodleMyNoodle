﻿using CCC.Fix2D;
using Unity.Entities;

[NetSerializable(IsBaseClass = true)]
public abstract class SimPlayerInput : SimInput
{
    // this will be assigned by the server when its about to enter the simulation
    public PersistentId SimPlayerId;

    public override string ToString()
    {
        return $"{GetType().Name}(player:{SimPlayerId.Value})";
    }
}

[UpdateBefore(typeof(ExecutePawnControllerInputSystem))]
[UpdateInGroup(typeof(InputSystemGroup))]
public class ExecutePlayerInputSystem : SimSystemBase
{
    protected override void OnUpdate()
    {
        foreach (var input in World.TickInputs)
        {
            if (input is SimPlayerInput playerInput)
            {
                Entity playerEntity = CommonReads.FindPlayerEntity(Accessor, playerInput.SimPlayerId);
                ExecutePlayerInput(playerInput, playerEntity);
            }
        }
    }

    private void ExecutePlayerInput(SimPlayerInput input, Entity playerEntity)
    {
        // fbessette: For now, we simply do a switch. 
        //            In the future, we'll probably want to implement something dynamic instead
        ExecutePawnControllerInputSystem pawnControllerInputSystem = World.GetOrCreateSystem<ExecutePawnControllerInputSystem>();
        switch (input)
        {
            case SimPlayerInputSetPawnName setNameInput:
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputSetPawnName(playerEntity, setNameInput.Name));
                break;

            case SimPlayerInputNextTurn nextTurnInput:
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputNextTurn(playerEntity, nextTurnInput.ReadyForNextTurn));
                break;

            case SimPlayerInputUseItem useItemInput:
            {
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputUseItem(playerEntity, useItemInput.ItemIndex, useItemInput.UseData));
                break;
            }

            case SimPlayerInputClickSignalEmitter clickSignalEmitter:
            {
                Entity emitter = clickSignalEmitter.Emitter;
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputClickSignalEmitter(playerEntity, emitter));
                break;
            }

            case SimPlayerInputUseObjectGameAction useGameActionInput:
            {
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputUseObjectGameAction(playerEntity, useGameActionInput.ObjectPosition, useGameActionInput.UseData));
                break;
            }
            case SimPlayerInputSetPawnDoodle setPawnDoodleInput:
            {
                Entity pawn = GetPlayerPawn(playerEntity);
                if (pawn != Entity.Null)
                {
                    EntityManager.SetOrAddComponentData(pawn, new DoodleId() { Guid = setPawnDoodleInput.DoodleId });
                    EntityManager.SetOrAddComponentData(pawn, new DoodleStartDirection() { IsLookingRight = setPawnDoodleInput.DoodleDirectionIsLookingRight });
                }
                break;
            }
            case SimPlayerInputEquipItem equipItemInput:
            {
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputEquipItem(playerEntity, equipItemInput.ItemIndex, equipItemInput.ChestEntity));
                break;
            }
            case SimPlayerInputDropItem dropItemInput:
            {
                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputDropItem(playerEntity, dropItemInput.ItemIndex));
                break;
            }
            case SimPlayerInputMove moveInput:
            {
                if (HasComponent<Team>(playerEntity))
                {
                    Entity pawn = GetPlayerPawn(playerEntity);
                    if (HasComponent<MoveInput>(pawn))
                    {
                        SetComponent<MoveInput>(pawn, moveInput.NewDirection);
                    }
                }

                break;
            }
            case SimPlayerInputJump jumpInput:
            {
                if (HasComponent<Team>(playerEntity))
                {
                    if (CommonReads.CanTeamPlay(Accessor, GetComponent<Team>(playerEntity)))
                    {
                        Entity pawn = GetPlayerPawn(playerEntity);
                        if (EntityManager.HasComponent<InventoryItemReference>(pawn))
                        {
                            CommonReads.FindFirstItemWithGameAction<GameActionBasicJump>(Accessor, pawn, out int itemIndex);

                            if (itemIndex != -1)
                            {
                                pawnControllerInputSystem.Inputs.Add(new PawnControllerInputUseItem(playerEntity, itemIndex, new GameAction.UseParameters()));
                            }
                        }
                    }
                }

                break;
            }

        }
    }

    private Entity GetPlayerPawn(Entity playerEntity)
    {
        if (EntityManager.TryGetComponentData(playerEntity, out ControlledEntity controlledEntity))
        {
            Entity pawn = controlledEntity.Value;

            if (EntityManager.Exists(pawn))
            {
                return pawn;
            }
        }

        return Entity.Null;
    }
}

public partial class CommonReads
{
    public static Entity FindPlayerEntity(ISimWorldReadAccessor readAccessor, PersistentId simPlayerId)
    {
        Entity result = Entity.Null;
        readAccessor.Entities
                    .WithAll<PlayerTag>()
                    .ForEach((Entity controller, ref PersistentId id) =>
                    {
                        if (id.Value == simPlayerId.Value)
                        {
                            result = controller;
                            return;
                        }
                    });

        return result;
    }
}