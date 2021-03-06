﻿using Unity.Mathematics;
using UnityEngineX;
using static fixMath;
using System.Collections.Generic;
using UnityEngine;
using CCC.Fix2D;
using System;
using Unity.MathematicsX;

public class GameActionMeleeAttack : GameAction
{
    public override Type[] GetRequiredSettingTypes() => new Type[]
    {
        typeof(GameActionSettingDamage),
        typeof(GameActionSettingRange),
    };

    public override UseContract GetUseContract(ISimWorldReadAccessor accessor, in UseContext context)
    {
        var range = accessor.GetComponent<GameActionSettingRange>(context.Item);
        GameActionParameterPosition.Description tileParam = new GameActionParameterPosition.Description()
        {
            MaxRangeFromInstigator = range.Value
        };

        return new UseContract(tileParam);
    }

    public override bool Use(ISimWorldReadWriteAccessor accessor, in UseContext context, UseParameters useData, ref ResultData resultData)
    {
        if (useData.TryGetParameter(0, out GameActionParameterPosition.Data paramPosition))
        {
            fix2 instigatorPos = accessor.GetComponent<FixTranslation>(context.InstigatorPawn);
            fix range = accessor.GetComponent<GameActionSettingRange>(context.Item);
            int damage = accessor.GetComponent<GameActionSettingDamage>(context.Item);

            fix2 attackPosition = Helpers.ClampPositionInsideRange(paramPosition.Position, instigatorPos, range);
            fix attackRadius = (fix)0.1f;

            var hits = CommonReads.Physics.OverlapCircle(accessor, attackPosition, attackRadius, ignoreEntity: context.InstigatorPawn);
            CommonWrites.RequestDamage(accessor, context.InstigatorPawn, hits, damage);

            fix2 attackVector = attackPosition - instigatorPos;
            resultData.AddData(new KeyValuePair<string, object>("Direction", attackVector));

            return true;
        }

        return false;
    }
}