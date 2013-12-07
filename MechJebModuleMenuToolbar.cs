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

        public MechJebModuleMenuToolbar(MechJebCore core) : base(core)
        {
            showInFlight = true;
            showInEditor = true;
            hidden = true;
            enabled = true;
        }

        private IButton mjMenuButton;

        public Dictionary<DisplayModule, IButton> toolbarButtons;

        public override void OnStart(PartModule.StartState state)
        {
            hidden = true;

            toolbarButtons = new Dictionary<DisplayModule, IButton>();
                                   
            core.GetComputerModule<MechJebModuleMenu>().hideButton = true;

            // The main MJ Button
            mjMenuButton = ToolbarManager.Instance.add("MechJeb", "0_MechJebMenu");
            mjMenuButton.TexturePath = "MechJeb2/Plugins/Icons/MJ2";
            mjMenuButton.ToolTip = "MechJeb Main Menu";
            mjMenuButton.OnClick += (b) =>
            {
                MechJebModuleMenu mjMenu = vessel.GetMasterMechJeb().GetComputerModule<MechJebModuleMenu>();
                mjMenu.ShowHideWindow();
            };
        }

        public override void OnModuleDisabled()
        {
            enabled = true;
        }

        public override void OnDestroy()
        {
            if (toolbarButtons != null)
                foreach (IButton b in toolbarButtons.Values)
                    b.Destroy();
            if (mjMenuButton != null)
                mjMenuButton.Destroy();
        }

        public static string CleanName(String name)
        {
            return name.Replace('.', '_').Replace(' ', '_').Replace(':', '_').Replace('/', '_');
        }

        public override void DrawGUI(bool inEditor)
        {
            // Remove deleted button (MechJebModuleCustomInfoWindow)
            foreach (DisplayModule d in toolbarButtons.Keys)
                if (!core.GetComputerModules<DisplayModule>().Contains(d))
                    toolbarButtons[d].Destroy();

            // No real point to keep the OrderBy for now, but this may get usefull later and is not much overhead
            // Instanciate all the button
            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>().OrderBy(m => m, DisplayOrder.instance))
            {
                String name = CleanName(module.GetName());                
                //if (!module.hidden && module.showInCurrentScene)
                if (!module.hidden)
                {
                    IButton button;                                        
                    
                    if (!toolbarButtons.ContainsKey(module))
                    {
                        button = ToolbarManager.Instance.add("MechJeb", name);
                        print("MechJebModuleMenuToolbar adding Button: " + name + " for " + module.GetType().Name);
                        toolbarButtons[module] = button;
                        button.ToolTip = "MechJeb " + module.GetName();
                        String TexturePath = "MechJeb2/Plugins/Icons/" + name;
                        if (GameDatabase.Instance.GetTexture(TexturePath, false) == null)
                        { 
                            TexturePath = "MechJeb2/Plugins/Icons/QMark";
                            print("[MechJebModuleMenuToolbar] No icon for " + name);
                        }
                        button.TexturePath = TexturePath;
                        button.Visibility = new MJButtonVisibility(module, vessel);
                        button.Visible = module.useIcon;
                        button.OnClick += (b) =>
                        {
                            module.enabled = !module.enabled;
                        };
                    }
                    else
                        button = toolbarButtons[module];

                    if (button.Visible != module.useIcon)
                        button.Visible = module.useIcon;

                    // for now this does nothing since I don't have a separate set of icon for active / inactive.

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
                        
                        // TODO : CHANGE THE ICON 
                        // button.TexturePath = 
                    }
                }
            }                       
        }

        public class MJButtonVisibility : IVisibility
        {
            private Vessel vessel;
            private DisplayModule module;

            public bool Visible
            {
                get
                {
                    return module.useIcon && module.showInCurrentScene && (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel);
                }
            }

            public MJButtonVisibility(DisplayModule module, Vessel vessel)
            {
                this.vessel = vessel;
                this.module = module;
            }
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