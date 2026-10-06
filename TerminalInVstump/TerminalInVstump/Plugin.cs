using BepInEx;
using System;
using UnityEngine;

namespace TerminalInVstump
{
    /// <summary>
    /// Moves the Virtual Stump terminal into the stump, gives it a wood look,
    /// and hides the old entrance terminal and laser door.
    /// </summary>

    /* This attribute tells Utilla to look for [ModdedGameJoin] and [ModdedGameLeave] */
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        const string LocalObjects = "Environment Objects/LocalObjects_Prefab";
        const string StumpRoot = "VirtualStump_CustomMapLobby/VirtualStump_Root";
        const string TerminalPath = StumpRoot + "/VirtualStump_CustomMapsTerminal";
        const string EntrancePath = "GMM/Entrances_GMM/GMM_DestinationsEntrance";

        // how often (in seconds) to look for objects that haven't loaded yet
        const float SearchInterval = 0.5f;

        static readonly Color WoodColor = new Color(0.5518f, 0.4083f, 0.28f, 1f);

        // the terminal and its parts
        Transform terminal, monitor, controlsGroup, uiParent, promptText, buttonText;

        // objects that get hidden
        Transform laserDoor, oldTerminal;

        // wood material: copied from woodSource, applied to the targets
        Renderer woodSource, monitorRenderer, terminalMonitorRenderer, controlsStandRenderer;
        Material woodMaterial;

        bool foundEverything;
        float nextSearch;

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            FindObjects();
        }

        void LateUpdate()
        {
            // keep looking until everything has loaded, but not every single frame
            if (!foundEverything && Time.time >= nextSearch)
            {
                nextSearch = Time.time + SearchInterval;
                FindObjects();
            }

            // the game can turn these back on, so check every frame
            KeepDisabled(laserDoor);
            KeepDisabled(oldTerminal);

            if (terminal == null) return;

            PlaceObjects();
            ApplyWood();
        }

        void FindObjects()
        {
            GameObject root = GameObject.Find(LocalObjects);
            if (root == null) return;

            Transform rootTransform = root.transform;

            // these don't depend on the terminal
            if (laserDoor == null) laserDoor = rootTransform.Find(EntrancePath + "/CustomMapTunnelDoor/VirtualStump_LaserDoor_Red");
            if (oldTerminal == null) oldTerminal = rootTransform.Find(EntrancePath + "/VirtualStump_CustomMapsTerminal");

            if (terminal == null) terminal = rootTransform.Find(TerminalPath);
            if (terminal == null) return;

            if (monitor == null) monitor = terminal.Find("Monitor");
            if (controlsGroup == null) controlsGroup = terminal.Find("ControlsGroup");
            if (uiParent == null) uiParent = terminal.Find("UIParent");

            // the texts are matched by name, since their exact path is easy to get wrong
            if (promptText == null) promptText = FindByName(terminal, "TerminalControlPromptText");
            if (buttonText == null) buttonText = FindByName(terminal, "TerminalControlButton_TMP");

            // renderers for the material swap
            if (woodSource == null) woodSource = RendererOf(rootTransform.Find(StumpRoot + "/VirtualStump_GameModeSelector/StaticUnlit/VirtualStump_GameMode"));
            if (monitorRenderer == null) monitorRenderer = RendererOf(monitor);
            if (terminalMonitorRenderer == null) terminalMonitorRenderer = RendererOf(terminal.Find("ControlsGroup/TerminalMonitor"));
            if (controlsStandRenderer == null) controlsStandRenderer = RendererOf(terminal.Find("ControlsGroup/ControlsStand"));

            foundEverything = laserDoor && oldTerminal && monitor && controlsGroup && uiParent
                && promptText && buttonText && woodSource
                && monitorRenderer && terminalMonitorRenderer && controlsStandRenderer;

            if (foundEverything) Logger.LogInfo("Found everything");
        }

        /// <summary>Positions the terminal and everything attached to it.</summary>
        void PlaceObjects()
        {
            if (!terminal.gameObject.activeSelf) terminal.gameObject.SetActive(true);

            Place(terminal, new Vector3(-3.163f, 1.05f, 1.069f), new Vector3(0f, 99.1039f, 0f));
            Place(monitor, new Vector3(-0.2463f, -0.221f, 0.9763f), new Vector3(0f, 12.0196f, 0f), new Vector3(1.4742f, 1.1891f, 0.8f));
            Place(controlsGroup, new Vector3(-0.4473f, -1.081f, 1.1394f), new Vector3(315.6414f, 181.7013f, 270f));

            // the texts are children of uiParent, so it has to be placed before them
            Place(uiParent, new Vector3(-0.5334f, -0.705f, -0.2799f), new Vector3(0f, 8.2146f, 0f), Vector3.one);
            Place(promptText, new Vector3(0.6175f, 0.408f, 1.2783f), new Vector3(0f, 183.3002f, 0f), new Vector3(0.006f, 0.0071f, 1.1841f));
            Place(buttonText, new Vector3(0.2281f, -0.32f, 1.9655f), new Vector3(16.9651f, 172.8623f, 0f), new Vector3(0.0015f, 0.0023f, 0.0003f));
        }

        /// <summary>Sets local position, rotation and (optionally) scale, only touching what changed.</summary>
        static void Place(Transform target, Vector3 position, Vector3 rotation, Vector3? scale = null)
        {
            if (target == null) return;

            Quaternion localRotation = Quaternion.Euler(rotation);

            if (target.localPosition != position) target.localPosition = position;
            if (target.localRotation != localRotation) target.localRotation = localRotation;
            if (scale.HasValue && target.localScale != scale.Value) target.localScale = scale.Value;
        }

        void ApplyWood()
        {
            // make the tinted copy once; the original object keeps its own material
            if (woodMaterial == null)
            {
                if (woodSource == null || woodSource.sharedMaterial == null) return;

                woodMaterial = new Material(woodSource.sharedMaterial);

                // different shaders use different names for the main color
                if (woodMaterial.HasProperty("_Color")) woodMaterial.SetColor("_Color", WoodColor);
                if (woodMaterial.HasProperty("_BaseColor")) woodMaterial.SetColor("_BaseColor", WoodColor);
            }

            SetMaterial(monitorRenderer);
            SetMaterial(terminalMonitorRenderer);
            SetMaterial(controlsStandRenderer);
        }

        void SetMaterial(Renderer target)
        {
            if (target != null && target.sharedMaterial != woodMaterial)
                target.sharedMaterial = woodMaterial;
        }

        static void KeepDisabled(Transform target)
        {
            if (target != null && target.gameObject.activeSelf)
                target.gameObject.SetActive(false);
        }

        /// <summary>Finds a child by name, including inactive ones.</summary>
        static Transform FindByName(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains(name)) return child;
            }
            return null;
        }

        static Renderer RendererOf(Transform target)
        {
            return target != null ? target.GetComponent<Renderer>() : null;
        }
    }
}