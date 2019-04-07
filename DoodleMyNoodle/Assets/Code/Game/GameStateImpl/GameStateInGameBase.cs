﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateInGameBase : GameState
{
    public virtual void ReturnToMenu()
    {
        GameStateManager.TransitionToState(((GameStateDefinitionInGameBase)definition).gameStateIfReturn);
    }

    public virtual void ExitApplication()
    {
        Application.Quit();
    }
}