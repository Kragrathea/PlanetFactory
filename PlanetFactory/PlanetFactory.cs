using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace PlanetFactory
{
    public partial class PlanetFactory : MonoBehaviour
    {
        private static Dictionary<string, PSystemBody> prefabBodies = new Dictionary<string, PSystemBody>();
        private static int nextFlightGlobalsIndex = 16;
        private bool guiHidden = true;
        public static string DataPath = "GameData/PlanetFactory/PluginData/PlanetFactory/";
        public static string OverridePath = string.Empty;
        private static PlanetFactory _instance=null;
        public static PlanetFactory Instance{  
            get{
                return _instance;
            }
            private set{
                _instance=value;
            }
        }

        public string ConfigFilename = DataPath + "PlanetFactory.cfg";

        public static void SetPathFor(PFBody body)
        {
            if (body != null && body.parentSystem != null)
            {
                //PFUtil.Log("set OverridePath:" + body.parentSystem.path);
                PlanetFactory.OverridePath = body.parentSystem.path;
                //print("currentPath:" + CurrentPath);
            }
            else
            {
                //PFUtil.Log("clear OverridePath");
                PlanetFactory.OverridePath = string.Empty;
            }
        }
        public static string CurrentPath{
            get
            {
                if (OverridePath != string.Empty)
                    return OverridePath;
                return DataPath;
            }
        }
        public PlanetFactory()
        {
            _instance = this;
        }
        private ConfigNode PFConfigRoot;
        public bool autoLoad = true;
        public bool isFactoryEnabled = true;
        //public bool isSystemEnabled = true;
      
        public bool autoLoadSave = true;
        public string autoLoadSaveName = "default";
        public double ultraWarpAlt = 20000000000.0;
        public bool ultraWarpEnabled = true;
        public static bool shiftyMode = false;
        public string disabledSystems = "";
                                     
        private void LoadConfig()
        {
            PFUtil.Log("LoadConfig:" + ConfigFilename);
            PFConfigRoot = ConfigNode.Load(ConfigFilename);
            if (PFConfigRoot == null)
            {
                PFConfigRoot = new ConfigNode();
            }
            var pfConfig = PFConfigRoot.nodes.GetNode("PlanetFactory");
            if (pfConfig == null)
            {
                PFConfigRoot.AddNode("PlanetFactory");
                SaveConfig();
            }
            else
            {
                PFUtil.Log("loading PlanetFactory config:");
                LoadConfiguration(this, pfConfig);
            }
        }
        private void SaveConfig()
        {
            PFUtil.Log("SaveConfig");
            var pfConfig = PFConfigRoot.nodes.GetNode("PlanetFactory");

            pfConfig.SetValue("autoLoad", autoLoad.ToString());
            pfConfig.SetValue("isFactoryEnabled", isFactoryEnabled.ToString());
            //pfConfig.SetValue("isSystemEnabled", isSystemEnabled.ToString());
          
            pfConfig.SetValue("autoLoadSave", autoLoadSave.ToString());
            pfConfig.SetValue("autoLoadSaveName", autoLoadSaveName);

            //var sNode=new ConfigNode("Systems");

            var disabledSys = new List<string>();
            foreach (var s in Systems)
            {
                if (!s.enabled)
                    disabledSys.Add(s.name);
            }
//            print(string.Join(",", disabledSys.ToArray()));
            if (!pfConfig.values.Contains("disabledSystems"))
                pfConfig.AddValue("disabledSystems", string.Join(",", disabledSys.ToArray()));
            else
                pfConfig.SetValue("disabledSystems", string.Join(",", disabledSys.ToArray()));
            //PFConfigRoot.AddNode(sNode);

            PFConfigRoot.Save(ConfigFilename);
        }

        private List<string> saveNames;
        private void FindSaves()
        {
            PFUtil.Log("FindSaves");
            var dirs=Directory.GetDirectories("saves\\");
            saveNames = dirs.Where(x => File.Exists(x + "\\persistent.sfs")).Select(x => x.Split(new[] { '\\' })[1]).ToList();
        }
        private void Awake()
        {
            print("Injector awake");

            DontDestroyOnLoad(this);

        }
        private void Start()
        {
            print("PlanetFactory Starting");

            string sFilePath = KSP.IO.IOUtils.GetFilePathFor(this.GetType(), "");

            sFilePath = sFilePath.Replace("\\", "/");
            DataPath = sFilePath+"/";
            print("Setting datapath:" + DataPath);

            File.Delete(PlanetFactory.DataPath + "Log.txt");

            PFUtil.Log("//////////////////////////////////////////////////////////////////////////////////////////");
            PFUtil.Log("PlanetFactory Starting at"+DateTime.Now);
            PFUtil.Log("//////////////////////////////////////////////////////////////////////////////////////////");
            PFUtil.Log("PlanetFactory Data Path:" + DataPath);

            ConfigFilename = DataPath + "PlanetFactory.cfg";

            LoadConfig();

            FindSystems();

            //FindSaves();

            InitComboBox();
        }


        private bool bDoOnce = true;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha9) && Input.GetKey(KeyCode.LeftControl))
                guiHidden = !guiHidden;

            if (Input.GetKeyDown(KeyCode.U) && Input.GetKey(KeyCode.LeftControl))
                ultraWarpEnabled = !ultraWarpEnabled;

            UpdateTimewarps();

            if (!autoLoad)
                return;

            //var menu = GameObject.Find("MainMenu");
            //if (menu != null && bDoOnce)
            //{
            //    bDoOnce = false;

            //    if (autoLoadSave)
            //    {
            //        HighLogic.CurrentGame = GamePersistence.LoadGame("persistent", autoLoadSaveName, true, false);
            //        if (HighLogic.CurrentGame != null)
            //        {
            //            HighLogic.SaveFolder = autoLoadSaveName;
            //            //load to SpaceCenter
            //            HighLogic.CurrentGame.Start();
            //            //Go to flight
            //            //FlightDriver.StartAndFocusVessel("persistent", 0);// FlightGlobals.Vessels.Count()-1);
            //        }
            //    }
            //    else
            //    {
            //        //pop up load game dialog.
            //        var mc = menu.GetComponent<MainMenu>();
            //        mc.continueBtn.onPressed.Invoke();
            //    }
            //}
        }

        private bool isFactoryLoaded = false;
        private bool isTooLateToLoad = false;
        private bool antiKaboomEnabled = true;


        private void SetLocalCollision(string planetName,bool enabled=true)
        {
            var localPlanet = PFUtil.FindLocal(planetName);
            var cols = localPlanet.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                if (c.enabled!=enabled)
                {
                    //PFUtil.Log("Updating collision " + c.gameObject.name + "=" + enabled);
                    c.enabled = enabled;
                }
            }

        }

        private string currentBodyName;
        private void UpdateCollision()
        {
            if (FlightGlobals.currentMainBody!=null && FlightGlobals.currentMainBody.bodyName != currentBodyName)
            {
                PFUtil.Log("Body change " + currentBodyName + " to " + FlightGlobals.currentMainBody.bodyName);

                if (currentBodyName != null)
                    SetLocalCollision(currentBodyName, false);
                currentBodyName = FlightGlobals.currentMainBody.bodyName;
                SetLocalCollision(currentBodyName, true);
            }
        }
  

        private float[] ultraWarpRates= new float[] { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };
        private float[] stockWarpRates = new float[] { 1, 10, 100, 1000, 10000, 100000 };
        public void UpdateTimewarps()
        {
            if (FlightGlobals.currentMainBody == null || FlightGlobals.ActiveVessel==null || TimeWarp.fetch == null)
                return;
            if(antiKaboomEnabled)
                UpdateCollision();

            if (ultraWarpEnabled && (FlightGlobals.ship_altitude > ultraWarpAlt || FlightGlobals.ActiveVessel.LandedOrSplashed))
            {
                if (TimeWarp.fetch.warpRates.Length < ultraWarpRates.Length)
                {
                    PFUtil.Log("UltraWarp on " + FlightGlobals.ship_altitude);
                    TimeWarp.fetch.warpRates = ultraWarpRates;
                }
            }else
            {
                if (TimeWarp.fetch.warpRates.Length != stockWarpRates.Length)
                {
                    PFUtil.Log("UltraWarp off " + FlightGlobals.ship_altitude);
                    if(TimeWarp.CurrentRateIndex>stockWarpRates.Length-1)
                        TimeWarp.SetRate(stockWarpRates.Length-1, true);
                    TimeWarp.fetch.warpRates = stockWarpRates;
                }
            }

        }

        public void OnLevelWasLoaded(int level)
        {
            //PFUtil.Log("OnLevelWasLoaded:" + level);

            if (PSystemManager.Instance != null && ScaledSpace.Instance == null)
            {
                isTooLateToLoad = true;
            }

//            if (!isFactoryEnabled)// && !isSystemEnabled)
//                return;


            PlanetariumCamera.fetch.maxDistance = 5000000000000;

            if (PSystemManager.Instance != null && ScaledSpace.Instance == null)
            {
                isFactoryLoaded = true;
                if (!isFactoryEnabled)
                {
                    newBodies = new List<PFBody>();
                }

                LoadSystems();

                //PFUtil.Log("Body indexes");
                //foreach (var body in newBodies)
                //{
                //    PFUtil.Log(body.name + ":" + body.flightGlobalsIndex);
                //}

                findPrefabBodies(PSystemManager.Instance.systemPrefab.rootBody);
                
                foreach (var b in newBodies)
                {
                    try
                    {
                        b.CreatePrefab();
                    }
                    catch (Exception e)
                    {
                        PFUtil.Log("Error creating planet " + b.name,LogType.Error);
                        PFUtil.Log("Exception:" + e.Message, LogType.Error);
                        PFUtil.Log("InnerException:" + e.InnerException.Message, LogType.Error);
                        PFUtil.Log("Stack:", LogType.Error);
                    }
                }
                PSystemManager.Instance.OnPSystemReady.Add(OnPSystemReady);
            }
        }

        public void OnPSystemReady()
        {
            //print("OnPSystemReady");
//            if (!isFactoryEnabled)// && !isSystemEnabled)
//                return;
            foreach (var b in newBodies)
            {
                try
                {
                    b.UpdateBody();
                }
                catch (Exception e)
                {
                    PFUtil.Log("Error updating planet " + b.name, LogType.Error);
                    PFUtil.Log("Exception:" + e.Message, LogType.Error);
                    if(e.InnerException!=null)
                        PFUtil.Log("InnerException:" + e.InnerException.Message, LogType.Error);
                    PFUtil.Log("Stack:"+e.StackTrace, LogType.Error);
                }
            }
            //PFUtil.Log(Dump.DumpMods());
        }

        public class PFSystem
        {
            public string name;
            public List<string> planetNames = new List<string>();
            public bool enabled = true;
            public string path = "";

            public List<PFBody> LoadPlanets()
            {
                var bodies = new List<PFBody>();

                if (!enabled)
                    return bodies;

                PFUtil.Log("Loading System:" + name);
                foreach (var planetName in planetNames)
                {
                    var bodyConfigFilename = path + planetName + ".cfg";
                    PFUtil.Log("Loading Body:" + planetName);
                    var configRoot = ConfigNode.Load(bodyConfigFilename);
                    if (configRoot.nodes.Contains("PFBody"))
                    {
                            var n = configRoot.GetNode("PFBody");
                            var body = new PFBody()
                            {
                                parentSystem=this
                            };
                            LoadConfiguration(body, n);
                            bodies.Add(body);
                    }
                }
                //PFUtil.Log("LoadSystem finished");
                return(bodies);
            }
        }
        public List<PFSystem> Systems = new List<PFSystem>();
        public void FindSystems()
        {
            var sysFiles=Directory.GetFiles(DataPath, "*.system", SearchOption.AllDirectories);
            foreach (var sysFile in sysFiles)
            {
                var sysName=Path.GetFileNameWithoutExtension(sysFile);
                PFUtil.Log("LoadSystem:" + sysName);
                //PFUtil.Log(sysFile);
                var configRoot = ConfigNode.Load(sysFile);
                var systemNode = configRoot.GetNode("System");
                if (systemNode == null)
                    continue;

                PFSystem curSystem = null;
                int i = 0;
                while (true)
                {
                    var planetName = systemNode.GetValue("planetName", i);
                    if (planetName == null)
                        break;

                    if (curSystem == null)
                    {
                        var sysPath =Path.GetDirectoryName(sysFile)+"/";
                        //PFUtil.Log("New system:" + sysName + "->" + sysPath);
                        curSystem = new PFSystem() {
                            name = sysName,
                            path = sysPath
                        };
                    }
                    curSystem.planetNames.Add(planetName);
                    PFUtil.Log(string.Format("Adding body {0} to system {1}", planetName, curSystem.name));
                    i++;
                }

                if (curSystem != null)
                {
                    var disabledList = disabledSystems.Split(new char[] { ',' });
                    if (disabledList.Any(x => x == curSystem.name))
                    {
                        PFUtil.Log("Disabling system " + curSystem.name);
                        curSystem.enabled = false;
                    }
                    Systems.Add(curSystem);
                }
                //var lines=File.ReadAllLines(name);
                //PFSystem curSystem = null;
                //foreach (var line in lines)
                //{
                //    var l=line.Split(new string[]{"//"},StringSplitOptions.None);
                //    var planetName = l[0].Trim();
                //    if (planetName != string.Empty && File.Exists(DataPath+planetName+".cfg"))
                //    {
                //        if (curSystem == null)
                //            curSystem = new PFSystem(){name=name};
                //        curSystem.planetNames.Add(planetName);
                //        print(string.Format("Adding body {0} to system {1}",planetName,curSystem.name));
                //    }
                //}
            }
        }

        public void LoadSystems()
        {
            foreach (var sys in Systems)
            {
                var bodies = sys.LoadPlanets();
                newBodies.AddRange(bodies);
            }
        }
        //public void xLoadSystem()
        //{
        //    var systemConfigFilename=DataPath + "System.cfg";
        //    print("LoadSystem:" + systemConfigFilename);
        //    configRoot = ConfigNode.Load(systemConfigFilename);
        //    int i = 0;
        //    while (true)
        //    {
        //        var n = configRoot.GetNode("PFBody", i);
        //        if (n == null)
        //            break;

        //        var body = new PFBody();
        //        LoadConfiguration(body, n);
        //        newBodies.Add(body);
        //        i++;
        //    }
        //    print("LoadSystem finished");
        //}
        public class PFOrbit
        {
            public double inclination;
            public double eccentricity;
            public double semiMajorAxis;
            public double LAN;
            public double argumentOfPeriapsis;
            public double meanAnomalyAtEpoch;
            public double epoch;
            public string referenceBody;

            public PFOrbit()
            {
            }
            public bool Load(string name)
            {
                var root = ConfigNode.Load(CurrentPath + name + ".cfg");
                if (root == null)
                    return false;
                var orbitConfig = root.nodes.GetNode("Orbit");
                if (orbitConfig == null)
                    return false;
                //print("Loading orbital config " + name);
                LoadConfiguration(this, orbitConfig);
                return true;
            }
        }
        public delegate void localUpdateDelegate(PFBody body);

        public class PFBody
        {
            public string name;
            public string templateName;
            public PFOrbit orbit;
            public int flightGlobalsIndex;
            public bool hideScaled = false;
            public PFSystem parentSystem=null;
            public bool clearPQS = false;

            [XmlIgnore]
            public localUpdateDelegate localUpdate=null;

            public PFBody()
            {
                
            }
            public PFBody(string name, string templateName, int flightGlobalsIndex, PFOrbit orbit,
                localUpdateDelegate localUpdate=null)
            {
                this.name = name;
                this.templateName = templateName;
                this.orbit = orbit;
                this.localUpdate = localUpdate;
                this.flightGlobalsIndex = flightGlobalsIndex;
            }

            public void CreatePrefab()
            {
                try
                {
                    if (parentSystem != null)
                    {
                        PlanetFactory.OverridePath = parentSystem.path;
                    }
                    var templateBody = prefabBodies[templateName];

                    var body = (PSystemBody)Instantiate(templateBody);

                    body.celestialBody.bodyName = name;
                    body.flightGlobalsIndex = flightGlobalsIndex;

                    if (orbit == null)
                        orbit = new PFOrbit();

                    //load from config (if any)
                    orbit.Load(name);

                    var parentBody = prefabBodies[orbit.referenceBody];
                    if (body.celestialBody.orbitDriver != null)
                    {
                        body.celestialBody.orbitDriver.orbit = new Orbit(orbit.inclination, orbit.eccentricity,
                            orbit.semiMajorAxis, orbit.LAN,
                            orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch, parentBody.celestialBody);

                        body.celestialBody.orbitDriver.UpdateOrbit();
                    }


                    LoadCB(body.celestialBody);


                    body.children.Clear();
                    parentBody.children.Add(body);
                    body.enabled = false;

                    prefabBodies[name] = body;
                }
                finally
                {
                    PlanetFactory.OverridePath = string.Empty;
                }
            }

            public void UpdateBody()
            {
                PFUtil.Log("UpdateLocal:" + name);
                try
                {
                    if (parentSystem != null)
                    {
                        PlanetFactory.OverridePath = parentSystem.path;
                    }

                    if (clearPQS)
                        ClearPQSMods(name);

                    if (localUpdate != null)
                        localUpdate(this);
                    else
                        LoadPQS(name);

                    PFUtil.Log("UpdateScaled:" + name);
                    var smallPlanet = PFUtil.FindScaled(name);

                    if (templateName == "Sun")
                    {
                        var sunBody = PFUtil.FindCB(orbit.referenceBody);
                        var cb = PFUtil.FindCB(name);

                        var orbitDriver = smallPlanet.AddComponent<OrbitDriver>();
                        orbitDriver.updateMode = OrbitDriver.UpdateMode.UPDATE;
                        orbitDriver.name = cb.name;
                        orbitDriver.orbit = new Orbit(orbit.inclination, orbit.eccentricity, orbit.semiMajorAxis, orbit.LAN,
                            orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch, sunBody);

                        cb.orbitDriver = orbitDriver;

                        orbitDriver.referenceBody = sunBody;
                        orbitDriver.celestialBody = cb;

                        cb.sphereOfInfluence = cb.orbit.semiMajorAxis * Math.Pow(cb.Mass / cb.orbit.referenceBody.Mass, 0.4);
                        cb.hillSphere = cb.orbit.semiMajorAxis * (1 - cb.orbit.eccentricity) * Math.Pow(cb.Mass / cb.orbit.referenceBody.Mass, 0.333333333333333);
                        cb.orbitDriver.QueuedUpdate = true;
                        cb.CBUpdate();

                        orbitDriver.UpdateOrbit();
                    }

                    LoadScaledPlanet(smallPlanet, name);

                    if (hideScaled)//doesnt work. 
                    {
                        var bigPlanet = PFUtil.FindLocal(name);
                        var mr = bigPlanet.GetComponent<MeshFilter>();
                        if (mr != null)
                            mr.mesh = mr.sharedMesh = null;

                        mr = smallPlanet.GetComponent<MeshFilter>();
                        if (mr != null)
                            mr.mesh = mr.sharedMesh = null;
                        //mr.active = false;
                    }
                }
                finally
                {
                    PlanetFactory.OverridePath = string.Empty;
                }
            }

            public static void LoadScaledPlanet(GameObject smallPlanet, string name,bool bLoadTemp=false)
            {
                PFUtil.Log("LoadScaledPlanet:" + name);

                var root = ConfigNode.Load(CurrentPath + name + ".cfg");
                if (root != null)
                {
                    var sConfig = root.nodes.GetNode("ScaledTransform");
                    //print(cbConfig);
                    if (sConfig != null)
                    {
                        var scaledBody = PFUtil.FindScaled(name);
                        if (sConfig.HasValue("ratio"))
                        {
                            var ratio = float.Parse(sConfig.GetValue("ratio"));
                            var newScale = (float)PFUtil.FindCB(name).Radius * ratio;
                            scaledBody.transform.localScale = new Vector3(newScale, newScale, newScale);
                        }
                    }

                }
                var binName = name + ".bin";
                if (!bLoadTemp)
                {
                    var colorTexture = PFUtil.LoadTexture(CurrentPath + name + "_map.png");
                    var bumpTexture = PFUtil.LoadTexture(CurrentPath + name + "_normal.png");

                    if (bumpTexture == PFUtil.defaultTexture)
                        bumpTexture = null; //Bug fix. Dont use Normal map if not found.

                    LoadScaledPlanetTextures(name, colorTexture, bumpTexture);
                }
                else
                {
                    var colorTexture = PFUtil.LoadTexture(CurrentPath + name + "_map_.png");
                    var bumpTexture = PFUtil.LoadTexture(CurrentPath + name + "_normal_.png");

                    if (bumpTexture == PFUtil.defaultTexture)
                        bumpTexture = null; //Bug fix. Dont use Normal map if not found.

                    binName = name + "_.bin";
                    LoadScaledPlanetTextures(name, colorTexture, bumpTexture);
                }

                var binFileName = CurrentPath + binName;
                if (File.Exists(binFileName))
                //if (KSP.IO.File.Exists<PlanetFactory>(binName))
                {
                    PFUtil.Log("Loading mesh:" + binName);
                    var smallPlanetMeshFilter = (MeshFilter) smallPlanet.GetComponentInChildren((typeof (MeshFilter)));
                    var newVerts = new Vector3[smallPlanetMeshFilter.mesh.vertices.Count()];
                    //var reader = KSP.IO.BinaryReader.CreateForType<PlanetFactory>(binName);

                    var stream = File.OpenRead(binFileName);
                    var reader = new BinaryReader(stream);

                    for (var i = 0; i < newVerts.Count(); i++)
                    {
                        newVerts[i].x = reader.ReadSingle();
                        newVerts[i].y = reader.ReadSingle();
                        newVerts[i].z = reader.ReadSingle();
                    }
                    smallPlanetMeshFilter.mesh.vertices = newVerts;
                    smallPlanetMeshFilter.mesh.RecalculateNormals();

                    //smallPlanetMeshFilter.mesh.tangents = null; 
                    PFUtil.RecalculateTangents(smallPlanetMeshFilter.mesh);

                }
            }

        }

        public static Orbit ParseOrbit(ConfigNode node)
        {
            var inc = double.Parse(node.GetValue("inclination"));
            var ecc = double.Parse(node.GetValue("eccentricity"));
            var sma = double.Parse(node.GetValue("semiMajorAxis"));
            var lan = double.Parse(node.GetValue("LAN"));
            var aop = double.Parse(node.GetValue("argumentOfPeriapsis"));
            var mae = double.Parse(node.GetValue("meanAnomalyAtEpoch"));
            var epo = double.Parse(node.GetValue("epoch"));
            var refBody = node.GetValue("referenceBody");
            return new Orbit(inc, ecc, sma, lan, aop, mae, epo, PFUtil.FindCB(refBody));
        }

        public static AnimationCurve ParseCurve(ConfigNode node)
        {
            var values = node.GetValues("key");

            int length = (int)values.Length;
            var curve = new AnimationCurve();
            for (int i = 0; i < length; i++)
            {
                string[] strArrays = values[i].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                curve.AddKey(float.Parse(strArrays[0]), float.Parse(strArrays[1]));
            }
            return (curve);
        }

        //Overlay a .CFG on a given object.
        public static void LoadConfiguration(System.Object obj, ConfigNode node)
        {
            var t = obj.GetType();
 
            var config = ConfigToDict(node);
            foreach (var key in config.Keys)
            {
                var field = t.GetField(key);
                //print(field);
                if(field!=null)
                {
                    //print(field.FieldType.ToString().ToLower() +" Was "+field.GetValue(obj));
                    switch(field.FieldType.ToString().ToLower())
                    {
                        case "system.string":
                            field.SetValue(obj,config[key]);
                            break;
                        case "system.single":
                            field.SetValue(obj,float.Parse(config[key]));
                            break;
                        case "system.double":
                            field.SetValue(obj, double.Parse(config[key]));
                            break;
                        case "system.int32":
                            field.SetValue(obj, int.Parse(config[key]));
                            break;
                        case "system.uint32":
                            field.SetValue(obj, uint.Parse(config[key]));
                            break;
                        case "system.boolean":
                            field.SetValue(obj, bool.Parse(config[key]));
                            break;
                        case "unityengine.color":
                            field.SetValue(obj, ConfigNode.ParseColor(config[key]));
                            break;
                        case "unityengine.vector3":
                            field.SetValue(obj, ConfigNode.ParseVector3(config[key]));
                            break;
                        case "unityengine.animationcurve":
                            field.SetValue(obj, ParseCurve(node.GetNode(key)));
                            break;
                        case "unityengine.gradient":
                            field.SetValue(obj, new Gradient());
                            break;
                        case "orbit":
                            field.SetValue(obj, ParseOrbit(node.GetNode(key)));
                            break;
                        case "pqsmod_pfheightcolor+landclass[]":
                            {
                                int i = 0;
                                var lcs = new List<PQSMod_PFHeightColor.LandClass>();
                                //print("parse landclass");
                                var pn = node.GetNode(key);
                                while (true)
                                {
                                    var n = pn.GetNode("LandClass", i);
                                    if (n != null)
                                    {
                                        var nlc = new PQSMod_PFHeightColor.LandClass();
                                        LoadConfiguration(nlc, n);
                                        lcs.Add(nlc);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    i++;
                                }
                            field.SetValue(obj, lcs.ToArray());
                            }
                            break;
                        case "pqsmod_vertexplanet+landclass[]":
                            {
                                int i = 0;
                                var lcs = new List<PQSMod_VertexPlanet.LandClass>();
           PFUtil.Log("pqsmod_vertexplanet+landclass[]");
                                var pn = node.GetNode(key);
                                while (true)
                                {
                                    var n = pn.GetNode("LandClass", i);
                                    if (n != null)
                                    {
                                        var nlc = new PFVertexPlanetLandClass();
                                        LoadConfiguration(nlc, n);
                                        lcs.Add(nlc);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    i++;
                                }
                                field.SetValue(obj, lcs.ToArray());
         PFUtil.Log("END pqsmod_vertexplanet+landclass[]");

                            }
                            break;                            
                        case "mapso":
                            //print("Loading map:"+config[key]);
                            if (config[key].ToLower() == "null")
                                field.SetValue(obj, null);
                            else
                            {
                                var colorTexture = PFUtil.LoadTexture(CurrentPath + config[key]);
                                var mapso = (MapSO) ScriptableObject.CreateInstance(typeof (MapSO));
                                mapso.CreateMap(MapSO.MapDepth.RGBA, colorTexture);
                                field.SetValue(obj, mapso);
                            }
                            break;


                    }
                   
                }
            }

        }


            //TODO:Remove this funtion. Easier to deal with the config nodes directly.
        public static Dictionary<string,string> ConfigToDict(ConfigNode node)
        {
            var dict=new Dictionary<string,string> ();
            for(var vi=0;vi<node.values.Count;vi++)
            {
                var value = node.values[vi];
                dict[value.name]=value.value;
            }
            for (int ni = 0; ni < node.nodes.Count; ni++)
            {
                var snode = node.nodes[ni];
                dict[snode.name] = "";
            }
            return (dict);
        }
        public static void LoadCB(CelestialBody body,ConfigNode root=null)
        {
            if(root==null)
                root = ConfigNode.Load(CurrentPath + body.bodyName + ".cfg");
            if (root != null)
            {
                var cbConfig = root.nodes.GetNode("CelestialBody");
                if (cbConfig != null)
                {
                    PFUtil.Log("loading CB config:" + body.bodyName);
                    LoadConfiguration(body, cbConfig);
                    body.CBUpdate();
                }
            }
        }
        public static void LoadOrbit(CelestialBody body)
        {
            var orbit = new PFOrbit();
            if(!orbit.Load(body.name))
                return;

            var parentBody = PFUtil.FindCB(body.name);
            if (body.orbitDriver != null)
            {
                body.orbitDriver.orbit = new Orbit(orbit.inclination, orbit.eccentricity,
                    orbit.semiMajorAxis, orbit.LAN,
                    orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch,body.orbit.referenceBody);// parentBody);

                body.orbitDriver.UpdateOrbit();
            }
        }
        //PQSMod_MaterialSetDirection.target
        private static string[] EssentialMods = { "PQSMod_MaterialSetDirection", "PQSMod_CelestialBodyTransform", "PQSMod_AltitudeAlpha", "PQSMod_UVPlanetRelativePosition", "PQSMod_QuadMeshColliders" };
        //PQSMod_AerialPerspectiveMaterial?
        public static void ClearPQSMods(string bodyName)
        {
            PFUtil.Log("ClearPQSMods:" + bodyName);
            var localGameObject = PFUtil.FindLocal(bodyName);
            var mods = localGameObject.GetComponentsInChildren<PQSMod>(true);
            foreach (var mod in mods)
            {
                var modType = mod.GetType().ToString();
                if (!EssentialMods.Any(x => modType.Contains(x)))
                {
                    mod.modEnabled = false;
                    mod.gameObject.SetActive(false);
                    Destroy(mod.gameObject);
                    PFUtil.Log("Removed:" + modType);
                }
            }
        }

        public static void LoadPQS(string bodyName)
        {
            var localGameObject = PFUtil.FindLocal(bodyName);// localSpace.transform.FindChild(body.name).gameObject;

            var root = ConfigNode.Load(CurrentPath + bodyName + ".cfg");

            var pqs = localGameObject.GetComponentInChildren<PQS>();

            //Remove PQS cities. TODO:Refactor.
            var mods = localGameObject.GetComponentsInChildren<PQSMod>(true);
            foreach (var mod in mods)
            {
                if(mod.GetType().ToString().Contains("PQSCity"))
                {
                    mod.modEnabled = false;
                    mod.gameObject.SetActive(false);
                    Destroy(mod.gameObject);
                    //PFUtil.Log("Removed PQSCity:");
                }
            }


            for (int ni = 0; ni < root.nodes.Count; ni++)
            {
                var node = root.nodes[ni];

                if (!node.name.ToLower().StartsWith("pqs"))
                    continue;

                var componentTypeStr = node.name;
                var componentType = Type.GetType(componentTypeStr + ", Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                if(componentType==null)
                    componentType = Type.GetType(componentTypeStr + ", PlanetFactory, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (componentType == null)
                {
                    PFUtil.Log("Cant find PQSMod type:" + componentTypeStr,LogType.Warning);
                    continue;
                }
                var component = localGameObject.GetComponentInChildren(componentType);
                if (component == null)
                {
                    //print("pqs not found");
                    if (node.HasValue("modEnabled"))
                    {
                        //print("is pqs mod " + node.GetValue("modEnabled"));
                        if (node.GetValue("modEnabled").ToLower() == "true")
                        {
                            //print("Adding PQSMOD:" + componentTypeStr);
                            var newgob = new GameObject();
                            var newComponent = (PQSMod)newgob.AddComponent(componentType);
                            
                            newgob.transform.parent = pqs.gameObject.transform;
                            newComponent.sphere = pqs;
                            component = newComponent;

                        }
                    }
                }

                if (component != null)
                {
                    LoadConfiguration(component, node);

                    try
                    {
                        //print("Rebuilding");
                        var mod = (PQSMod)component;
                        mod.RebuildSphere();
                    }
                    catch
                    {
                        //print("failed");
                    }
                }
            }
            if (pqs != null)
                pqs.RebuildSphere();
        }

        public PFBody FindPFBody(string name)
        {
            return newBodies.FirstOrDefault(x=>x.name==name);
        }

        public List<PFBody> newBodies = new List<PFBody>
        {
            new PFBody("Ablate", "Minmus", 120,
                new PFOrbit
                {
                    inclination = 05.0f,
                    eccentricity = 0,
                    semiMajorAxis = 910000000,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 30000,
                    referenceBody = "Sun"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);
                }),

            new PFBody("Ascension", "Minmus", 130,
                new PFOrbit
                {
                    inclination = 28.0f,
                    eccentricity = 0.0,
                    semiMajorAxis = 4400000000,
                    LAN = 0,        
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 600000000,
                    referenceBody = "Sun"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);

                    //PFEffects.AddEffect(PFUtil.FindScaled(body.name), "fx_exhaustFlame_blue", Vector3.zero, Quaternion.Euler(-90, 0, 0), new Vector3(5000000000, 5000000000, 5000000000));
                }),
            new PFBody("Inaccessable", "Minmus", 140,
                new PFOrbit
                {
                    inclination = 2.0f,
                    eccentricity = 0,
                    semiMajorAxis = 125000000000,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 3600000000,
                    referenceBody = "Sun"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);

                    if (!shiftyMode)
                        return;


                    var localGameObject = PFUtil.FindLocal(body.name);
                    var vp = localGameObject.GetComponentInChildren<PQSMod_VertexPlanet>();
                    var md = vp.gameObject.AddComponent<PQSMod_MapDecal>();

                    md.modEnabled=true;
                    md.order=99987;
                    md.radius=500;
                    md.position=new Vector3(0,40737.1f,0);
                    md.angle=0;

                    var t = PFUtil.LoadTexture("Shifty.png",true);
                    md.heightMap = (MapSO)ScriptableObject.CreateInstance(typeof(MapSO));
                    md.heightMap.CreateMap(MapSO.MapDepth.RGBA, t);

                    //md.heightMap=null;
                    md.heightMapDeformity=80;
                    md.cullBlack=true;
                    md.useAlphaHeightSmoothing=false;
                    md.absolute=false;
                    md.absoluteOffset=0;
                    md.smoothHeight=0.75f;
                    md.smoothColor=0.75f;
                    md.removeScatter=true;
                    md.sphere=vp.sphere;
                    md.RebuildSphere();
                }),

            new PFBody("Sentar", "Jool", 150,
                new PFOrbit
                {
                    inclination = 26.0f,
                    eccentricity = 0,
                    semiMajorAxis = 160000000000,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 0,
                    referenceBody = "Sun"
                },
                localUpdate: delegate(PFBody body)
                {

                    var localGameObject = PFUtil.FindLocal(body.name);

                    var orbitDriver = localGameObject.GetComponent<OrbitDriver>();
                    if (orbitDriver != null)
                        orbitDriver.orbitColor = Color.blue;

                    var smallPlanet = PFUtil.FindScaled(body.name);
                    var ringTexture = PFUtil.LoadTexture(DataPath + smallPlanet.name + "_ring.png");
                    PFEffects.AddRing(smallPlanet, 2001, 3000, 0, ringTexture);

                    var atmo = smallPlanet.transform.FindChild("Atmosphere");
                    var afg = atmo.GetComponent<AtmosphereFromGround>();
                    afg.waveLength = new Color(0.8f, 0.8f, 0.6f, 0.0f);
                    afg.invWaveLength = new Color(1f/Mathf.Pow(afg.waveLength[0], 4),
                        1f/Mathf.Pow(afg.waveLength[1], 4), 1f/Mathf.Pow(afg.waveLength[2], 4), 0.5f);

                    var cb = localGameObject.GetComponent<CelestialBody>();
                    cb.atmosphericAmbientColor = Color.blue;

                    var rimTexture = PFUtil.LoadTexture(DataPath + body.name + "_rim.png");
                    if (rimTexture != null)
                    {
                        //print("loading rim");
                        LoadScaledPlanetRim(body.name, rimTexture);
                    }        
                    cb.GeeASL = 0.0962500333786;
                    cb.CBUpdate();
                }),
            new PFBody("Thud", "Tylo", 152,
                new PFOrbit
                {
                    inclination = -20.0,
                    eccentricity = 0,
                    semiMajorAxis = 050000000d,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 0,
                    referenceBody = "Sentar"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);
                }),

            new PFBody("Erin", "Laythe", 154,
                new PFOrbit
                {
                    inclination = 15.0,
                    eccentricity = 0,
                    semiMajorAxis = 80000000d,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 0,
                    referenceBody = "Sentar"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);
                }),
            new PFBody("Pock", "Minmus", 156,
                new PFOrbit
                {
                    inclination = 20.0f,
                    eccentricity = 0.3,
                    semiMajorAxis = 3560000,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 0,
                    referenceBody = "Erin"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);

                    var localGameObject = PFUtil.FindLocal(body.name);
                    var vp = localGameObject.GetComponentInChildren<PQSMod_VertexPlanet>();

                    if (vp != null)
                    {
                            
                        vp.seed = 102030;
                        vp.deformity = 7000;

                        vp.landClasses[0].baseColor = new Color(0.3f, 0.3f, 0.4f);
                        vp.landClasses[1].baseColor = new Color(0.5f, 0.5f, 0.7f);
                        vp.landClasses[2].baseColor = new Color(0.7f, 0.6f, 0.7f);
                        vp.landClasses[3].baseColor = new Color(0.8f, 0.8f, 0.9f);

                        vp.sphere.minDetailDistance = 8;
                        vp.sphere.minLevel = 1;
                        vp.sphere.maxLevel = 9;

                        vp.RebuildSphere();
                    }

                    //print("Cratering");
                    var mun = PFUtil.FindLocal("Mun");
                    var mcraters = mun.GetComponentsInChildren<PQSMod_VoronoiCraters>()[0];
                    var ecg = (GameObject) Instantiate(mcraters.gameObject);
                    ecg.transform.parent = vp.gameObject.transform;

                    var ec = localGameObject.GetComponentInChildren<PQSMod_VoronoiCraters>();
                    ec.deformation = ec.deformation*0.7;
                    ec.simplexSeed = 667;
                    ec.voronoiSeed = -667;

                    ec.sphere = vp.sphere;
                    ec.RebuildSphere();

                }),
            new PFBody("Ringle", "Tylo", 158,
                new PFOrbit
                {
                    inclination = 20.0,
                    eccentricity = 0.25,
                    semiMajorAxis = 280000000d,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 0,
                    referenceBody = "Sentar"
                },
                localUpdate: delegate(PFBody body)
                {
                    var localGameObject = PFUtil.FindLocal(body.name);

                    var vhm = localGameObject.GetComponentInChildren<PQSMod_VertexHeightMap>();
                    if (vhm != null)
                    {
                        var colorTexture = PFUtil.LoadTexture(DataPath + body.name + "_color.png");
                        var heightTexture = PFUtil.LoadTexture(DataPath + body.name + "_height.png");
                        NewUpdateHeightMapPlanet(localGameObject, heightTexture, colorTexture);
                    }
                    var orbitDriver = localGameObject.GetComponent<OrbitDriver>();
                    if (orbitDriver != null)
                        orbitDriver.orbitColor = Color.yellow;

                    //print("Cratering");
                    var mun =  PFUtil.FindLocal("Mun");
                    var mcraters = mun.GetComponentsInChildren<PQSMod_VoronoiCraters>()[1];
                    var ecg = (GameObject) Instantiate(mcraters.gameObject);
                    ecg.transform.parent = vhm.gameObject.transform;

                    var ec = localGameObject.GetComponentInChildren<PQSMod_VoronoiCraters>();
                    ec.deformation = ec.deformation*3;
                    ec.simplexSeed = 667;
                    ec.voronoiSeed = -667;

                    ec.sphere = vhm.sphere;
                    ec.RebuildSphere();

                    var smallPlanet = ScaledSpace.Instance.transform.FindChild(body.name).gameObject;
                    var ringTexture = PFUtil.LoadTexture(DataPath + smallPlanet.name + "_ring.png");
                    var cb = PFUtil.FindCB(body.name);
                    PFEffects.AddRing(smallPlanet, cb.pqsController.mapMaxHeight*2, cb.pqsController.mapMaxHeight*3, 0,
                        ringTexture);

                }),
            new PFBody("Skelton", "Duna", 160,
                new PFOrbit
                {
                    inclination = 15.0,
                    eccentricity = 0.0,
                    semiMajorAxis = 120000000d,
                    LAN = 0,
                    argumentOfPeriapsis = 0,
                    meanAnomalyAtEpoch = 0,
                    epoch = 0,
                    referenceBody = "Sentar"
                },
                localUpdate: delegate(PFBody body)
                {
                    LoadPQS(body.name);
                }),
