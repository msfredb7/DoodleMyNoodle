﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationControllerClient : SimulationController
{
    public int SimTicksInQueue => _simTicksDropper.QueueLength;
    public bool IsSyncingSimulationWithServer => _syncOp != null && _syncOp.IsRunning;
    public float CurrentSimPlayingSpeed => _simTicksDropper.Speed;


    SimulationSyncClientOperation _syncOp;
    SessionClientInterface _session;
    SelfRegulatingDropper<NetMessageSimTick> _simTicksDropper;

    // used while we are in sync process
    List<NetMessageSimTick> _shelvedSimTicks = new List<NetMessageSimTick>();

    protected override void Awake()
    {
        base.Awake();

        _simTicksDropper = new SelfRegulatingDropper<NetMessageSimTick>(
            maximalCatchUpSpeed: GameConstants.CLIENT_SIM_TICK_MAX_CATCH_UP_SPEED,
            maximalExpectedTimeInQueue: GameConstants.CLIENT_SIM_TICK_MAX_EXPECTED_TIME_IN_QUEUE);
        Debug.Log($"Client tick dropper -  maximalCatchUpSpeed: {GameConstants.CLIENT_SIM_TICK_MAX_CATCH_UP_SPEED}   maximalExpectedTimeInQueue: {GameConstants.CLIENT_SIM_TICK_MAX_EXPECTED_TIME_IN_QUEUE}");
    }

    public override void OnGameReady()
    {
        base.OnGameReady();

        _session = OnlineService.clientInterface.SessionClientInterface;
        _session.RegisterNetMessageReceiver<NetMessageSimTick>(OnNetMessageSimTick);

#if DEBUG_BUILD
        GameConsole.AddCommand("sim.sync", Cmd_SimSync, "Sync the simulation with the server");
#endif
    }

    public override void OnGameStart()
    {
        base.OnGameStart();

        SyncSimulationWithServer();
    }

    public override void OnSafeDestroy()
    {
#if DEBUG_BUILD
        GameConsole.RemoveCommand("sim.sync");
#endif

        base.OnSafeDestroy();

        _session?.UnregisterNetMessageReceiver<NetMessageSimTick>(OnNetMessageSimTick);
        _session = null;
    }

    public override void SubmitInput(SimInput input)
    {
        if (input == null)
        {
            DebugService.LogError("Trying to submit a null input");
            return;
        }

        if (_syncOp != null && _syncOp.IsRunning)
        {
            DebugService.Log("Discarding input since we are syncing to the simulation");
            return;
        }

        _session.SendNetMessageToServer(new NetMessageInputSubmission()
        {
            submissionId = InputSubmissionId.Generate(),
            input = input
        });
    }

    void OnNetMessageSimTick(NetMessageSimTick tick, INetworkInterfaceConnection source)
    {
        if (_syncOp != null
            && _syncOp.IsRunning)
        {
            // if we receive ticks while we're syncing, shelve the tick so we can restored it later
            _shelvedSimTicks.Add(tick);
            return;
        }

        // The server has sent a tick message
        _simTicksDropper.Enqueue(tick, (float)SimulationConstants.TIME_STEP);
    }

    private void FixedUpdate()
    {
        if (!SimulationView.IsRunningOrReadyToRun)
            return;

        _simTicksDropper.Update(Time.fixedDeltaTime);

        while (CanTickSimulation && _simTicksDropper.TryDrop(out NetMessageSimTick tick))
        {
            if (SimulationView.TickId != tick.tickId)
            {
                DebugService.LogError($"We forcefully set the next simulation's tick at {tick.tickId}" +
                    $" (from {SimulationView.TickId}) to match with the server. This should not happen if 'join in progress' works correctly");
                SimulationView.ForceSetTickId(tick.tickId);
            }
            ExecuteTick(tick);
        }
    }

    void ExecuteTick(NetMessageSimTick tick)
    {
        SimInput[] simInputs = new SimInput[tick.inputs.Length];
        for (int i = 0; i < tick.inputs.Length; i++)
        {
            simInputs[i] = tick.inputs[i].input;
        }

        SimTickData tickData = new SimTickData()
        {
            inputs = simInputs
        };

        SimulationView.Tick(tickData);
    }

    SimulationSyncClientOperation SyncSimulationWithServer()
    {
        if(IsSyncingSimulationWithServer)
        {
            DebugService.LogWarning("Trying to start a SimSync process while we are already in one");
            return null;
        }

        PauseSimulation();

        _syncOp = new SimulationSyncClientOperation(_session);

        _syncOp.OnTerminateCallback = (op) =>
        {
            // restore ticks we received while syncing
            _simTicksDropper.Clear();
            for (int i = 0; i < _shelvedSimTicks.Count; i++)
            {
                if (_shelvedSimTicks[i].tickId >= SimulationView.TickId)
                {
                    _simTicksDropper.Enqueue(_shelvedSimTicks[i], (float)SimulationConstants.TIME_STEP);
                }
            }

            UnpauseSimulation();
        };

        _syncOp.OnSucceedCallback = (op) =>
        {
            DebugScreenMessage.DisplayMessage($"Synced sim. {op.Message}");
        };

        _syncOp.OnFailCallback = (op) =>
        {
            DebugScreenMessage.DisplayMessage($"Failed to sync sim. {op.Message}");
        };

        _syncOp.Execute();

        return _syncOp;
    }

#if DEBUG_BUILD
    private void Cmd_SimSync(string[] obj)
    {
        if (_ongoingCmdOperation != null && _ongoingCmdOperation.IsRunning)
        {
            Debug.LogWarning("Cannot sync sim because another operation is ongoing");
            return;
        }

        _ongoingCmdOperation = SyncSimulationWithServer();
    }
#endif
}
