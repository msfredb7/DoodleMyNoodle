﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineX.InspectorDisplay;

public class SimTeamMemberComponent : SimComponent
{
    public OLD_Team Team { get => _data.Team; set => _data.Team = value; }

    [System.Serializable]
    struct SerializedData
    {
        public OLD_Team Team;
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