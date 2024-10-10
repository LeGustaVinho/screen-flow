using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM

#endif

namespace LegendaryTools.Systems.ScreenFlow
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class ScreenFlow : 
        
#if SCREEN_FLOW_SINGLETON
        SingletonBehaviour<ScreenFlow>
#else
        MonoBehaviour
#endif
        , IScreenFlow
    {
        public bool AutoInitializeOnStart;
        
        public ScreenFlowConfig Config;
        public ScreenConfig StartScreen;

        [Header("UI Entities In Scene")] 
        public List<ScreenInScene> ScreensInScene = new List<ScreenInScene>();
        public List<PopupInScene> PopupsInScene = new List<PopupInScene>();

        public bool IsTransiting => screenTransitionRoutine != null || 
                                    popupTransitionRoutine != null || 
                                    transitionRoutine != null;

        public bool IsPreloading => preloadRoutine != null;

        public ScreenConfig CurrentScreenConfig =>
            ScreensHistory.Count > 0 ? ScreensHistory[ScreensHistory.Count - 1].Entity : null;

        public IScreenBase CurrentScreenInstance { private set; get; }

        public PopupConfig CurrentPopupConfig =>
            PopupConfigsStack.Count > 0 ? PopupConfigsStack[PopupConfigsStack.Count - 1] : null;

        public IPopupBase CurrentPopupInstance =>
            PopupInstancesStack.Count > 0 ? PopupInstancesStack[PopupInstancesStack.Count - 1] : null;

        public List<IPopupBase> CurrentPopupInstancesStack => new List<IPopupBase>(PopupInstancesStack);

        public int PopupStackCount => PopupInstancesStack.Count;

        public event Action<ScreenConfig, ScreenConfig> OnScreenChange;
        public event Action<PopupConfig, PopupConfig> OnPopupOpen;

        protected readonly List<UIEntityBaseConfig> PreloadQueue = new List<UIEntityBaseConfig>();
        protected readonly List<EntityArgPair<ScreenConfig>> ScreensHistory = new List<EntityArgPair<ScreenConfig>>();
        protected readonly List<PopupConfig> PopupConfigsStack = new List<PopupConfig>();
        protected readonly List<IPopupBase> PopupInstancesStack = new List<IPopupBase>();
        protected readonly Dictionary<IPopupBase, Canvas> AllocatedPopupCanvas = new Dictionary<IPopupBase, Canvas>();
        protected readonly List<Canvas> AvailablePopupCanvas = new List<Canvas>();
        
        private readonly List<ScreenFlowCommand> commandQueue = new List<ScreenFlowCommand>();
        private readonly Dictionary<string, UIEntityBaseConfig> uiEntitiesLookup =
            new Dictionary<string, UIEntityBaseConfig>();

        private Coroutine preloadRoutine;
        private Coroutine screenTransitionRoutine;
        private Coroutine popupTransitionRoutine;
        private Coroutine nextScreenLoading;
        private Coroutine newPopupLoading;
        private Coroutine hideScreenRoutine;
        private Coroutine showScreenRoutine;
        private Coroutine hidePopupRoutine;
        private Coroutine showPopupRoutine;
        private Coroutine transitionRoutine;

        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;

        public void SendTrigger(string name, System.Object args = null, bool enqueue = true, 
            Action<IScreenBase> requestedScreenOnShow = null, Action<IScreenBase> previousScreenOnHide = null)
        {
            if (uiEntitiesLookup.TryGetValue(name, out UIEntityBaseConfig uiEntityBaseConfig))
            {
                SendTrigger(uiEntityBaseConfig, args, enqueue, requestedScreenOnShow, previousScreenOnHide);
            }
        }

        public void SendTrigger(UIEntityBaseConfig uiEntity, System.Object args = null, bool enqueue = true, 
            Action<IScreenBase> requestedScreenOnShow = null, Action<IScreenBase> previousScreenOnHide = null)
        {
            ScreenFlowCommand command = new ScreenFlowCommand(ScreenFlowCommandType.Trigger, uiEntity, args, requestedScreenOnShow, previousScreenOnHide);
            if (!IsTransiting)
            {
                commandQueue.Add(command);
                transitionRoutine ??= StartCoroutine(ProcessCommandQueue());
            }
            else
            {
                if (enqueue)
                {
                    commandQueue.Add(command);
                }
            }
        }

        public void SendTriggerT<TConfig, TShow, THide>(TConfig uiEntity, System.Object args = null, bool enqueue = true,
            Action<TShow> requestedScreenOnShow = null, Action<THide> previousScreenOnHide = null)
            where TConfig : UIEntityBaseConfig
            where TShow : class, IScreenBase
            where THide : class, IScreenBase
        {
            void RequestedScreenOnShowDual(IScreenBase screenBase)
            {
                requestedScreenOnShow?.Invoke(screenBase as TShow);
            }
            
            void PreviousScreenOnHideDual(IScreenBase screenBase)
            {
                previousScreenOnHide?.Invoke(screenBase as THide);
            }
            
            SendTrigger(uiEntity, args , enqueue, RequestedScreenOnShowDual, PreviousScreenOnHideDual);
        }

        public void MoveBack(System.Object args = null, bool enqueue = true, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null)
        {
            ScreenFlowCommand command = new ScreenFlowCommand(ScreenFlowCommandType.MoveBack, null, args, onShow, onHide);
            if (!IsTransiting)
            {
                commandQueue.Add(command);
                transitionRoutine ??= StartCoroutine(ProcessCommandQueue());
            }
            else
            {
                if (enqueue)
                {
                    commandQueue.Add(command);
                }
            }
        }

        public void CloseForegroundPopup(System.Object args = null, bool enqueue = true, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null)
        {
            if (CurrentPopupInstance != null)
            {
                ClosePopup(CurrentPopupInstance, args, enqueue, onShow, onHide);
            }
        }

        public void ClosePopup(IPopupBase popupBase, System.Object args = null, bool enqueue = true, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null)
        {
            var command = new ScreenFlowCommand(ScreenFlowCommandType.ClosePopup, popupBase, args, onShow, onHide);
            if (!IsTransiting)
            {
                commandQueue.Add(command);
                transitionRoutine ??= StartCoroutine(ProcessCommandQueue());
            }
            else
            {
                if (enqueue)
                {
                    commandQueue.Add(command);
                }
            }
        }
#if SCREEN_FLOW_SINGLETON
        protected override void Awake()
        {
            base.Awake();

            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
        }
#else
        protected void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
        }
