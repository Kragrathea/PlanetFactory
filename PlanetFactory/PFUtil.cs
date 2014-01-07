using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace PlanetFactory
{
    public static class PFUtil
    {
        private static GameObject _localSpace;

        public static List<string> logLines;
        public static void Log(string line,LogType logType=LogType.Log)
        {
            PlanetFactory.print(line);
            try
            {
                DebugConsole.Log(line, logType);
                var writer=File.AppendText(PlanetFactory.DataPath + "Log.txt");
                writer.WriteLine(line);
                writer.Close();
                //File.WriteAllLines(PlanetFactory.DataPath + "Log.txt", new string[] { line });
            }
            catch
            {
            }
            //logLines.Add(line);
        }

        public static GameObject LocalSpace
        {
            get
            {
                if (_localSpace == null)
                    _localSpace = GameObject.Find("localSpace");
                return (_localSpace);
            }
        }

        public static GameObject FindLocal(string name)
        {
            try
            {
                return (LocalSpace.transform.FindChild(name).gameObject);
            }
            catch
            {
            }
            return null;
        }

        public static GameObject FindScaled(string name)
        {
            try
            {
                return (ScaledSpace.Instance.transform.FindChild(name).gameObject);
            }
            catch
            {
            }

            return null;
        }

        public static CelestialBody FindCB(string name)
        {
            try
            {
                return (FindLocal(name).GetComponent<CelestialBody>());
            }
            catch
            {
            }

            return null;
        }
        public static PlanetFactory.PFBody FindPFBody(string name)
        {
            try
            {
                return PlanetFactory.Instance.FindPFBody(name);
            }
            catch
            {
            }

            return null;
        }
        private static Texture2D _defaultTexture=null;
        public static Texture2D defaultTexture { get{
            if(_defaultTexture==null)
                _defaultTexture=LoadTexture("Default.png",true);
            return _defaultTexture;
        } }

        public static Texture2D LoadTexture(string name,bool embedded=false)
        {
            byte[] textureData = null;
            if (!embedded)
            {
                if (!File.Exists(name))
                {
                    PFUtil.Log(string.Format("Texture {0} not found. Using defaultTexture", name),LogType.Warning);
                    return defaultTexture;
                }
                textureData = File.ReadAllBytes(name);
            }
            else
            {
                System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlanetFactory." + name);
                var br = new BinaryReader(myStream);
                textureData = br.ReadBytes((int)myStream.Length);
            }
            var bytes = textureData.Skip(16).Take(4);
            ulong wid = bytes.Aggregate<byte, ulong>(0, (current, b) => (current * 0x100) + b);

            bytes = textureData.Skip(16 + 4).Take(4);
            ulong hei = bytes.Aggregate<byte, ulong>(0, (current, b) => (current * 0x100) + b);

            //Console.WriteLine("Loading Texture:"+name+"("+wid+"x"+hei+")");
            
            var texture = new Texture2D((int)wid, (int)hei, TextureFormat.ARGB32, true);
            texture.LoadImage(textureData);

            return texture;
        }

        public static void WarpShip(Vessel vessel, Orbit newOrbit)
        {
            if (newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude > newOrbit.referenceBody.sphereOfInfluence)
            {
                Log("Destination position was above the sphere of influence");
                return;
            }

            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = string.Empty;
            var parts = vessel.parts;
            if (parts != null)
            {
                var clamps = parts.Where(p => p.Modules != null && p.Modules.OfType<LaunchClamp>().Any()).ToList();
                foreach (var clamp in clamps)
                    clamp.Die();
            }

            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
            }

            foreach (var v in (FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels).Where(v => v.packed == false))
                v.GoOnRails();

            HardsetOrbit(vessel.orbit, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        public static void WarpPlanet(CelestialBody body, Orbit newOrbit)
        {
            var oldBody = body.referenceBody;
            HardsetOrbit(body.orbit, newOrbit);
            if (oldBody != newOrbit.referenceBody)
            {
                oldBody.orbitingBodies.Remove(body);
                newOrbit.referenceBody.orbitingBodies.Add(body);
            }
            body.CBUpdate();
        }

        public static void HardsetOrbit(Orbit orbit, Orbit newOrbit)
        {
            orbit.inclination = newOrbit.inclination;
            orbit.eccentricity = newOrbit.eccentricity;
            orbit.semiMajorAxis = newOrbit.semiMajorAxis;
            orbit.LAN = newOrbit.LAN;
            orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            orbit.epoch = newOrbit.epoch;
            orbit.referenceBody = newOrbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(Planetarium.GetUniversalTime());
        }

        public static void RecalculateTangents(Mesh theMesh)
        {

            int vertexCount = theMesh.vertexCount;
            Vector3[] vertices = theMesh.vertices;
            Vector3[] normals = theMesh.normals;
            Vector2[] texcoords = theMesh.uv;
            int[] triangles = theMesh.triangles;
            int triangleCount = triangles.Length/3;

            var tangents = new Vector4[vertexCount];
            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            int tri = 0;

            for (int i = 0; i < (triangleCount); i++)
            {

                int i1 = triangles[tri];
                int i2 = triangles[tri + 1];
                int i3 = triangles[tri + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = texcoords[i1];
                Vector2 w2 = texcoords[i2];
                Vector2 w3 = texcoords[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f/(s1*t2 - s2*t1);
                var sdir = new Vector3((t2*x1 - t1*x2)*r, (t2*y1 - t1*y2)*r, (t2*z1 - t1*z2)*r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;

                tri += 3;
            }
            for (int i = 0; i < (vertexCount); i++)
            {
                Vector3 n = normals[i];
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalize
                Vector3.OrthoNormalize(ref n, ref t);

                tangents[i].x = t.x;
                tangents[i].y = t.y;
                tangents[i].z = t.z;

                // Calculate handedness
                tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
            }
            theMesh.tangents = tangents;
        }
#if DEBUG
        public static Texture2D BumpToNormalMap(Texture2D source, float strength)
        {
            strength = Mathf.Clamp(strength, 0.0F, 10.0F);
            var result = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true);
            for (int by = 0; by < result.height; by++)
            {
                for (var bx = 0; bx < result.width; bx++)
                {
                    var xLeft = source.GetPixel(bx - 1, by).grayscale*strength;
                    var xRight = source.GetPixel(bx + 1, by).grayscale * strength;
                    var yUp = source.GetPixel(bx, by - 1).grayscale * strength;
                    var yDown = source.GetPixel(bx, by + 1).grayscale * strength;
                    var xDelta = ((xLeft - xRight) + 1) * 0.5f;
                    var yDelta = ((yUp - yDown) + 1) * 0.5f;
                    result.SetPixel(bx, by, new Color(xDelta, yDelta, 1.0f, xDelta));
                }
            }
            result.Apply();
            return result;
        }
#endif
    }

}

public static class Dump
{
    public static string GetGobPath(GameObject gob)
    {
        if (gob == null)
            return "";
        var parent = gob.transform.parent;
        var str = "" + gob.transform.name + "(" + gob.name + gob.GetInstanceID() + ")";
        while (parent != null)
        {
            str = parent.name + "(" + parent.gameObject.name + parent.gameObject.GetInstanceID() + ")" + "->" + str;
            parent = parent.transform.parent;
        }
        return str;
    }

    public static string ObjectToGui(object o, string name)
    {
        var t = o.GetType();
        //Console.WriteLine("Type:" + t);
        if (t == typeof(string) || t.IsValueType)
            return String.Format("({0}){1}={2}\n", t, name, o);
        var enumerable = o as IEnumerable;
        if (enumerable != null)
            return String.Format("({0}){1}={2}", t, name, "<Enum>\n");

        var str = "";
        foreach (var f in t.GetFields())
        {
            try
            {
                object val = f.GetValue(o);
                string typ = "<null>";
                if (val != null)
                    typ = val.GetType().ToString();
                str += String.Format("({0}){1}.({2}<field>){3}={4}\n", t, name, typ, f.Name, f.GetValue(o));
            }
            catch
            {
                str += String.Format("({0}){1}.({2}<field>){3}={4}\n", t, name, "Unk", f.Name, "ERROR");
            }
        }
        foreach (var p in t.GetProperties())
        {
            try
            {
                object val = p.GetValue(o, null);
                string typ = "<null>";
                if (val != null)
                    typ = val.GetType().ToString();

                str += String.Format("({0}){1}.({2}<prop>){3}={4}\n", t, name, typ, p.Name, p.GetValue(o, null));
            }
            catch
            {
                str += String.Format("({0}){1}.({2}<prop>){3}={4}\n", t, name, "Unk", p.Name, "ERROR");
            }
        }
        return str;
    }

    public static string DumpMods()
    {
        var lines = new List<string>();
        var all = Resources.FindObjectsOfTypeAll(typeof(PQSMod)) as PQSMod[];
        foreach (PQSMod obj in all)
        {
            var path = GetGobPath(obj.gameObject);

            if (path.Contains("Scatter"))
                continue;
            //PFUtil.Log(path);
            lines.Add(path);

            var tstr = "";
            tstr += DumpObject(obj, path,2); // path + "->");

            tstr = tstr.Replace("\r\r\n", "\r\n");
            lines.Add(tstr);
        }
        lines.Sort();
        var str = string.Join("\n", lines.Distinct().ToArray());
        return (str);
    }

    public static string DumpAll()
    {
        var lines = new List<string>();
        var all = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        foreach (GameObject obj in all)
        {
            var path = GetGobPath(obj);
            //if (path.Contains("KerbinSystem") || path.Contains("Mun") || path.Contains("Tylo"))// || path.Contains("Joker"))
            if (path.Contains("Kerbin") && path.Contains("localSpace")// || path.Contains("Minmus") || path.Contains("Vall")
                //path.Contains("Kerbin") |||| path.Contains("Dres") || path.Contains("Jool") || path.Contains("Laythe") || path.Contains("Eeloo")
                //|| path.Contains("CelestialBody")
                )
            {
                if (path.Contains("Scatter"))
                    continue;
                //PFUtil.Log(path);
                lines.Add(path);

                //var objStr = ObjectToGui(obj, "");
                //var objValues = objStr.Split('\n');
                //foreach (var v in objValues)
                //{
                //    lines.Add(path + "." + v);
                //}

                var comps = obj.GetComponents<Component>();
                var tstr = "";
                tstr += String.Format("\nlocalPos:{0} localScale{1}\n", obj.transform.localPosition, obj.transform.localScale);
                tstr += String.Format("\npos:{0} lossyScale{1}\n", obj.transform.position, obj.transform.lossyScale);
                foreach (Component c in comps)
                {
                    var typ = c.GetType().ToString();
                    tstr += c.name + "(" + typ + "),\n";
                    if (true //typ.Contains("Atmosphere")
                        //|| typ.Contains("CelestialBody")
                        )
                    {
                        //tstr += Dump.Graph(c
                        tstr += DumpComponent(c, path + "COMP:");
                        //var cobjStr = ObjectToGui(c, "");
                        //var cobjValues = objStr.Split('\n');
                        //foreach (var v in cobjValues)
                        //{
                        //    tstr += path + "COMP:" + c.name + "." + v + "\n";
                        //}
                    }
                }
                tstr = tstr.Replace("\r\r\n", "\r\n");
                lines.Add(path + ".COMPONENTS={" + tstr + "}");

                tstr = "";
                foreach (Transform c in obj.transform)
                {
                    tstr += String.Format("\nlocalPos:{0} localScale{1}\n", c.localPosition, c.localScale);
                    tstr += String.Format("\npos:{0} lossyScale{1}\n", c.position, c.lossyScale);
                    tstr += c.name + "(" + c.GetType() + "),";
                }
                lines.Add(path + ".XFORMS={" + tstr + "}");

                //lines.Add(Dump.Graph(obj, path + ".XFORMS={    "));
            }
        }
        lines.Sort();
        var str = string.Join("\n", lines.Distinct().ToArray());
        return (str);
    }

    public static string GetGob(string name = "Minmus")
    {
        var mm = GameObject.Find(name);
        return Graph(mm);
    }

    public static string Graph(Transform transform, string indent)
    {
        var outStr = "";
        outStr += (indent + "XF:" + transform.name + "<" + transform.GetType().Name + ">");
        outStr += "\n";


        foreach (Component component in transform.GetComponents<Component>())
        {
            outStr += DumpComponent(component, indent + "  ");
            outStr += "\n";

        }

        foreach (Transform child in transform.transform)
        {
            outStr += Graph(child.gameObject, indent + "  ");
            outStr += "\n";

        }
        return outStr;
    }

    public static string Graph(GameObject gameObject, string indent = "")
    {
        var outStr = "";
        outStr += (indent + gameObject.name + "<" + gameObject.GetType().Name + ">");
        outStr += "\n";


        foreach (Component component in gameObject.GetComponentsInChildren<Component>(true))
        {
            outStr += DumpComponent(component, indent + "  ");
            outStr += "\n";

        }

        foreach (Transform child in gameObject.transform)
        {
            outStr += Graph(child.gameObject, indent + "  ");
            outStr += "\n";

        }
        return outStr;
    }

    public static string DumpComponent(Component component, string indent)
    {
        var myName = component.name + "->" + (component.GetType().Name);
        var outStr = (indent + myName + "\n");
        outStr += DumpObject(component, indent + myName, 1);
        return outStr;
    }

    public static string DumpObject(object o, string name = "", int depth = 3)
    {
        //var v = new MyStuff.Debug.Foo();
        try
        {
            var leafprefix = "xxxx"; // (string.IsNullOrWhiteSpace(name) ? name : name + " = ");
            if (!string.IsNullOrEmpty(name))
                leafprefix = name + "=";

            if (null == o) return leafprefix + "null";

            var t = o.GetType();
            if (depth-- < 1 || t == typeof(string) || t.IsValueType)
                return leafprefix + "(" + t.Name + ")" + o;

            var sb = new StringBuilder();

            var enumerable = o as IEnumerable;
            if (enumerable != null)
            {
                name = (name ?? "").TrimEnd('[', ']') + '[';
                var elements = enumerable.Cast<object>().Select(e => DumpObject(e, "", depth)).ToList();
                var arrayInOneLine = elements.Count + "] = {" + string.Join(",", elements.ToArray()) + '}';
                if (!arrayInOneLine.Contains(Environment.NewLine)) // Single line?
                    return name + arrayInOneLine;
                var i = 0;
                foreach (var element in elements)
                {
                    var lineheader = name + i++ + ']';
                    sb.Append(lineheader)
                        .AppendLine(element.Replace(Environment.NewLine, Environment.NewLine + lineheader));
                }
                return sb.ToString();
            }
            foreach (var f in t.GetFields())
                try
                {
                    var pname = f.Name.ToLower();
                    //if (!pname.Contains("enable") && !pname.Contains("active"))
                    //    continue;

                    if (/*f.Name.ToLower() == "orbit" || f.Name.ToLower() == "material" ||
                        f.GetType() == typeof(Material) || f.Name.ToLower() == "orbitingbodies" ||*/ f.Name.ToLower().Contains("landclass")
                        || f.GetType().Name.ToLower().Contains("simplex"))
                        sb.AppendLine(DumpObject(f.GetValue(o), name + '.' + f.Name, depth + 2));
                    else
                        sb.AppendLine(DumpObject(f.GetValue(o), name + '.' + f.Name, depth));
                }
                catch
                {
                }
            foreach (var p in t.GetProperties())
                try
                {
                    var pname = p.Name.ToLower();
                    //if (pname == "targetrotation")
                    //    continue;
                    //if (!pname.Contains("enable") && !pname.Contains("active"))
                    //    continue;
                    if (/*p.Name.ToLower() == "orbit" || p.Name.ToLower() == "material" ||
                        p.GetType() == typeof(Material) || p.Name.ToLower() == "orbitingbodies" ||*/ p.Name.ToLower().Contains("landclass")
                        || p.GetType().Name.ToLower().Contains("simplex"))
                        sb.AppendLine(DumpObject(p.GetValue(o, null), name + '.' + p.Name, depth + 2));
                    else
                        sb.AppendLine(DumpObject(p.GetValue(o, null), name + '.' + p.Name, depth));
                }
                catch
                {
                }

            if (sb.Length == 0) return leafprefix + o;
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return name + "???";
        }
    }

    public static string MeshToString(MeshFilter mf)
    {
        Mesh m = mf.mesh;
        Material[] mats = mf.renderer.sharedMaterials;

        var sb = new StringBuilder();

        sb.Append("g ").Append(mf.name).Append("\n");
        foreach (Vector3 v in m.vertices)
        {
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.normals)
        {
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
        }
        return sb.ToString();
    }
}




