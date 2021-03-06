﻿using CCC.Online;
using SimulationControl;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineX;

[NetSerializable]
public struct SyncedValueCurrentLevel
{
    public string Name;
}

public class LevelManager : GameSystem<LevelManager>
{
    public SceneInfo SimManagersScene;
    public LevelBank LevelBank;

    private const string LEVEL_NOT_STARTED = "level-not-started";

    public bool IsLevelStarted { get; private set; }

    public override void OnGameStart()
    {
        base.OnGameStart();

        PresentationHelpers.PresentationWorld.GetExistingSystem<TickSimulationSystem>().PauseSimulation(LEVEL_NOT_STARTED);

        SyncedValues.RegisterValueListener<SyncedValueCurrentLevel>(OnLevelSet, callImmediatelyIfValueExits: true);

        string levelToPlay = null;

        if (SyncedValues.CanWriteValues)
        {
            switch (GameStateManager.currentGameState)
            {
                case GameStateInGameServer serverGameState:
                    levelToPlay = serverGameState.LevelToPlay;
                    break;

                case GameStateInGameLocal localGameState:
                    levelToPlay = localGameState.LevelToPlay;
                    break;

                    // CLIENT DOESN'T GET TO CHOOSE !!
            }

            SyncedValues.SetOrCreate(new SyncedValueCurrentLevel() { Name = levelToPlay });
        }
    }

    protected override void OnDestroy()
    {
        if (SyncedValues.CanWriteValues)
        {
            SyncedValues.Destroy<SyncedValueCurrentLevel>();
        }

        PresentationHelpers.PresentationWorld?.GetExistingSystem<TickSimulationSystem>()?.PauseSimulation(LEVEL_NOT_STARTED);
        SyncedValues.UnregisterValueListener<SyncedValueCurrentLevel>(OnLevelSet);

        base.OnDestroy();
    }

    public void StartLevel(string levelName)
    {
        if (!SystemReady)
        {
            Log.Error("LevelManagerSystem is not ready");
            return;
        }

        if (SyncedValues.CanWriteValues)
        {
            SyncedValues.SetOrCreate(new SyncedValueCurrentLevel() { Name = levelName });
        }
        else
        {
            Log.Error("Client cannot start a level. The server must do that.");
        }
    }

    private void OnLevelSet(SyncedValueCurrentLevel newValue)
    {
        if (!string.IsNullOrEmpty(newValue.Name))
        {
            string levelName = newValue.Name;
            Level lvl = LevelBank.Levels.Find((x) => x && x.name == levelName);
            if (!lvl)
            {
                Debug.LogError($"Could not start level {levelName}. It was not found in the level bank. " +
                    $"The bank is a scriptable object named LevelBank.");
                return;
            }

            OnLevelSet(lvl);
        }
        else
        {

            IsLevelStarted = false;
        }
    }

    private void OnLevelSet(Level level)
    {
        if (IsLevelStarted)
        {
            Log.Error("Cannot start another level (not yet implemented)");
            return;
        }
        IsLevelStarted = true;
        PresentationHelpers.PresentationWorld.GetExistingSystem<TickSimulationSystem>().UnpauseSimulation(LEVEL_NOT_STARTED);

        if (Game.PlayingAsMaster)
        {
            // load simulation scenes
            PresentationHelpers.SubmitInput(new SimCommandLoadScene() { SceneName = SimManagersScene.SceneName });
            foreach (SceneInfo scene in level.SimulationScenes)
            {
                PresentationHelpers.SubmitInput(new SimCommandLoadScene() { SceneName = scene.SceneName });
            }
        }

        // instantiate presentation
        Game.InstantiateGameplayPresentationSystems();
        
        // load presentation scenes
        foreach (SceneInfo scene in level.PresentationScenes)
        {
            SceneService.LoadAsync(scene.SceneName);
        }
    }

    public void OnLevelStopped()
    {
        if (IsLevelStarted)
            throw new System.NotImplementedException();
    }

}
