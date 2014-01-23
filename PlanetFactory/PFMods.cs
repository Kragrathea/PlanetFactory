using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class PQSMod_PFHeightColor : PQSMod
{
    public float blend = 1;
    public LandClass[] landClasses = null;
    public bool lerp = true;

    public override void OnSetup()
    {
        requirements = PQS.ModiferRequirements.MeshColorChannel;
        blend = 1f;
    }

    public override void OnVertexBuild(PQS.VertexBuildData data)
    {
        var height = (data.vertHeight - sphere.radiusMin) / (sphere.radiusMax - sphere.radiusMin);

        height = Mathf.Clamp((float)height, 0, 1);
        LandClass curLandClass = null;
        LandClass nextLandClass = null;
        for (var i = 0; i < landClasses.Length; i++)
        {
            var lc = landClasses[i];
            if (height >= lc.altStart && height <= lc.altEnd)
            {
                curLandClass = lc;
                if (lerp && i + 1 < landClasses.Length)
                    nextLandClass = landClasses[i + 1];

                break;
            }

        }
        if (curLandClass == null)
        {
            data.vertColor = Color.red;
        }
        else if (nextLandClass == null)
        {
            data.vertColor = Color.Lerp(data.vertColor, curLandClass.color, blend);
        }
        else
        {
            data.vertColor = Color.Lerp(data.vertColor, Color.Lerp(curLandClass.color, nextLandClass.color,
                (float)((height - curLandClass.altStart) / (curLandClass.altEnd - curLandClass.altStart))), blend);
        }
    }
    public class LandClass
    {
        public string name;
        public double altStart;
        public double altEnd;
        public Color color;
        public bool lerpToNext;

        public LandClass(string name, double fractalStart, double fractalEnd, Color baseColor)
        {
            this.name = name;
            this.altStart = fractalStart;
            this.altEnd = fractalEnd;
            this.color = baseColor;
        }
        public LandClass()
        {
        }
    }
}

public class PFVertexPlanetLandClass : PQSMod_VertexPlanet.LandClass
{
    public PFVertexPlanetLandClass() :
        base("Default", 0, 1, Color.cyan, Color.yellow, 0.2)
    {
        colorNoiseMap = new PQSMod_VertexPlanet.SimplexWrapper(1, 4, 0.6, 4);
        colorNoiseMap.Setup(666);
    }
}

public class PQSMod_PFOblate : PQSMod
{
    public double offset;
    public double power = 1.0;
    public PQSMod_PFOblate()
    {
    }

    public override void OnSetup()
    {
        this.requirements = PQS.ModiferRequirements.MeshCustomNormals;
    }
    public override double GetVertexMaxHeight()
    {
        return offset;
    }

    public override double GetVertexMinHeight()
    {
        return offset;
    }

    public override void OnVertexBuildHeight(PQS.VertexBuildData vbData)
    {
        var hei = Math.Sin(3.14159265358979 * vbData.v);
        hei = Math.Pow(hei, power);
        vbData.vertHeight = vbData.vertHeight + hei * offset;
    }
}

public class PQSMod_PFOffset : PQSMod
{
    public double offset;
    public PQSMod_PFOffset()
    {
    }

    public override void OnSetup()
    {
        this.requirements = PQS.ModiferRequirements.MeshCustomNormals;
    }
    public override double GetVertexMaxHeight()
    {
        return offset;
    }

    public override double GetVertexMinHeight()
    {
        return offset;
    }

    public override void OnVertexBuildHeight(PQS.VertexBuildData vbData)
    {
        vbData.vertHeight = vbData.vertHeight + offset;
    }
}

public class PQSMod_PFDebug : PQSMod
{
    public double offset = 0.0;
    public double minAlt = double.MaxValue;
    public PQSMod_PFDebug()
    {
    }

    public override void OnSetup()
    {
        this.requirements = PQS.ModiferRequirements.MeshCustomNormals;
    }
    public override double GetVertexMaxHeight()
    {
        return offset;
    }

    public override double GetVertexMinHeight()
    {
        return offset;
    }

    public override void OnVertexBuild(PQS.VertexBuildData data)
    {
        if (data.vertHeight < sphere.radius + 5)
        {
            data.vertColor = Color.red;
        }
    }

    public override void OnVertexBuildHeight(PQS.VertexBuildData vbData)
    {
        if (vbData.vertHeight < minAlt)
        {
            minAlt = vbData.vertHeight;
            //print("new minAlt "+minAlt);
        }
    }
}

