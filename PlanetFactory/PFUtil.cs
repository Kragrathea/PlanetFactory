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
                    return defaultTexture;
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





