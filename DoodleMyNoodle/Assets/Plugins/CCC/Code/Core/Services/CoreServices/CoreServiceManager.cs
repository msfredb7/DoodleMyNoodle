﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityX;

public class CoreServiceManager
{
    class ServiceInitJoin : AsyncOperationJoin<ICoreService>
    {
        CoreServiceManager owner;
        public ServiceInitJoin(CoreServiceManager owner, Action onJoin) : base(onJoin)
        {
            this.owner = owner;
        }

        protected override void OnCompleteAnyInit(ICoreService obj)
        {
            owner.OnAnyServiceInitComplete(obj);
            base.OnCompleteAnyInit(obj);
        }
    }

    class InitEvent
    {
        public SafeEvent callbacks = new SafeEvent();
        public bool callbacksHaveBeenSent = false;
    }

    Dictionary<Type, ICoreService> services = new Dictionary<Type, ICoreService>();

    public static bool initializationComplete { get; private set; } = false;

    static public CoreServiceManager instance { get; private set; }

    static InitEvent onAllInitComplete = new InitEvent();
    static Dictionary<Type, InitEvent> initializationCallbackMap = new Dictionary<Type, InitEvent>();


    static public void AddInitializationCallback<T>(Action onComplete) where T : ICoreService
    {
        if (onComplete == null)
        {
            Log.Error("CoreServiceManager.AddInitializationCallback: Received a null callback.");
            return;
        }

        if (initializationComplete)
        {
            onComplete();
        }
        else
        {
            InitEvent initEvent;
            initializationCallbackMap.TryGetValue(typeof(T), out initEvent);

            if (initEvent == null) // The even is null ? Create a new one for type 'T'
            {
                initEvent = new InitEvent();
                initializationCallbackMap.Add(typeof(T), initEvent);
            }

            if (initEvent.callbacksHaveBeenSent) // if callbacks have already been sent, dont add it to the list
            {
                onComplete();
            }
            else
            {
                initEvent.callbacks += onComplete;
            }
        }
    }
    static public void RemoveInitializationCallback<T>(Action onComplete) where T : ICoreService
    {
        if(initializationComplete == false)
        {
            InitEvent safeEvent;
            if (initializationCallbackMap.TryGetValue(typeof(T), out safeEvent))
            {
                if (safeEvent.callbacksHaveBeenSent == false) // there's no point in removing the callback if they've already been sent
                {
                    safeEvent.callbacks.RemoveAction(onComplete);
                }
            }
        }
    }

    static public void AddInitializationCallback(Action onComplete)
    {
        if (onComplete == null)
        {
            Log.Error("CoreServiceManager.AddInitializationCallback: Received a null callback.");
            return;
        }

        if (initializationComplete)
        {
            onComplete();
        }
        else
        {
            onAllInitComplete.callbacks += onComplete;
        }
    }
    static public void RemoveInitializationCallback(Action onComplete)
    {
        if(onAllInitComplete != null && onAllInitComplete.callbacksHaveBeenSent == false)
        {
            onAllInitComplete.callbacks -= onComplete;
        }
    }

    public CoreServiceManager()
    {
        Debug.Assert(instance == null);
        instance = this;

        CoreServiceBank bank = CoreServiceBank.LoadBank();

        // Spawn core services
        List<ICoreService> unofficialServiceList = bank.GetCoreServices();
        foreach (ICoreService service in unofficialServiceList)
        {
            ICoreService officialInstance = service.ProvideOfficialInstance();
            services.Add(officialInstance.GetType(), officialInstance);
        }

        // Initialize them all
        ServiceInitJoin join = new ServiceInitJoin(this, OnInitializationComplete);
        foreach (KeyValuePair<Type, ICoreService> servicePair in services)
        {
            servicePair.Value.Initialize(join.RegisterOperation());
        }
        join.MarkEnd();
    }

    public T GetCoreService<T>() where T : ICoreService
    {
        return (T)services[typeof(T)];
    }

    void OnAnyServiceInitComplete(ICoreService service)
    {
        // Execute the 'on init complete' event related to THAT service
        InitEvent initEvent;
        if (initializationCallbackMap.TryGetValue(service.GetType(), out initEvent))
        {
            initEvent.callbacks.SafeInvoke();
            initEvent.callbacksHaveBeenSent = true;
        }
    }

    void OnInitializationComplete()
    {
        initializationComplete = true;

        onAllInitComplete.callbacks.SafeInvoke();
        onAllInitComplete.callbacksHaveBeenSent = true;

        //clean up
        onAllInitComplete = null;
        initializationCallbackMap = null;
    }
}
