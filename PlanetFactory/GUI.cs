using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using KSP.IO;

namespace PlanetFactory
{

    public partial class PlanetFactory : MonoBehaviour
    {

        GUIContent[] comboBoxList;
        private ComboBox comboBoxControl = new ComboBox();
        private GUIStyle listStyle = new GUIStyle();

        private void InitComboBox()
        {
            comboBoxList = saveNames.Select(x=>new GUIContent(x)).ToArray();

            comboBoxControl.SelectedItemIndex = saveNames.FindIndex(x=>x==autoLoadSaveName);
            listStyle.normal.textColor = Color.white;
            listStyle.onHover.background =
            listStyle.hover.background = new Texture2D(2, 2);
            listStyle.hover.textColor = Color.blue;
            listStyle.padding.left =
            listStyle.padding.right =
            listStyle.padding.top =
            listStyle.padding.bottom = 4;
        }

        private Texture2D logoTexture;

        public Rect logoWindowRect = new Rect(20, 40, 410, 250);
        public Rect utilWindowRect = new Rect(20, 40, 210, 20);
        public Rect planetWindowRect = new Rect(300, 20, 210, 20);

        void LogoWindow(int windowID)
        {
            if (logoTexture == null)
                logoTexture = PFUtil.LoadTexture(DataPath + "logo_small.png");
            if (logoTexture != null)
                GUI.DrawTexture(new Rect(4, 20, logoTexture.width, logoTexture.height), logoTexture);

            var yOff = 20;
            isFactoryEnabled = GUI.Toggle(new Rect(10, yOff, 320, 20), isFactoryEnabled, "Load Sentar Expansion");
            yOff += 20;
            //isSystemEnabled = GUI.Toggle(new Rect(10, yOff, 320, 20), isSystemEnabled, "Load System CFG");
            //yOff += 20;
            foreach (var s in PlanetFactory.Instance.Systems)
            {
                s.enabled = GUI.Toggle(new Rect(10, yOff, 320, 20), s.enabled, string.Format("Load {0} System", s.name));
                yOff += 20;
            }
            if (GUI.changed)
            {
                SaveConfig();
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
        private void oldOnGUI()
        {
            if (!isTooLateToLoad)
            {
                logoWindowRect = GUI.Window(0, logoWindowRect, LogoWindow, "Krags Planet Factory");

                //autoLoadSave = GUI.Toggle(new Rect(20, 280, 160, 20), autoLoadSave, "Auto Load Savegame->");

                //int selectedItemIndex = comboBoxControl.SelectedItemIndex;
                //selectedItemIndex = comboBoxControl.List(
                //    new Rect(170, 280, 160, 20), comboBoxList[selectedItemIndex].text, comboBoxList, listStyle);

                //autoLoadSaveName = comboBoxList[selectedItemIndex].text;

                if (GUI.changed)
                {
                    SaveConfig();
                }

            }

            if (guiHidden)
                return;

            if (FlightGlobals.currentMainBody == null)
                return;
            try
            {
                var curBodyName = FlightGlobals.currentMainBody.bodyName;

                if (GUI.Button(new Rect(20, 60, 100, 20), "Reload " + curBodyName))
                {
                    var pfBody = PFUtil.FindPFBody(curBodyName);
                    PlanetFactory.SetPathFor(pfBody);

                    LoadPQS(curBodyName);

                    var cb = PFUtil.FindCB(curBodyName);
                    if (cb)
                    {
                        LoadCB(cb);
                        LoadOrbit(cb);
                    }
                    PlanetFactory.SetPathFor(null);
                }
                if (GUI.Button(new Rect(20, 80, 100, 20), "Reload scaled " + curBodyName))
                {
                    var pfBody = PFUtil.FindPFBody(curBodyName);
                    PlanetFactory.SetPathFor(pfBody);
                    PFBody.LoadScaledPlanet(PFUtil.FindScaled(curBodyName), curBodyName, true);
                    PlanetFactory.SetPathFor(null);
                }

                if (GUI.Button(new Rect(20, 120, 100, 20), "Export " + curBodyName))
                {
                    var width = 2048;
                    if (FlightGlobals.currentMainBody.Radius < 50000)
                        width = 1024;

                    var pfBody = PFUtil.FindPFBody(curBodyName);
                    PlanetFactory.SetPathFor(pfBody);
                    PFExport.CreateScaledSpacePlanet(curBodyName, pfBody.templateName, null, width, 20000);
                    PlanetFactory.SetPathFor(null);

                }
                if (GUI.Button(new Rect(20, 140, 100, 20), "Test Effects" + curBodyName))
                {
                    PFEffects.TestEffects(PFUtil.FindScaled(curBodyName));
                }
            }
            finally
            {
                PlanetFactory.SetPathFor(null);
            }
        }

        void OnGUI()
        {
            if (!isTooLateToLoad)
            {
                logoWindowRect = GUI.Window(0, logoWindowRect, LogoWindow, "Krags Planet Factory");

                if (GUI.changed)
                {
                    SaveConfig();
                }

            }
            if (guiHidden)
                return;
            utilWindowRect = GUILayout.Window(2378942, utilWindowRect, UtilWindow, "PlanetFactory");

            if(showPlanetWindow && FlightGlobals.ActiveVessel!=null)
                planetWindowRect = GUILayout.Window(5533442, planetWindowRect, PlanetWindow, "Go To");


        }
        bool showPlanetWindow = false;
        bool showPlanetPicker = false;
        float altitudeScalar = 0.3f;
        void PlanetWindow(int windowID)
        {


            GUILayout.BeginVertical();
            GUILayout.Label("Altitude");
            altitudeScalar = GUILayout.HorizontalSlider(altitudeScalar, 0.1f, 3.95f);

            if (GUI.changed)
            {
                var body = PFUtil.FindCB(FlightGlobals.currentMainBody.name);
                var oldOrbit = FlightGlobals.ActiveVessel.orbitDriver.orbit;
                //var newOrbit =new Orbit(oldOrbit.inclination,oldOrbit.E, body.Radius + (body.Radius * altitudeScalar)
                //    ,oldOrbit.LAN, 0,oldOrbit.epoch,0,oldOrbit.referenceBody);

                //PFUtil.HardsetOrbit(FlightGlobals.ActiveVessel.orbitDriver.orbit, newOrbit);

                //var body = PFUtil.FindCB(planet.name);
                var orbit = new Orbit(oldOrbit.inclination, oldOrbit.E, body.Radius + (body.Radius * altitudeScalar), 0, 0, oldOrbit.meanAnomalyAtEpoch, 0, body);

                PFUtil.WarpShip(FlightGlobals.ActiveVessel, orbit);

            }
            if (GUI.Button(new Rect(3, 3, 20, 20), "X"))
            {
                showPlanetWindow = false;
                planetWindowRect.height = 20;
            }

            
            if (GUILayout.Button("Planet Picker"))
            {
                showPlanetPicker = !showPlanetPicker;
                planetWindowRect.height = 20;
            }
            if (showPlanetPicker)
            {
                foreach (var planet in newBodies)
                {
                    if (GUILayout.Button(planet.name))
                    {
                        PFUtil.Log(planet.name);

                        var body = PFUtil.FindCB(planet.name);
                        var orbit = new Orbit(0, 0, body.Radius + (body.Radius * altitudeScalar), 0, 0, 0, 0, body);

                        PFUtil.WarpShip(FlightGlobals.ActiveVessel, orbit);

                        showPlanetPicker = false;
                        planetWindowRect.height = 20;
                        //Set(CreateOrbit(0, 0, body.Radius + (body.Radius / 3), 0, 0, 0, 0, body));

                    }
                }

            }
            GUILayout.EndVertical();
            GUI.contentColor = Color.white;

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
        void UtilWindow(int windowID)
        {
            if (GUI.Button(new Rect(3, 3, 20, 20), "X"))
            {
                guiHidden = true;
            }

            GUILayout.BeginVertical();

            try
            {
                if (GUILayout.Button("Toggle Log"))
                {
                    DebugConsole.show = !DebugConsole.show;
                }
                if (FlightGlobals.ActiveVessel!=null)
                {
                    if (GUILayout.Button("Go To..."))
                    {
                        showPlanetWindow = !showPlanetWindow;
                    }
                }

                if (FlightGlobals.currentMainBody != null && newBodies.Any(x=>x.name==currentBodyName))
                {
                    var curBodyName = FlightGlobals.currentMainBody.bodyName;

                    GUILayout.Label("Current Planet:" + curBodyName);
                    if (GUILayout.Button("Reload CFG"))
                    {
                        var pfBody = PFUtil.FindPFBody(curBodyName);
                        PlanetFactory.SetPathFor(pfBody);

                        PlanetFactory.LoadPQS(curBodyName);

                        var cb = PFUtil.FindCB(curBodyName);
                        if (cb)
                        {
                            PlanetFactory.LoadCB(cb);
                            PlanetFactory.LoadOrbit(cb);
                        }
                        PlanetFactory.SetPathFor(null);
                    }
                    //if (GUILayout.Button("Reload scaled " + curBodyName))
                    //{
                    //    var pfBody = PFUtil.FindPFBody(curBodyName);
                    //    PlanetFactory.SetPathFor(pfBody);
                    //    PlanetFactory.PFBody.LoadScaledPlanet(PFUtil.FindScaled(curBodyName), curBodyName, true);
                    //    PlanetFactory.SetPathFor(null);
                    //}

                    if (GUILayout.Button("Create Scaled"))
                    {
                        var width = 2048;
                        if (FlightGlobals.currentMainBody.Radius < 50000)
                            width = 1024;

                        var pfBody = PFUtil.FindPFBody(curBodyName);
                        PlanetFactory.SetPathFor(pfBody);
                        PFExport.CreateScaledSpacePlanet(curBodyName, pfBody.templateName, null, width, 20000);
                    PlanetFactory.PFBody.LoadScaledPlanet(PFUtil.FindScaled(curBodyName), curBodyName, true);
                        PlanetFactory.SetPathFor(null);

                    }


                    //if (comboBoxControl == null)
                    //    InitComboBox();


                }
            }
            finally
            {
                PlanetFactory.SetPathFor(null);
            }

            GUILayout.EndVertical();

            GUI.contentColor = Color.white;

            //GUILayout.EndScrollView();

            //if (comboBoxControl != null)
            //{
            //    var selectedItemIndex = comboBoxControl.SelectedItemIndex;
            //    selectedItemIndex = comboBoxControl.List(
            //        new Rect(20, 40, 160, 20), comboBoxList[selectedItemIndex].text, comboBoxList, listStyle);
            //}

            // Set the window to be draggable by the top title bar
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

        }
   
    }



    // Popup list created by Eric Haines
    // ComboBox Extended by Hyungseok Seo.(Jerry) sdragoon@nate.com
    // 

    public class ComboBox
    {
        private static bool forceToUnShow = false;
        private static int useControlID = -1;
        private bool isClickedComboButton = false;

        private int selectedItemIndex = 0;

        public int List(Rect rect, string buttonText, GUIContent[] listContent, GUIStyle listStyle)
        {
            return List(rect, new GUIContent(buttonText), listContent, "button", "box", listStyle);
        }

        public int List(Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle)
        {
            return List(rect, buttonContent, listContent, "button", "box", listStyle);
        }

        public int List(Rect rect, string buttonText, GUIContent[] listContent, GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
        {
            return List(rect, new GUIContent(buttonText), listContent, buttonStyle, boxStyle, listStyle);
        }

        public int List(Rect rect, GUIContent buttonContent, GUIContent[] listContent,
                                        GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
        {
            if (forceToUnShow)
            {
                forceToUnShow = false;
                isClickedComboButton = false;
            }

            bool done = false;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.mouseUp:
                    {
                        if (isClickedComboButton)
                        {
                            done = true;
                        }
                    }
                    break;
            }

            if (GUI.Button(rect, buttonContent, buttonStyle))
            {
                if (useControlID == -1)
                {
                    useControlID = controlID;
                    isClickedComboButton = false;
                }

                if (useControlID != controlID)
                {
                    forceToUnShow = true;
                    useControlID = controlID;
                }
                isClickedComboButton = true;
            }

            if (isClickedComboButton)
            {
                var listRect = new Rect(rect.x, rect.y + listStyle.CalcHeight(listContent[0], 1.0f),
                          rect.width, listStyle.CalcHeight(listContent[0], 1.0f) * listContent.Length);

                GUI.Box(listRect, "", boxStyle);
                int newSelectedItemIndex = GUI.SelectionGrid(listRect, selectedItemIndex, listContent, 1, listStyle);
                if (newSelectedItemIndex != selectedItemIndex)
                {
                    selectedItemIndex = newSelectedItemIndex;
                    GUI.changed = true;
                }
            }

            if (done)
                isClickedComboButton = false;

            return SelectedItemIndex;
        }

        public int SelectedItemIndex
        {
            get
            {
                return selectedItemIndex;
            }
            set {
                selectedItemIndex = value < 0 ? 0 : value;
            }
        }
    }
    


}