using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanetFactory
{
    static class PFEffects
    {
        public static void AddRing(GameObject smallPlanet, double innerRadius, double outerRadius, float tilt, Texture2D ringTexture)
        {
            //print("Adding Ring:" + smallPlanet.name);
            var vect = new Vector3(1, 0, 0);
            var steps = 64;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var normals = new List<Vector3>();
            for (var r = 0.0f; r < 360.0f; r += 360.0f / steps)
            {
                var rv = Quaternion.Euler(0, r, 0) * vect;

                verts.Add(rv * (float)innerRadius);
                normals.Add(-Vector3.right);
                uvs.Add(new Vector2(0, 0));

                verts.Add(rv * (float)outerRadius);
                normals.Add(-Vector3.right);
                uvs.Add(new Vector2(1, 1));

            }
            for (var r = 0.0f; r < 360.0f; r += 360.0f / steps)
            {
                var rv = Quaternion.Euler(0, r, 0) * vect;

                verts.Add(rv * (float)innerRadius);
                normals.Add(-Vector3.right);
                uvs.Add(new Vector2(0, 0));

                verts.Add(rv * (float)outerRadius);
                normals.Add(-Vector3.right);
                uvs.Add(new Vector2(1, 1));

            }
            var wrapAt = (steps * 2);
            for (var t = 0; t < (steps * 2); t += 2)
            {
                tris.Add((t + 0) % wrapAt);
                tris.Add((t + 1) % wrapAt);
                tris.Add((t + 2) % wrapAt);

                tris.Add((t + 1) % wrapAt);
                tris.Add((t + 3) % wrapAt);
                tris.Add((t + 2) % wrapAt);

            }
            for (var t = 0; t < (steps * 2); t += 2)
            {

                tris.Add(wrapAt + ((t + 2) % wrapAt));
                tris.Add(wrapAt + ((t + 1) % wrapAt));
                tris.Add(wrapAt + ((t + 0) % wrapAt));

                tris.Add(wrapAt + ((t + 2) % wrapAt));
                tris.Add(wrapAt + ((t + 3) % wrapAt));
                tris.Add(wrapAt + ((t + 1) % wrapAt));

            }

            var rgob = new GameObject {name = "Ring"};
            rgob.transform.parent = smallPlanet.transform;
            rgob.transform.position = smallPlanet.transform.localPosition;
            rgob.transform.localRotation = Quaternion.Euler(tilt, 0, 0);

            rgob.transform.localScale = smallPlanet.transform.localScale;
            rgob.layer = smallPlanet.layer;


            var ringMeshFilter = rgob.AddComponent<MeshFilter>();

            ringMeshFilter.mesh = new Mesh();
            ringMeshFilter.mesh.vertices = verts.ToArray();
            ringMeshFilter.mesh.triangles = tris.ToArray();
            ringMeshFilter.mesh.uv = uvs.ToArray();
            ringMeshFilter.mesh.RecalculateNormals();
            ringMeshFilter.mesh.RecalculateBounds();
            ringMeshFilter.mesh.Optimize();
            ringMeshFilter.sharedMesh = ringMeshFilter.mesh;

            //var otherSmallPlanet = ScaledSpace.Instance.transform.FindChild("Dena").gameObject;

            var otherMeshRenderer = (MeshRenderer)smallPlanet.GetComponentInChildren((typeof(MeshRenderer)));
            var smallPlanetMeshRenderer = rgob.AddComponent<MeshRenderer>();
            smallPlanetMeshRenderer.material = otherMeshRenderer.material;

            //var colorTexture = loadTexture(DataPath + smallPlanet.name + "_ring.png");
            smallPlanetMeshRenderer.material.mainTexture = ringTexture;
            var color = smallPlanetMeshRenderer.material.color;
            color.a = 0.60f;
            smallPlanetMeshRenderer.material.color = color;
        }

        public static GameObject AddEffect(GameObject smallPlanet, string effectName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MonoBehaviour.print(String.Format("Adding effect {0} to {1}", effectName, smallPlanet.name));
            var effect = (GameObject)Object.Instantiate(Resources.Load("Effects/" + effectName));

            effect.transform.parent = smallPlanet.transform;
            effect.transform.localPosition = position;
            effect.transform.localRotation = rotation;// smallPlanet.transform.localRotation;
            effect.layer = smallPlanet.layer;

            effect.transform.localScale = scale;
            MonoBehaviour.print("Finished effect");
            return (effect);
        }

        public static void TestEffects(GameObject smallPlanet)
        {
            var fxNames = new[]
            {
                "fx_exhaustFlame_blue",
                "fx_exhaustFlame_blue_small",
                "fx_exhaustFlame_white_tiny",
                "fx_exhaustFlame_yellow",
                "fx_exhaustFlame_yellow_small",
                "fx_exhaustFlame_yellow_tiny",
                "fx_exhaustLight_blue",
                "fx_exhaustLight_yellow",
                "fx_exhaustSparks_flameout",
                "fx_exhaustSparks_yellow",
                "fx_gasBurst_white",
                "fx_gasJet_tiny",
                "fx_gasJet_white",
                "fx_smokeTrail_light",
                "fx_smokeTrail_medium"
            };

            float r = 0;
            foreach (var n in fxNames)
            {
                AddEffect(smallPlanet,  n,
                    Quaternion.Euler(0, r, 0)*new Vector3(0f, 0f, 1100f),
                    Quaternion.Euler(-90, 0, 0),
                    new Vector3(20000, 20000, 20000));
                r += 360.0f/fxNames.Length;
            }
        }

        public static void AddCraters(GameObject localPlanet, int seed, float deformation)
        {
            //print("Cratering");
            //var mun = PFUtil.FindLocal("Mun");
            //var mcraters = mun.GetComponentsInChildren<PQSMod_VoronoiCraters>()[0];
            //var ecg = (GameObject)GameObject.Instantiate(mcraters.gameObject);

            //ecg.transform.parent = vp.gameObject.transform;

            //var ec = localPlanet.GetComponentInChildren<PQSMod_VoronoiCraters>();
            //ec.deformation = ec.deformation * 0.7;
            //ec.simplexSeed = seed;
            //ec.voronoiSeed = -seed;

            //ec.sphere = vp.sphere;
            //ec.RebuildSphere();
        }
    }
}
