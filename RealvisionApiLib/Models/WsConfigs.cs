using System;
using System.Collections.Generic;
using System.Text;

namespace REALvisionApiLib
{

    public class WsConfigs
    {
        public Printer Printer { get; set; }
        public Model[] Models { get; set; }
        public string SelectedSupport { get; set; }
    }

    public class Printer
    {
        public int[] Dummy { get; set; }
    }

    public class Model
    {
        public Metadata metaData { get; set; }
        public Position position { get; set; }
        public Scale scale { get; set; }
        public float rotationX { get; set; }
        public float rotationY { get; set; }
        public float rotationZ { get; set; }
        public bool FlipedX { get; set; }
        public bool FlipedY { get; set; }
        public bool FlipedZ { get; set; }
        public Boundsposition[] BoundsPositions { get; set; }
        public int[] BoundsIndices { get; set; }
        public Modelposition[] ModelPositions { get; set; }
        public int[] ModelIndices { get; set; }
    }

    public class Metadata
    {
        public bool IsGroup { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int PrintingMaterial { get; set; }
        public string Watertight { get; set; }
    }

    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Scale
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Boundsposition
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Modelposition
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

}
