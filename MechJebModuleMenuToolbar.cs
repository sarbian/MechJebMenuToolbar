using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toolbar;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    /// Description of MechJebModuleMenuToolbar.
    /// </summary>
    public class MechJebModuleMenuToolbar : DisplayModule
    {

        public MechJebModuleMenuToolbar(MechJebCore core)
            : base(core)
        {
            priority = -1000;
            enabled = true;
            hidden = true;
            showInFlight = false;
            showInEditor = false;
        }

        Dictionary<DisplayModule, IButton> toolbarButtons;

        public override void OnStart(PartModule.StartState state)
        {
            core.GetComputerModule<MechJebModuleMenu>().enabled = false;
            toolbarButtons = new Dictionary<DisplayModule, IButton>();
        }

        public override void OnModuleDisabled()
        {
            enabled = true;
        }

        public override void OnDestroy()
        {
            print("MechJebModuleMenuToolbar OnDestroy");
            if (toolbarButtons!=null)
                foreach (IButton b in toolbarButtons.Values)
                    b.Visible = false;
        }

        public override void DrawGUI(bool inEditor)
        {
            // Remove deleted button
            if (toolbarButtons.Count > core.GetComputerModules<DisplayModule>().Count)
                foreach (DisplayModule d in toolbarButtons.Keys)
                    if (!core.GetComputerModules<DisplayModule>().Contains(d))
                        // here we should have code to remove button for deleted MechJebModuleCustomInfoWindow
                        toolbarButtons[d].Visible=false;

            // No real point to keep the OrderBy for now, but this may get usefull later and is not much overhead
            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>().OrderBy(m => m, DisplayOrder.instance))
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    IButton button;
                    if (!toolbarButtons.ContainsKey(module))
                    {
                        String name = module.GetName().Replace('.', '_').Replace(' ', '_').Replace(':', '_').Replace('/', '_');
                        button = ToolbarManager.Instance.add("MechJeb", name);
                        print("MechJebModuleMenuToolbar adding Button: " + name + " for " + module.GetType().Name);
                        toolbarButtons[module] = button;
                        button.Text = module.GetName();
                        button.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.FLIGHT);
                        button.OnClick += (b) =>
                        {
                            module.enabled = !module.enabled;
                        };
                    }
                    else
                        button = toolbarButtons[module];

                    // Rajouter un test pour !module.hidden && module.showInCurrentScene
                    // en utilisant le button5.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH);
                    // Avec uene version custom qui prend les 2 en compte

                    button.Visible = !module.hidden && module.showInCurrentScene;

                    if (button.Visible)
                    {
                        // This display module is considered active if it uses any of these modules.
                        ComputerModule[] makesActive = { core.attitude, core.thrust, core.rover, core.node, core.rcs, core.rcsbal };

                        bool active = false;
                        foreach (var m in makesActive)
                        {
                            if (m != null)
                            {
                                if (active |= m.users.RecursiveUser(module)) break;
                            }
                        }
                        if (module is MechJebModuleWarpHelper && ((MechJebModuleWarpHelper)module).warping) active = true;
                        if (module is MechJebModuleThrustWindow && core.thrust.limiter != MechJebModuleThrustController.LimitMode.None) active = true;

                        button.TextColor = active ? Color.green : Color.white;
                    }
                }
            }            
        }

        public override string GetName()
        {
            return "MechJebModuleMenuToolbar";
        }


        class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder() { }
            public static DisplayOrder instance = new DisplayOrder();

            int IComparer<DisplayModule>.Compare(DisplayModule a, DisplayModule b)
            {
                if (a is MechJebModuleCustomInfoWindow && b is MechJebModuleCustomInfoWindow) return a.GetName().CompareTo(b.GetName());
                if (a is MechJebModuleCustomInfoWindow) return 1;
                if (b is MechJebModuleCustomInfoWindow) return -1;
                return a.GetName().CompareTo(b.GetName());
            }
        }
    }
}