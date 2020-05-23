﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineX.InspectorDisplay;

public class SimVelocityComponent : SimComponent, ISimTickable
{
    [System.Serializable]
    struct SerializedData
    {
        public fix2 Velocity;
    }

    public fix2 Value { get => _data.Velocity; set => _data.Velocity = value; }

    public void OnSimTick()
    {
        SimTransform.LocalPosition += _data.Velocity * Simulation.DeltaTime;
    }

    #region Serialized Data Methods
    [UnityEngine.SerializeField]
    [AlwaysExpand]
    SerializedData _data = new SerializedData()
    {
        // define default values here
    };

    public override void PushToDataStack(SimComponentDataStack dataStack)
    {
        base.PushToDataStack(dataStack);
        dataStack.Push(_data);
    }

    public override void PopFromDataStack(SimComponentDataStack dataStack)
    {
        _data = (SerializedData)dataStack.Pop();
        base.PopFromDataStack(dataStack);
    }
    #endregion
}