#if true

            //new PFBody("Serious", "Sun", 200,
            //    new PFOrbit
            //    {
            //        inclination = 30.0f,
            //        eccentricity = 0.2,
            //        semiMajorAxis = 450000000000,
            //        LAN = 0,
            //        argumentOfPeriapsis = 0,
            //        meanAnomalyAtEpoch = 0,
            //        epoch = 0,
            //        referenceBody = "Sun"
            //    },
            //    localUpdate: delegate(PFBody body)
            //    {
            //        var localGameObject = PFUtil.FindLocal(body.name);
            //        var cb = localGameObject.GetComponent<CelestialBody>();

            //        //cb.GeeASL = 0.0962500333786;
            //        //cb.CBUpdate();
            //    }),

            //new PFBody("Joker", "Minmus", 210,
            //    new PFOrbit
            //    {
            //        inclination = 30.0f,
            //        eccentricity = 0,
            //        semiMajorAxis = 2610000000,
            //        LAN = 0,
            //        argumentOfPeriapsis = 0,
            //        meanAnomalyAtEpoch = 0,
            //        epoch = 0,
            //        referenceBody = "Serious"
            //    },
            //    localUpdate: delegate(PFBody body)
            //    {
            //        var localGameObject = PFUtil.FindLocal(body.name);
            //        var vp = localGameObject.GetComponentInChildren<PQSMod_VertexPlanet>();
            //        if (vp != null)
            //        {
            //            vp.seed = 666;
            //            vp.deformity = 10000;

            //            vp.landClasses[0].baseColor = new Color(0.4f, 0.1f, 0.1f);
            //            vp.landClasses[1].baseColor = new Color(0.4f, 0.7f, 0.4f);
            //            vp.landClasses[2].baseColor = new Color(0.5f, 0.5f, 0.7f);
            //            vp.landClasses[3].baseColor = new Color(0.9f, 0.9f, 0.9f);

            //            vp.sphere.minDetailDistance = 8;
            //            vp.sphere.minLevel = 1;
            //            vp.sphere.maxLevel = 9;

            //            vp.RebuildSphere();
            //        }

            //        var orbitDriver = localGameObject.GetComponent<OrbitDriver>();
            //        if (orbitDriver != null)
            //            orbitDriver.orbitColor = Color.magenta;

            //    }),