#endif

#if SCREEN_FLOW_SINGLETON
        protected override void Start()
        {
            base.Start();
            if(AutoInitializeOnStart)
                Initialize();
        }
#else
        protected void Start()
        {
            if(AutoInitializeOnStart)
                Initialize();
        }
#endif
        IEnumerator PreLoadRoutine()
        {
            yield return Preload();
            preloadRoutine = null;
        }

        protected virtual void Update()
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ProcessBackKey();
            }
            #endif
        }

        [Sirenix.OdinInspector.Button]
        public void Initialize()
        {
            if (Config == null)
            {
                Debug.LogError("[ScreenFlow:Start] -> Config is null");
                return;
            }
            
            uiEntitiesLookup.Clear();
            foreach (ScreenConfig screenConfig in Config.Screens)
            {
                if (!uiEntitiesLookup.ContainsKey(screenConfig.name))
                {
                    uiEntitiesLookup.Add(screenConfig.name, screenConfig);    
                }
                else
                {
                    Debug.LogError("[ScreenFlow:Start()] -> UI Entity " + screenConfig.name + " already exists in ScreenFlow");
                }
            }

            foreach (PopupConfig popupConfig in Config.Popups)
            {
                if (!uiEntitiesLookup.ContainsKey(popupConfig.name))
                {
                    uiEntitiesLookup.Add(popupConfig.name, popupConfig);
                }
                else
                {
                    Debug.LogError("[ScreenFlow:Start()] -> UI Entity " + popupConfig.name + " already exists in ScreenFlow");
                }
            }
            
            foreach (ScreenInScene screenInScene in ScreensInScene)
            {
                if (!uiEntitiesLookup.ContainsKey(screenInScene.Config.name))
                {
                    uiEntitiesLookup.Add(screenInScene.Config.name, screenInScene.Config);
                    screenInScene.Config.AssetLoaderConfig.SetAsSceneAsset(screenInScene.ScreenInstance);
                }
                else
                {
                    Debug.LogError("[ScreenFlow:Start()] -> UI Entity " + screenInScene.Config.name + " already exists in ScreenFlow");
                }
            }

            foreach (PopupInScene popupInScene in PopupsInScene)
            {
                if (!uiEntitiesLookup.ContainsKey(popupInScene.Config.name))
                {
                    uiEntitiesLookup.Add(popupInScene.Config.name, popupInScene.Config);
                    popupInScene.Config.AssetLoaderConfig.SetAsSceneAsset(popupInScene.PopupInstance);
                }
                else
                {
                    Debug.LogError("[ScreenFlow:Start()] -> UI Entity " + popupInScene.Config.name + " already exists in ScreenFlow");
                }
            }
            
            if (StartScreen != null)
            {
                SendTrigger(StartScreen);
            }
            
            preloadRoutine = StartCoroutine(PreLoadRoutine());
        }

        private IEnumerator ProcessCommandQueue()
        {
            while (commandQueue.Count > 0)
            {
                ScreenFlowCommand next = commandQueue[0];
                commandQueue.RemoveAt(0);

                switch (next.Type)
                {
                    case ScreenFlowCommandType.Trigger:
                    {
                        if (next.Object is ScreenConfig screenConfig)
                        {
                            yield return ScreenTransitTo(screenConfig, false, next.Args, next.OnShow, next.OnHide);
                        }
                        else if (next.Object is PopupConfig popupConfig)
                        {
                            if (CurrentScreenConfig != null)
                            {
                                if (CurrentScreenConfig.AllowPopups)
                                {
                                    yield return PopupTransitTo(popupConfig, next.Args, next.OnShow, next.OnHide);
                                }
                            }
                        }
                        break;
                    }
                    case ScreenFlowCommandType.MoveBack:
                    {
                        yield return MoveBackOp(next.Args, next.OnShow, next.OnHide);
                        break;
                    }
                    case ScreenFlowCommandType.ClosePopup:
                    {
                        yield return ClosePopupOp(next.Object as IPopupBase, next.Args, next.OnShow, next.OnHide);
                        break;
                    }
                }
            }

            transitionRoutine = null;
        }

        private IEnumerator MoveBackOp(System.Object args, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null)
        {
            EntityArgPair<ScreenConfig> previousScreenConfig = ScreensHistory.Count > 1 ? ScreensHistory[ScreensHistory.Count - 2] : null;
            if (previousScreenConfig != null)
            {
                if (CurrentScreenConfig.CanMoveBackFromHere && previousScreenConfig.Entity.CanMoveBackToHere)
                {
                    yield return ScreenTransitTo(previousScreenConfig.Entity, 
                        true, args ?? previousScreenConfig.Args, onShow, onHide);
                }
            }
        }
        
        private IEnumerator ClosePopupOp(IPopupBase popupBase, System.Object args, Action<IScreenBase> onShow = null, 
            Action<IScreenBase> onHide = null)
        {
            int stackIndex = PopupInstancesStack.FindIndex(item => item == popupBase);

            if (stackIndex >= 0)
            {
                bool isTopOfStack = stackIndex == PopupInstancesStack.Count - 1;
                PopupConfig popupConfig = PopupConfigsStack[stackIndex];

                PopupConfig behindPopupConfig = null;
                IPopupBase behindPopupInstance = null;

                if (stackIndex - 1 >= 0)
                {
                    behindPopupConfig = PopupConfigsStack[stackIndex - 1];
                    behindPopupInstance = PopupInstancesStack[stackIndex - 1];
                }

                if (isTopOfStack)
                {
                    switch (popupConfig.AnimationType)
                    {
                        case AnimationType.NoAnimation:
                        case AnimationType.Wait:
                        {
                            //Wait for hide's animation to complete
                            yield return popupBase.RequestHide(args);
                            onHide?.Invoke(CurrentPopupInstance);
                            DisposePopupFromHide(popupConfig, popupBase);
                            break;
                        }
                        case AnimationType.Intersection:
                        {
                            hidePopupRoutine = StartCoroutine(popupBase.RequestHide(args));
                            break;
                        }
                    }

                    if (behindPopupInstance != null && 
                        (behindPopupConfig.GoingBackgroundBehaviour == PopupGoingBackgroundBehaviour.JustHide ||
                         behindPopupConfig.GoingBackgroundBehaviour == PopupGoingBackgroundBehaviour.HideAndDestroy))
                    {
                        switch (popupConfig.AnimationType)
                        {
                            case AnimationType.NoAnimation:
                            case AnimationType.Wait:
                            {
                                //Wait for shows's animation to complete
                                yield return behindPopupInstance.Show(args);
                                break;
                            }
                            case AnimationType.Intersection:
                            {
                                //Show animation starts playing (may be playing in parallel with hide's animation)
                                showPopupRoutine = StartCoroutine(behindPopupInstance.Show(args));
                                break;
                            }
                        }
                    }

                    if (hidePopupRoutine != null) //If we were waiting for hide's animation
                    {
                        yield return hidePopupRoutine; //Wait for hide's animation to complete
                        onHide?.Invoke(CurrentPopupInstance);
                        DisposePopupFromHide(CurrentPopupConfig, CurrentPopupInstance);
                        hidePopupRoutine = null;
                    }

                    if (showPopupRoutine != null) //If we were waiting for show's animation
                    {
                        yield return showPopupRoutine; //Wait for show's animation to complete
                        showPopupRoutine = null;
                    }
                }
                else
                {
                    onHide?.Invoke(popupBase);
                    DisposePopupFromHide(popupConfig, popupBase);
                }
                
                onShow?.Invoke(behindPopupInstance);
            }

            popupTransitionRoutine = null;
        }

        private IEnumerator Preload()
        {
            PreloadQueue.Clear();
            
            ScreenConfig[] screens = Array.FindAll(Config.Screens, item => item.AssetLoaderConfig.PreLoad);
            
            if(screens.Length > 0)
                PreloadQueue.AddRange(screens);

            PopupConfig[] popups = Array.FindAll(Config.Popups, item => item.AssetLoaderConfig.PreLoad);
            
            if(popups.Length > 0)
                PreloadQueue.AddRange(Array.FindAll(Config.Popups, item => item.AssetLoaderConfig.PreLoad));

            yield return PreloadingAssets();
        }

        private IEnumerator PreloadingAssets()
        {
            foreach (UIEntityBaseConfig uiEntityBaseConfig in PreloadQueue)
            {
                uiEntityBaseConfig.AssetLoaderConfig.PrepareLoadRoutine<ScreenBase>();
                yield return uiEntityBaseConfig.AssetLoaderConfig.WaitLoadRoutine();
            }
        }

        private IEnumerator ScreenTransitTo(ScreenConfig screenConfig, bool isMoveBack = false,
            System.Object args = null, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null)
        {
            if (!screenConfig.AssetLoaderConfig.IsLoaded)
            {
                if (!screenConfig.AssetLoaderConfig.IsLoading) //Prevents loading, because the asset is being loaded in the preload routine 
                {
                    if (PreloadQueue.Contains(screenConfig)) //Check if it is in the preloading queue 
                    {
                        PreloadQueue.Remove(screenConfig);
                    }

                    //Schedule the new screen to load in the background
                    screenConfig.AssetLoaderConfig.PrepareLoadRoutine<GameObject>();
                    nextScreenLoading = StartCoroutine(screenConfig.AssetLoaderConfig.WaitLoadRoutine());
                }
            }

            yield return HandlePopupsOnScreenTransit(args);

            ScreenConfig oldScreenConfig = CurrentScreenConfig;
            if (CurrentScreenConfig != null)
            {
                switch (CurrentScreenConfig.AnimationType)
                {
                    case AnimationType.NoAnimation:
                    case AnimationType.Wait:
                    {
                        //Wait for hide's animation to complete
                        yield return CurrentScreenInstance.RequestHide(args);
                        
                        onHide?.Invoke(CurrentScreenInstance);

                        if (CurrentScreenConfig.AssetLoaderConfig.IsInScene)
                        {
                            //Screen is serialized on scene, no need to destroy or unload, so just set it disabled
                            CurrentScreenInstance.GameObject.SetActive(false);
                        }
                        else
                        {
                            Destroy(CurrentScreenInstance.GameObject);

                            if (!CurrentScreenConfig.AssetLoaderConfig.DontUnloadAfterLoad)
                            {
                                CurrentScreenConfig.AssetLoaderConfig.Unload();
                            }
                        }

                        break;
                    }
                    case AnimationType.Intersection:
                    {
                        //Hide animation starts playing 
                        hideScreenRoutine = StartCoroutine(CurrentScreenInstance.RequestHide(args));
                        break;
                    }
                }
            }

            if (nextScreenLoading != null)
            {
                yield return nextScreenLoading; //Wait for the new screen to load completely if it hasn't loaded yet
                nextScreenLoading = null;
            }
            else if (!screenConfig.AssetLoaderConfig.IsLoaded) //The asset is probably still being loaded in the preload routine.
            {
                //Waits for the asset to be fully loaded into the preload routine 
                while (!screenConfig.AssetLoaderConfig.IsLoaded)
                {
                    yield return null;
                }
            }

            if (screenConfig.AssetLoaderConfig.LoadedAsset == null)
            {
                Debug.LogError(
                    $"[ScreenFlow:ScreenTransitTo()] -> Failed to load {screenConfig.AssetLoaderConfig.name}",
                    screenConfig);
                yield break;
            }

            ScreenBase newScreenPrefab = null;
            switch (screenConfig.AssetLoaderConfig.LoadedAsset)
            {
                case GameObject screenGameObject:
                {
                    newScreenPrefab = screenGameObject.GetComponent<ScreenBase>();
                    break;
                }
                case ScreenBase screenBase : newScreenPrefab = screenBase; break;
            }

            if (newScreenPrefab == null)
            {
                Debug.LogError($"[ScreenFlow:ScreenTransitTo()] -> {screenConfig.AssetLoaderConfig.LoadedAsset.GetType()} doesn't have any component that inherits from ScreenBase class", screenConfig);
                yield break;
            }
            
            ScreenBase newScreenInstance;
            if (screenConfig.AssetLoaderConfig.IsInScene)
            {
                newScreenInstance = newScreenPrefab;
                newScreenInstance.gameObject.SetActive(true);
            }
            else
            {
                newScreenInstance = InstantiateUIElement<ScreenBase>(newScreenPrefab,
                    rectTransform, out RectTransform instanceRT, out RectTransform prefabRT);
            }

            switch (screenConfig.AnimationType)
            {
                case AnimationType.NoAnimation:
                case AnimationType.Wait:
                {
                    //Wait for shows's animation to complete
                    yield return newScreenInstance.Show(args);
                    break;
                }
                case AnimationType.Intersection:
                {
                    //Show animation starts playing (may be playing in parallel with hide's animation)
                    showScreenRoutine = StartCoroutine(newScreenInstance.Show(args));
                    break;
                }
            }

            if (hideScreenRoutine != null) //If we were waiting for hide's animation
            {
                yield return hideScreenRoutine; //Wait for hide's animation to complete
                onHide?.Invoke(CurrentScreenInstance);
                
                if (CurrentScreenConfig.AssetLoaderConfig.IsInScene)
                {
                    //Screen is serialized on scene, no need to destroy or unload, so just set it disabled
                    CurrentScreenInstance.GameObject.SetActive(false); 
                }
                else
                {
                    Destroy(CurrentScreenInstance.GameObject);

                    if (!CurrentScreenConfig.AssetLoaderConfig.DontUnloadAfterLoad)
                    {
                        CurrentScreenConfig.AssetLoaderConfig.Unload();
                    }
                }
                hideScreenRoutine = null;
            }
            
            if (showScreenRoutine != null) //If we were waiting for show's animation
            {
                yield return showScreenRoutine; //Wait for show's animation to complete
                showScreenRoutine = null;
            }

            //Update to new state
            CurrentScreenInstance = newScreenInstance;
            if (isMoveBack)
            {
                ScreensHistory.RemoveAt(ScreensHistory.Count - 1);
            }
            else
            {
                ScreensHistory.Add(new EntityArgPair<ScreenConfig>(screenConfig, args));
            }

            foreach (IPopupBase popup in PopupInstancesStack)
            {
                popup.ParentScreen = CurrentScreenInstance;
            }

            screenTransitionRoutine = null;
            onShow?.Invoke(newScreenInstance);
            OnScreenChange?.Invoke(oldScreenConfig, screenConfig);
        }

        private IEnumerator PopupTransitTo(PopupConfig popupConfig, System.Object args = null, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null)
        {
            if (!popupConfig.AssetLoaderConfig.IsLoaded)
            {
                popupConfig.AssetLoaderConfig.PrepareLoadRoutine<GameObject>();
                newPopupLoading = StartCoroutine(popupConfig.AssetLoaderConfig.WaitLoadRoutine());
            }

            PopupConfig oldPopupConfig = CurrentPopupConfig;
            if (CurrentPopupConfig != null)
            {
                switch (CurrentPopupConfig.AnimationType)
                {
                    case AnimationType.NoAnimation:
                    case AnimationType.Wait:
                    {
                        if (CurrentScreenConfig.AllowStackablePopups)
                        {
                            onHide?.Invoke(CurrentPopupInstance);
                            CurrentPopupInstance.GoToBackground(args);

                            if (CurrentPopupConfig.GoingBackgroundBehaviour != PopupGoingBackgroundBehaviour.DontHide)
                            {
                                yield return CurrentPopupInstance.RequestHide(args);
                                onHide?.Invoke(CurrentPopupInstance);

                                if (CurrentPopupConfig.GoingBackgroundBehaviour ==
                                    PopupGoingBackgroundBehaviour.HideAndDestroy)
                                {
                                    DisposePopupFromHide(CurrentPopupConfig, CurrentPopupInstance, CurrentPopupConfig == popupConfig);
                                }
                            }
                        }
                        else
                        {
                            //Wait for hide's animation to complete
                            yield return CurrentPopupInstance.RequestHide(args);
                            onHide?.Invoke(CurrentPopupInstance);
                            DisposePopupFromHide(CurrentPopupConfig, CurrentPopupInstance, CurrentPopupConfig == popupConfig);
                        }

                        break;
                    }
                    case AnimationType.Intersection:
                    {
                        if (CurrentScreenConfig.AllowStackablePopups)
                        {
                            CurrentPopupInstance.GoToBackground(args);

                            if (CurrentPopupConfig.GoingBackgroundBehaviour != PopupGoingBackgroundBehaviour.DontHide)
                            {
                                //Hide animation starts playing 
                                hidePopupRoutine = StartCoroutine(CurrentPopupInstance.RequestHide(args));
                            }
                        }
                        else
                        {
                            //Hide animation starts playing 
                            hidePopupRoutine = StartCoroutine(CurrentPopupInstance.RequestHide(args));
                        }

                        break;
                    }
                }
            }

            yield return newPopupLoading; //Wait for the new popup to load completely if it hasn't loaded yet
            newPopupLoading = null;

            if (popupConfig.AssetLoaderConfig.LoadedAsset == null)
            {
                Debug.LogError($"[ScreenFlow:PopupTransitTo()] -> Failed to load {popupConfig.AssetLoaderConfig.name}", popupConfig);
                yield break;
            }
            
            ScreenBase newPopupPrefab = null;
            switch (popupConfig.AssetLoaderConfig.LoadedAsset)
            {
                case GameObject screenGameObject:
                {
                    newPopupPrefab = screenGameObject.GetComponent<ScreenBase>();
                    break;
                }
                case PopupBase popupBase : {newPopupPrefab = popupBase; break;}
                case ScreenBase screenBase : newPopupPrefab = screenBase; break;
            }

            if (newPopupPrefab is not IPopupBase newPopupImpl)
            {
                Debug.LogError($"[ScreenFlow:PopupTransitTo()] -> {popupConfig.AssetLoaderConfig.LoadedAsset.GetType()} doesn't have any component that inherits from {nameof(IPopupBase)} interface", popupConfig);
                yield break;
            }
            
            Canvas canvasPopup = null;
            if (popupConfig.AssetLoaderConfig.IsInScene)
            {
                canvasPopup = GetComponentInParent<Canvas>();
                if (canvasPopup == null)
                {
                    canvasPopup = GetComponentInChildren<Canvas>();
                }
                newPopupPrefab.GameObject.SetActive(true);
            }
            else
            {
                canvasPopup = GetCanvas(newPopupPrefab); //Check if the prefab popup has any canvas, if it does, we don't need to instantiate a new canvas
                
                if (canvasPopup != null)
                {
                    //Instantiate the popup from the prefab (and it already has canvas =D) 
                    newPopupImpl = InstantiateUIElement(newPopupPrefab, null,
                        out RectTransform instanceRT, out RectTransform prefabRT) as IPopupBase;
                }
                else
                {
                    //Instantiate the popup from the prefab
                    newPopupImpl = InstantiateUIElement(newPopupPrefab as ScreenBase, null,
                        out RectTransform instanceRT, out RectTransform prefabRT) as IPopupBase;

                    //Instantiate new canvas to hold popup
                    canvasPopup = AllocatePopupCanvas(newPopupImpl);

                    //Parent popup to canvas
                    ReparentUIElement(instanceRT, prefabRT, canvasPopup.transform);
                }
            }

            newPopupImpl.ParentScreen = CurrentScreenInstance;

            //Change the order of the canvas, so that it is always above screen canvas
            CalculatePopupCanvasSortOrder(canvasPopup, CurrentPopupInstance);

            switch (popupConfig.AnimationType)
            {
                case AnimationType.NoAnimation:
                case AnimationType.Wait:
                {
                    //Wait for shows's animation to complete
                    yield return newPopupImpl.Show(args);
                    break;
                }
                case AnimationType.Intersection:
                {
                    //Show animation starts playing (may be playing in parallel with hide's animation)
                    showPopupRoutine = StartCoroutine(newPopupImpl.Show(args));
                    break;
                }
            }

            if (hidePopupRoutine != null) //If we were waiting for hide's animation
            {
                yield return hidePopupRoutine; //Wait for hide's animation to complete
                onHide?.Invoke(CurrentPopupInstance);

                if (CurrentPopupConfig.GoingBackgroundBehaviour ==
                    PopupGoingBackgroundBehaviour.HideAndDestroy)
                {
                    DisposePopupFromHide(CurrentPopupConfig, CurrentPopupInstance);
                }
                
                hidePopupRoutine = null;
            }

            if (showPopupRoutine != null) //If we were waiting for show's animation
            {
                yield return showPopupRoutine; //Wait for show's animation to complete
                showPopupRoutine = null;
            }
            
            newPopupImpl.OnClosePopupRequest += OnClosePopupRequest;

            //Update to new state
            PopupConfigsStack.Add(popupConfig);
            PopupInstancesStack.Add(newPopupImpl);
            
            popupTransitionRoutine = null;
            onShow?.Invoke(newPopupImpl);
            OnPopupOpen?.Invoke(oldPopupConfig, popupConfig);
        }

        private void OnClosePopupRequest(IPopupBase popupToClose)
        {
            ClosePopup(popupToClose);
        }

        private IEnumerator HandlePopupsOnScreenTransit(System.Object args = null)
        {
            if (CurrentPopupInstance != null)
            {
                switch (CurrentScreenConfig.PopupBehaviourOnScreenTransition)
                {
                    case PopupsBehaviourOnScreenTransition.PreserveAllOnHide:
                    {
                        //Just keep going
                        break;
                    }
                    case PopupsBehaviourOnScreenTransition.HideFirstThenTransit:
                    {
                        yield return ClosePopupOp(CurrentPopupInstance, args);
                        for (int i = PopupConfigsStack.Count - 1; i >= 0; i--)
                        {
                            DisposePopupFromHide(PopupConfigsStack[i], PopupInstancesStack[i]);
                        }

                        break;
                    }
                    case PopupsBehaviourOnScreenTransition.DestroyAllThenTransit:
                    {
                        for (int i = PopupConfigsStack.Count - 1; i >= 0; i--)
                        {
                            DisposePopupFromHide(PopupConfigsStack[i], PopupInstancesStack[i]);
                        }

                        break;
                    }
                }
            }
        }

        private void DisposePopupFromHide(PopupConfig popupConfig, IPopupBase popupInstance, bool forceDontUnload = false)
        {
            //Remove current popup from stack
            PopupConfigsStack.Remove(popupConfig);
            PopupInstancesStack.Remove(popupInstance);
            RecyclePopupCanvas(popupInstance);

            popupInstance.OnClosePopupRequest -= OnClosePopupRequest;

            if (popupConfig.AssetLoaderConfig.IsInScene)
            {
                popupInstance.GameObject.SetActive(false);
            }
            else
            {
                Destroy(popupInstance.GameObject);

                if (!popupConfig.AssetLoaderConfig.DontUnloadAfterLoad)
                {
                    if (forceDontUnload == false)
                    {
                        popupConfig.AssetLoaderConfig.Unload();
                    }
                }
            }
        }

        private T InstantiateUIElement<T>(T screenPrefab, Transform parent, out RectTransform elementInstanceRT,
            out RectTransform elementPrefabRT)
            where T : ScreenBase
        {
            T elementInstance = Instantiate(screenPrefab);
            elementInstanceRT = elementInstance.GetComponent<RectTransform>();
            elementPrefabRT = screenPrefab.GetComponent<RectTransform>();

            ReparentUIElement(elementInstanceRT, elementPrefabRT, parent);

            return elementInstance;
        }

        private void ReparentUIElement(RectTransform elementInstanceRT, RectTransform elementPrefabRT, Transform parent)
        {
            elementInstanceRT.SetParent(parent);
            elementInstanceRT.SetAsLastSibling();
            elementInstanceRT.localPosition = elementPrefabRT.position;
            elementInstanceRT.localRotation = elementPrefabRT.rotation;
            elementInstanceRT.localScale = elementPrefabRT.localScale;

            elementInstanceRT.anchoredPosition3D = elementPrefabRT.anchoredPosition3D;
            elementInstanceRT.anchorMin = elementPrefabRT.anchorMin;
            elementInstanceRT.anchorMax = elementPrefabRT.anchorMax;
            elementInstanceRT.sizeDelta = elementPrefabRT.sizeDelta;
            elementInstanceRT.offsetMin = elementPrefabRT.offsetMin;
            elementInstanceRT.offsetMax = elementPrefabRT.offsetMax;
        }

        private Canvas GetCanvas(IScreenBase popupBase)
        {
            Canvas canvasPopup = popupBase.GetComponentInParent<Canvas>();
            return canvasPopup;
        }

        private Canvas AllocatePopupCanvas(IPopupBase popupInstance)
        {
            Canvas availableCanvas = null;
            if (AvailablePopupCanvas.Count > 0)
            {
                availableCanvas = AvailablePopupCanvas[AvailablePopupCanvas.Count - 1];
                AvailablePopupCanvas.RemoveAt(AvailablePopupCanvas.Count - 1);
            }
            else
            {
                availableCanvas = CreatePopupCanvas();
            }

            AllocatedPopupCanvas.Add(popupInstance, availableCanvas);
            availableCanvas.gameObject.SetActive(true);
            return availableCanvas;
        }

        private void RecyclePopupCanvas(IPopupBase popupInstance)
        {
            if (AllocatedPopupCanvas.TryGetValue(popupInstance, out Canvas popupCanvas))
            {
                AllocatedPopupCanvas.Remove(popupInstance);
                AvailablePopupCanvas.Add(popupCanvas);
                popupCanvas.gameObject.SetActive(false);
            }
        }

        private Canvas CreatePopupCanvas()
        {
            if (Config.OverridePopupCanvasPrefab != null)
            {
                Canvas canvasPopupOverride = Instantiate(Config.OverridePopupCanvasPrefab);
                DontDestroyOnLoad(canvasPopupOverride); //Canvas are persistent because they can be reused 
                return canvasPopupOverride;
            }

            GameObject canvasPopupGo = new GameObject("[Canvas] - Popup");
            DontDestroyOnLoad(canvasPopupGo); //Canvas are persistent because they can be reused 

            Canvas canvasPopup = canvasPopupGo.AddComponent<Canvas>();
            CanvasScaler canvasScalerPopup = canvasPopupGo.AddComponent<CanvasScaler>();
            GraphicRaycaster graphicRaycasterPopup = canvasPopupGo.AddComponent<GraphicRaycaster>();

            //Copies the settings from the screen's canvas to the popup's canvas
            canvasPopup.renderMode = canvas.renderMode;
            canvasPopup.pixelPerfect = canvas.pixelPerfect;
            canvasPopup.sortingLayerID = canvas.sortingLayerID;
            canvasPopup.sortingOrder = canvas.sortingOrder;
            canvasPopup.targetDisplay = canvas.targetDisplay;
            canvasPopup.renderMode = canvas.renderMode;
            canvasPopup.additionalShaderChannels = canvas.additionalShaderChannels;
            canvasPopup.worldCamera = canvas.worldCamera;

            //Copies the settings from the screen's canvasScaler to the popup's canvasScaler
            canvasScalerPopup.uiScaleMode = canvasScaler.uiScaleMode;
            canvasScalerPopup.referenceResolution = canvasScaler.referenceResolution;
            canvasScalerPopup.screenMatchMode = canvasScaler.screenMatchMode;
            canvasScalerPopup.matchWidthOrHeight = canvasScaler.matchWidthOrHeight;
            canvasScalerPopup.referencePixelsPerUnit = canvasScaler.referencePixelsPerUnit;
            canvasScalerPopup.scaleFactor = canvasScaler.scaleFactor;
            canvasScalerPopup.physicalUnit = canvasScaler.physicalUnit;
            canvasScalerPopup.fallbackScreenDPI = canvasScaler.fallbackScreenDPI;
            canvasScalerPopup.defaultSpriteDPI = canvasScaler.defaultSpriteDPI;

            //Copies the settings from the screen's graphicRaycaster to the popup's graphicRaycaster
            graphicRaycasterPopup.ignoreReversedGraphics = graphicRaycaster.ignoreReversedGraphics;
            graphicRaycasterPopup.blockingObjects = graphicRaycaster.blockingObjects;

            return canvasPopup;
        }

        private void CalculatePopupCanvasSortOrder(Canvas canvasPopup, IPopupBase topOfStackPopupInstance)
        {
            if (canvasPopup != null)
            {
                if (topOfStackPopupInstance == null)
                {
                    canvasPopup.sortingOrder = canvas.sortingOrder + 1;
                }
                else
                {
                    if (AllocatedPopupCanvas.TryGetValue(topOfStackPopupInstance, out Canvas currentPopupCanvas))
                    {
                        canvasPopup.sortingOrder = currentPopupCanvas.sortingOrder + 1;
                    }
                }

                canvasPopup.name = "[Canvas] - Popup #" + canvasPopup.sortingOrder;
            }
        }

        private void ProcessBackKey()
        {
            if (CurrentScreenConfig != null)
            {
                //check if the screen is overriding config back key behaviour
                if (CurrentScreenInstance.BackKeyBehaviourOverride == BackKeyBehaviourOverride.Inherit)
                {
                    switch (CurrentScreenConfig.BackKeyBehaviour)
                    {
                        case BackKeyBehaviour.NotAllowed: return;
                        case BackKeyBehaviour.ScreenMoveBack:
                            MoveBack();
                            break;
                        case BackKeyBehaviour.CloseFirstPopup:
                        {
                            if (CurrentPopupInstance != null)
                            {
                                CloseForegroundPopup();
                            }
                            else
                            {
                                MoveBack();
                            }

                            break;
                        }
                    }
                }
                else
                {
                    switch (CurrentScreenInstance.BackKeyBehaviourOverride)
                    {
                        case BackKeyBehaviourOverride.NotAllowed: return;
                        case BackKeyBehaviourOverride.ScreenMoveBack:
                            MoveBack();
                            break;
                        case BackKeyBehaviourOverride.CloseFirstPopup:
                        {
                            if (CurrentPopupInstance != null)
                            {
                                CloseForegroundPopup();
                            }
                            else
                            {
                                MoveBack();
                            }

                            break;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}