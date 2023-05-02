# Screen Flow

Screen Flow is a UI screen and popup manager, facilitating UI loading and transition (uGUI).

**Functionalities:**

- Each Screen or Popup has an exclusive class to control a visualization.
- Supports UI transitions by animation or Tween, running concurrently or waiting for the previous one, all configurable.
- Supports asset loading by Addressables and Resources, or any other method, just implement the loading strategy.
- Popups can be stacked on screen or not, configurable
- Move forward or backward in screen transition history (keeping context data), configurable.
- Allows screen transitions to be controlled by a state machine.
- Open screen or popup from triggers and pass context data to it.
- Refers to screens/popus by names with Weaver Built-in or by configs.

**Dependencies:**

- [Legendary Tools - Singleton](https://github.com/LeGustaVinho/singleton "Legendary Tools - Singleton")
- [Legendary Tools - State Machine](https://github.com/LeGustaVinho/state-machine "Legendary Tools - State Machine")
- [Legendary Tools - Asset Provider](https://github.com/LeGustaVinho/asset-provider "Legendary Tools - Asset Provider")

### How to install

#### - From OpenUPM: 

- Open **Edit -> Project Settings -> Package Manager**
- Add a new Scoped Registry (or edit the existing OpenUPM entry) 

| Name  | package.openupm.com  |
| ------------ | ------------ |
| URL  | https://package.openupm.com  |
| Scope(s)  | com.legustavinho  |

- Open Window -> Package Manager
- Click `+`
- Select `Add package from git URL...`
- Paste `com.legustavinho.legendary-tools-screen-flow` and click `Add`

#### - From Git: 
- Open **Window -> Package Manager**
- Click `+`
- Select `Add package from git URL...`
- Paste and press `Add` for all these URLs, do it in this order:
	- https://github.com/LeGustaVinho/legendary-tools-common.git
	- https://github.com/LeGustaVinho/graphs.git
	- https://github.com/LeGustaVinho/state-machine.git
	- https://github.com/LeGustaVinho/singleton.git
	- https://github.com/LeGustaVinho/asset-provider.git
	- https://github.com/LeGustaVinho/screen-flow.git

### How to use:

#### Assembly and basic configuration of the ScreenFlow system, screens and popups

1. Place the `ScreenFlow` script next to your UI canvas in the scene or prefab.
2. Create the Screen Flow configuration file, in the project, Right Click -> Tools -> Screen Flow -> ScreenFlowConfig and link to the script.
3. Create the configuration files for each screen and popup, in the project, Right Click -> Tools -> Screen Flow -> ScreenConfig or PopupConfig.
4. Link the loading strategy in the configuration files, the loading strategy already comes Built-In in Screen Flow by Resources, if you want another strategy you must implement the `AssetProvider` class.

#### Implementation of screens and popups

Scripts for Screens and Popups need to inherit respectively from the `ScreenBase` and `PopupBase` classes, for example:

```csharp
public class TestScreen : ScreenBase
{
     public Animation Animation;
    
     public override async Task Show(object args)
     {
         Animation.PlayForward(Animation.clip.name);
        
         while (Animation.isPlaying)
         {
             await Task.Delay(25);
         }
     }

     public override async Task Hide(object args)
     {
         Animation.PlayBackward(Animation.clip.name);
        
         while (Animation.isPlaying)
         {
             await Task.Delay(25);
         }
     }
}
```

#### Calling screen transitions or popup openings

Use the `ScreenFlow.Instance.SendTrigger()` method to call a screen transition or popup opening by code, you can identify the screen or popup by name or use `ScreenConfig`/`PopupConfig` by parameter.

If you want to quickly call a transition from a button click, just add the `UIScreenFlowTrigger` component and fill it in with the reference of the screen or popup you want to call.