#endif

        };

        public static void NewUpdateHeightMapPlanet(GameObject planetGameObject, Texture2D height, Texture2D color)
        {
            var vhm = planetGameObject.GetComponentInChildren<PQSMod_VertexHeightMap>();
            vhm.heightMap = (MapSO) ScriptableObject.CreateInstance(typeof (MapSO));
            vhm.heightMap.CreateMap(MapSO.MapDepth.RGBA, height);
            //vhm.heightMapDeformity *= 1;//Huge mountains.

            var vcm = planetGameObject.GetComponentInChildren<PQSMod_VertexColorMap>();
            if (vcm != null)
            {
                vcm.vertexColorMap = (MapSO) ScriptableObject.CreateInstance(typeof (MapSO));
                vcm.vertexColorMap.CreateMap(MapSO.MapDepth.RGBA, color);
            }

                //Disable common PQS to avoid them getting in the way later.
            var hcm = planetGameObject.GetComponentInChildren<PQSMod_HeightColorMap>();
            if (hcm != null)
                hcm.modEnabled = false;

            var vcs = planetGameObject.GetComponentInChildren<PQSMod_VertexColorSolid>();
            if (vcs != null)
                vcs.modEnabled = false;

            var vhn = planetGameObject.GetComponentInChildren<PQSMod_VertexHeightNoise>();
            if (vhn != null)
                vhn.modEnabled = false;

            var vsha = planetGameObject.GetComponentInChildren<PQSMod_VertexSimplexHeightAbsolute>();
            if (vsha != null)
                vsha.modEnabled = false;

            var vsnc = planetGameObject.GetComponentInChildren<PQSMod_VertexSimplexNoiseColor>();
            if (vsnc != null)
                vsnc.modEnabled = false;


            var vsh = planetGameObject.GetComponentInChildren<PQSMod_VertexSimplexHeight>();
            if (vsh != null)
                vsh.deformity = 0; 

            vhm.RebuildSphere();

        }

        public static void LoadScaledPlanetTextures(string planetName, Texture2D colorTexture, Texture2D bumpTexture)
        {
            var scaledPlanet = ScaledSpace.Instance.transform.FindChild(planetName).gameObject;
            var smallPlanetMeshRenderer = (MeshRenderer) scaledPlanet.GetComponentInChildren((typeof (MeshRenderer)));
            if (smallPlanetMeshRenderer == null)
                return;
            if (colorTexture != null)
                smallPlanetMeshRenderer.material.mainTexture = colorTexture;
            if (bumpTexture != null)
                smallPlanetMeshRenderer.material.SetTexture("_BumpMap", bumpTexture);
        }

        public static void LoadScaledPlanetRim(string planetName, Texture2D rimTexture)
        {
            var scaledPlanet = ScaledSpace.Instance.transform.FindChild(planetName).gameObject;
            var smallPlanetMeshRenderer = (MeshRenderer)scaledPlanet.GetComponentInChildren((typeof(MeshRenderer)));
            smallPlanetMeshRenderer.material.SetTexture("_rimColorRamp", rimTexture);
        }

        public void findPrefabBodies(PSystemBody body)
        {
            prefabBodies[body.celestialBody.name] = body;
            foreach (var c in body.children)
            {
                findPrefabBodies(c);
            }
        }
    }
}

public class PlanetFactoryPartlessLoader : KSP.Testing.UnitTest
{
    public PlanetFactoryPartlessLoader()
    {
        PlanetFactoryPluginWrapper.Initialize();
    }
}

public static class PlanetFactoryPluginWrapper
{
    public static GameObject PlanetFactory;
    public static GameObject DebugConsole;

    public static void Initialize()
    {
        if (GameObject.Find("PlanetFactory") == null)
        {
            PlanetFactory = new GameObject(
                "PlanetFactory",
                new [] {typeof (PlanetFactory.PlanetFactory)});
            UnityEngine.Object.DontDestroyOnLoad(PlanetFactory);
        }
        if (GameObject.Find("DebugConsole") == null)
        {
            DebugConsole = new GameObject(
                "DebugConsole",
                new[] { typeof(PlanetFactory.DebugConsole) });
            UnityEngine.Object.DontDestroyOnLoad(DebugConsole);
        }
    }
}


