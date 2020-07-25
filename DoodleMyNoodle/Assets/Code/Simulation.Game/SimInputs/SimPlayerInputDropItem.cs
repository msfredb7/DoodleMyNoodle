﻿using System;
using Unity.Entities;
using UnityEngine;
using UnityEngineX;

public class SimPlayerInputDropItem : SimPlayerInput
{
    public int ItemIndex;

    public SimPlayerInputDropItem() { }

    public SimPlayerInputDropItem(int ItemIndex)
    {
        this.ItemIndex = ItemIndex;
    }

    public override string ToString()
    {
        return $"SimPlayerInputDropItem(player:{SimPlayerId.Value}, ItemIndex:{ItemIndex})";
    }
}

public class PawnInputDropItem : PawnControllerInputBase
{
    public int ItemIndex;

    public PawnInputDropItem(Entity pawnController) : base(pawnController) { }

    public PawnInputDropItem(Entity pawnController, int ItemIndex) : base(pawnController)
    {
        this.ItemIndex = ItemIndex;
    }
}