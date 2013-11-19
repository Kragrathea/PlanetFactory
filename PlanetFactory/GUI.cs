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

        public Rect windowRect = new Rect(20, 20, 410, 250);

        void LogoWindow(int windowID)
        {
            if (logoTexture == null)
                logoTexture = PFUtil.LoadTexture(DataPath + "logo_small.png");
            if (logoTexture != null)
                GUI.DrawTexture(new Rect(4, 20, logoTexture.width, logoTexture.height), logoTexture);
            isFactoryEnabled = GUI.Toggle(new Rect(10, 20, 320, 20), isFactoryEnabled, "Load Sentar Expansion");


        }
        private void OnGUI()
        {
            if (!isTooLateToLoad)
            {
                windowRect = GUI.Window(0, windowRect, LogoWindow, "Krags Planet Factory");

                autoLoadSave = GUI.Toggle(new Rect(20, 280, 160, 20), autoLoadSave, "Auto Load Savegame->");

                int selectedItemIndex = comboBoxControl.SelectedItemIndex;
                selectedItemIndex = comboBoxControl.List(
                    new Rect(170, 280, 160, 20), comboBoxList[selectedItemIndex].text, comboBoxList, listStyle);

                autoLoadSaveName = comboBoxList[selectedItemIndex].text;

                if (GUI.changed)
                {
                    SaveConfig();
                }

            }
#if DEBUG
            if (guiHidden)
                return;

            if (FlightGlobals.currentMainBody == null)
                return;

            var curBodyName=FlightGlobals.currentMainBody.bodyName;
            if (GUI.Button(new Rect(20, 60, 100, 20), "Reload " + curBodyName))
            {
                LoadPQS(curBodyName);

                var cb = PFUtil.FindCB(curBodyName);
                if (cb)
                {
                    LoadCB(cb);
                    LoadOrbit(cb);
                }

            }
            if (GUI.Button(new Rect(20, 80, 100, 20), "Reload scaled " + curBodyName))
            {
                PFBody.LoadScaledPlanet(PFUtil.FindScaled(curBodyName), curBodyName, true);
            }
            
            if (GUI.Button(new Rect(20, 120, 100, 20), "Export " + curBodyName))
            {
                var width = 2048;
                if (FlightGlobals.currentMainBody.Radius < 50000)
                    width = 1024;
                PFExport.CreateScaledSpacePlanet(curBodyName, "Laythe", null, width, 20000);
            }
            //if (GUI.Button(new Rect(20, 140, 100, 20), "Effect " + curBodyName))
            //{
            //    PFEffects.TestEffects(PFUtil.FindScaled(curBodyName));
            //}
#endif
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