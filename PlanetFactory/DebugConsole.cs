using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace PlanetFactory
{
    public class DebugConsole : MonoBehaviour
    {

        struct ConsoleMessage
        {
            public readonly string message;
            //public readonly string stackTrace;
            public readonly LogType type;

            public ConsoleMessage(string message, LogType type)
            {
                this.message = message;
                //this.stackTrace = stackTrace;
                this.type = type;
            }
        }

        public KeyCode toggleKey = KeyCode.BackQuote;

        static List<ConsoleMessage> entries = new List<ConsoleMessage>();
        static Vector2 scrollPos;
        public static bool show;
        bool collapse;

        // Visual elements:

        GUIContent[] comboBoxList;
        private ComboBox comboBoxControl = null;
        private GUIStyle listStyle = new GUIStyle();

        private void InitComboBox()
        {
            comboBoxList = PlanetFactory.Instance.newBodies.Select(x => new GUIContent(x.name)).ToArray();

            //comboBoxControl.SelectedItemIndex = PlanetFactory.Instance.newBodies.FindIndex(x => x.name == curPlanetName);
            listStyle.normal.textColor = Color.white;
            listStyle.onHover.background =
            listStyle.hover.background = new Texture2D(2, 2);
            listStyle.hover.textColor = Color.blue;
            listStyle.padding.left =
            listStyle.padding.right =
            listStyle.padding.top =
            listStyle.padding.bottom = 4;
            comboBoxControl = new ComboBox();
        }

        const int margin = 15;
        Rect windowRect = new Rect((Screen.width / 2)-margin, margin, Screen.width / 2, Screen.height / 2);

        GUIContent clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
        GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");

        //void OnEnable() { Application.RegisterLogCallback(HandleLog); }
        //void OnDisable() { Application.RegisterLogCallback(null); }

        static void ScrollToEnd()
        {
            scrollPos =new Vector2(0,9999999);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                show = !show;
            }
        }

        void OnGUI()
        {
            if (!show)
            {
                return;
            }

            windowRect = GUILayout.Window(123456, windowRect, ConsoleWindow, "PlanetFactory Log");
        }

        /// <summary>
        /// A window displaying the logged messages.
        /// </summary>
        /// <param name="windowID">The window's ID.</param>
        void ConsoleWindow(int windowID)
        {

//            GUILayout.BeginHorizontal();

//            // Clear button
//            //if (GUILayout.Button(clearLabel))
//            //{
//            //    entries.Clear();
//            //}
//            // Collapse toggle
//            //collapse = GUILayout.Toggle(collapse, collapseLabel, GUILayout.ExpandWidth(false));

////                return;
//            try
//            {
//                if (FlightGlobals.currentMainBody != null)
//                {
//                    var curBodyName = FlightGlobals.currentMainBody.bodyName;

//                    if (GUILayout.Button("Reload " + curBodyName))
//                    {
//                        var pfBody = PFUtil.FindPFBody(curBodyName);
//                        PlanetFactory.SetPathFor(pfBody);

//                        PlanetFactory.LoadPQS(curBodyName);

//                        var cb = PFUtil.FindCB(curBodyName);
//                        if (cb)
//                        {
//                            PlanetFactory.LoadCB(cb);
//                            PlanetFactory.LoadOrbit(cb);
//                        }
//                        PlanetFactory.SetPathFor(null);
//                    }
//                    if (GUILayout.Button("Reload scaled " + curBodyName))
//                    {
//                        var pfBody = PFUtil.FindPFBody(curBodyName);
//                        PlanetFactory.SetPathFor(pfBody);
//                        PlanetFactory.PFBody.LoadScaledPlanet(PFUtil.FindScaled(curBodyName), curBodyName, true);
//                        PlanetFactory.SetPathFor(null);
//                    }

//                    if (GUILayout.Button("Export " + curBodyName))
//                    {
//                        var width = 2048;
//                        if (FlightGlobals.currentMainBody.Radius < 50000)
//                            width = 1024;

//                        var pfBody = PFUtil.FindPFBody(curBodyName);
//                        PlanetFactory.SetPathFor(pfBody);
//                        PFExport.CreateScaledSpacePlanet(curBodyName, pfBody.templateName, null, width, 20000);
//                        PlanetFactory.SetPathFor(null);

//                    }


//                    //if (comboBoxControl == null)
//                    //    InitComboBox();


//                }
//            }
//            finally
//            {
//                PlanetFactory.SetPathFor(null);
//            }


//            GUILayout.EndHorizontal();

            if (GUI.Button(new Rect(3, 3, 20, 20), "X"))
            {
                show = false;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            // Go through each logged entry
            for (int i = 0; i < entries.Count; i++)
            {
                ConsoleMessage entry = entries[i];

                // If this message is the same as the last one and the collapse feature is chosen, skip it
                if (collapse && i > 0 && entry.message == entries[i - 1].message)
                {
                    continue;
                }

                // Change the text colour according to the log type
                switch (entry.type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        GUI.contentColor = Color.red;
                        break;

                    case LogType.Warning:
                        GUI.contentColor = Color.yellow;
                        break;

                    default:
                        GUI.contentColor = Color.white;
                        break;
                }

                GUILayout.Label(entry.message,GUILayout.MaxHeight(15));
            }

            GUI.contentColor = Color.white;

            GUILayout.EndScrollView();

            //if (comboBoxControl != null)
            //{
            //    var selectedItemIndex = comboBoxControl.SelectedItemIndex;
            //    selectedItemIndex = comboBoxControl.List(
            //        new Rect(20, 40, 160, 20), comboBoxList[selectedItemIndex].text, comboBoxList, listStyle);
            //}

            // Set the window to be draggable by the top title bar
            GUI.DragWindow(new Rect(0, 0, 10000, 20));


            windowRect = ResizeWindow(windowRect, ref isResizing, ref windowResizeStart, minWindowSize);

        }


        bool isResizing = false;
        Rect windowResizeStart = new Rect();
        Vector2 minWindowSize = new Vector2(75, 50);
        static GUIStyle styleWindowResize = null;
        static GUIContent gcDrag = new GUIContent("//", "drag to resize");

        public static Rect ResizeWindow(Rect windowRect, ref bool isResizing, ref Rect resizeStart, Vector2 minWindowSize)
        {
            if (styleWindowResize == null)
            {
                // this is a custom style that looks like a // in the lower corner
                styleWindowResize = GUI.skin.GetStyle("WindowResizer");
            }

            Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            Rect r = GUILayoutUtility.GetRect(gcDrag, styleWindowResize);
            r.x = windowRect.width - 25;
            r.y = windowRect.height - 25;
            r.width = 25;
            r.height = 25;

            if (Event.current.type == EventType.mouseDown && r.Contains(mouse))
            {
                isResizing = true;
                resizeStart = new Rect(mouse.x, mouse.y, windowRect.width, windowRect.height);
                //Event.current.Use(); // the GUI.Button below will eat the event, and this way it will show its active state
            }
            else if (Event.current.type == EventType.mouseUp && isResizing)
            {
                isResizing = false;
            }
            else if (!Input.GetMouseButton(0))
            {
                // if the mouse is over some other window we won't get an event, this just kind of circumvents that by checking the button state directly
                isResizing = false;
            }
            else if (isResizing)
            {
                windowRect.width = Mathf.Max(minWindowSize.x, resizeStart.width + (mouse.x - resizeStart.x));
                windowRect.height = Mathf.Max(minWindowSize.y, resizeStart.height + (mouse.y - resizeStart.y));
                windowRect.xMax = Mathf.Min(Screen.width, windowRect.xMax); // modifying xMax affects width, not x
                windowRect.yMax = Mathf.Min(Screen.height, windowRect.yMax); // modifying yMax affects height, not y
            }

            GUI.Button(r, gcDrag, styleWindowResize);

            return windowRect;
        }

        public static void Log(string message, LogType type=LogType.Log)
        {
            ConsoleMessage entry = new ConsoleMessage(message, type);
            entries.Add(entry);
            ScrollToEnd();
        }
    }
}
