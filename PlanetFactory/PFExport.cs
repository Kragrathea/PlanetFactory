using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace PlanetFactory
{
    static class PFExport
    {
#if DEBUG
        public static void CreateScaledSpacePlanet(string name, string templateName, GameObject body = null, int mapWidth = 2048,int maxHeight=20000)
        {
            //print("Creating new scaledPlanet " + name);

            var hasOcean = false;
            var oceanHeight = 0.0;
            var exportBin = true;
            var removeAlpha = true;

            var configRoot = ConfigNode.Load(PlanetFactory.CurrentPath + name + ".cfg");
            if (configRoot != null && configRoot.HasNode("ScaledExport"))
            {
                PFUtil.Log("Loading ScaledExport");
                var node=configRoot.GetNode("ScaledExport");
                if(node.HasValue("templateName"))
                    templateName = node.GetValue("templateName");
                if (node.HasValue("mapWidth"))
                    mapWidth =int.Parse(node.GetValue("mapWidth"));
                if (node.HasValue("maxHeight"))
                    maxHeight =int.Parse(node.GetValue("maxHeight"));

                if (node.HasValue("hasOcean"))
                    hasOcean = bool.Parse(node.GetValue("hasOcean"));
                if (node.HasValue("oceanHeight"))
                    oceanHeight = double.Parse(node.GetValue("oceanHeight"));

                if (node.HasValue("exportBin"))
                    exportBin = bool.Parse(node.GetValue("exportBin"));

                if (node.HasValue("removeAlpha"))
                    removeAlpha = bool.Parse(node.GetValue("removeAlpha"));

                PFUtil.Log("Loaded ScaledExport");

            }

            //print("templateName:" + templateName);

            var template = GameObject.Find(templateName);

            if (body == null)
                body = GameObject.Find(name);

            var bodyPQS = body.GetComponentInChildren<PQS>();

            var templatePQS = template.GetComponentInChildren<PQS>();

            if (exportBin)
            {
                var scaledSpace = GameObject.Find("scaledSpace");
                var smallMinmus = scaledSpace.transform.Find(templateName);

                var smallTemplateMeshFilter = (MeshFilter)smallMinmus.GetComponentInChildren((typeof(MeshFilter)));
                var originalVert = smallTemplateMeshFilter.mesh.vertices[0];
                var originalHeight = (float)templatePQS.GetSurfaceHeight(originalVert);
                var scale = originalHeight / originalVert.magnitude;
                //MonoBehaviour.print("Hei:" + originalHeight + " Mag:" + originalVert.magnitude + " Scale:" + scale);
                bodyPQS.isBuildingMaps = true;

                var newVerts = new Vector3[smallTemplateMeshFilter.mesh.vertices.Count()];
                for (int i = 0; i < smallTemplateMeshFilter.mesh.vertices.Count(); i++)
                {
                    var vertex = smallTemplateMeshFilter.mesh.vertices[i];
                    var rootrad = (float)Math.Sqrt(vertex.x * vertex.x +
                                    vertex.y * vertex.y +
                                    vertex.z * vertex.z);
                    var radius = (float)bodyPQS.GetSurfaceHeight(vertex) / scale;
                    //radius = 1000;
                    newVerts[i] = vertex * (radius / rootrad);
                }
                bodyPQS.isBuildingMaps = false;

                var binFileName = PlanetFactory.CurrentPath + name + "_.bin";
                //print("Writing scaledPlanet");
                var stream = File.Open(binFileName,FileMode.Create);
                var writer = new BinaryWriter(stream);
                //var writer = KSP.IO.BinaryWriter.CreateForType<PlanetFactory>(PlanetFactory.CurrentPath+ name + "_.bin");
                foreach (var v in newVerts)
                {
                    writer.Write(v.x);
                    writer.Write(v.y);
                    writer.Write(v.z);
                }
                writer.Close();
            }
            
            var textures = bodyPQS.CreateMaps(mapWidth, maxHeight, hasOcean, oceanHeight, bodyPQS.mapOceanColor);//new Color(0.2f,0.2f,0.6f,0.5f));

            //print("Writing textures");
            if (removeAlpha)
            {
                for (int i = 0; i < textures[0].width * textures[0].height; i++)
                {
                    var c = textures[0].GetPixel(i % textures[0].width, i / textures[0].width);
                    c.a = 1.0f;
                    textures[0].SetPixel(i % textures[0].width, i / textures[0].width, c);
                }
            }
            var mapBytes = textures[0].EncodeToPNG();
            File.WriteAllBytes(PlanetFactory.CurrentPath + name + "_map_.png", mapBytes);

            //mapBytes = textures[1].EncodeToPNG();
            //File.WriteAllBytes(PlanetFactory.CurrentPath + name + "_bump_.png", mapBytes);

            var normalMap = PFUtil.BumpToNormalMap(textures[1], 9);
            mapBytes = normalMap.EncodeToPNG();
            File.WriteAllBytes(PlanetFactory.CurrentPath + name + "_normal_.png", mapBytes);

        }
#endif
    }
}
