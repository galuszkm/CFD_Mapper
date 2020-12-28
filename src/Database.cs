using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using Kitware.VTK;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace CFD_Mapper
{
    public class Database
    {
        public Dictionary<int, Node> Nodes { get; set; }
        public Dictionary<int, Element> Elements { get; set; }
        public List<Face> Faces { get; set; }
        public List<CFD_Point> CFD { get; set; }
        public string Errors { get; set; }
        public List<string> Loadcase { get; set; }

        public double[] PressureRange;                 // Pressue range to set color scale
        public double[] PressureMappedRange;           // Mapped pressue range to set color scale

        // FEM vtk objects
        private vtkPoints PointsFEM;
        private vtkUnsignedCharArray ColorDataFEM;
        private vtkUnstructuredGrid GridFEM;
        private vtkDataSetMapper MapperFEM;
        public vtkActor ActorFEM;

        // CFD vtk objects
        private vtkPoints PointsCFD;
        private vtkUnsignedCharArray ColorDataCFD;
        private vtkPolyData GridCFD;
        private vtkCellArray Vertices;
        private vtkPolyDataMapper MapperCFD;
        public vtkActor ActorCFD;

        // Others
        public float SizeCFD;



        public Database(string pathFEM, string pathCFD, string solver, string units, RenderInterface iRen, float sizeCFD)
        {
            // Read input file
            string[] data = File.ReadAllLines(pathFEM);

            // Create Mesh
            Nodes = new Dictionary<int, Node>();
            Elements = new Dictionary<int, Element>();
            Faces = new List<Face>();
            Errors = "";

            if (solver == "Marc") Create_Mesh_Marc(data);
            if (solver == "LSDyna") Create_Mesh_LSDyna(data);

            // Create CFD points
            data = File.ReadAllLines(pathCFD);
            SizeCFD = sizeCFD;
            CFD = new List<CFD_Point>();
            foreach (string s in data)
            {
                try
                {
                    CFD.Add(new CFD_Point(s, units));
                }
                catch
                {
                    Errors += "    Error: Cannot read CFD input line: " + s + "\n";
                }
            }

            // Detect Loadcases in MSC Marc
            if (solver == "Marc")
            {
                Loadcase = new List<string>();
                foreach (string i in data)
                {
                    if (i.StartsWith("$....start of loadcase")) Loadcase.Add(i.Substring(22).Replace(" ", ""));
                }
            }

            // Set pressure range
            PressureRange = GetPressureRange();
            iRen.ChangeColorRange(PressureRange[0], PressureRange[1]);

            // Create Actors
            Create_ActorFEM();
            Create_ActorCFD(iRen);
            iRen.AddActor(ActorFEM);
            iRen.AddActor(ActorCFD);
        }

        private void Create_Mesh_Marc(string[] data)
        {
            Nodes = new Dictionary<int, Node>();
            Elements = new Dictionary<int, Element>();
            Faces = new List<Face>();

            // Find Elements
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].StartsWith("connectivity"))
                {
                    for (int j = i + 2; j < data.Length; j++)
                    {
                        if (data[j].StartsWith("coordinates")) { i = j - 1; break; }
                        try
                        {
                            Element temp = new Element(data[j]);
                            Elements.Add(temp.ID, temp);
                        }
                        catch
                        {
                            if (!data[j].StartsWith("connectivity") && !data[j].StartsWith("         0"))
                            {
                                Errors += "    Error: ELEMENT input in line: " + data[j] + "\n";
                            }
                        }

                    }
                }
            }

            // Find Nodes
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].StartsWith("coordinates"))
                {
                    for (int j = i + 2; j < data.Length; j++)
                    {
                        if (!data[j].StartsWith(" ")) { i = j - 1; break; }

                        Node temp = new Node(data[j], "Marc");
                        Nodes.Add(temp.ID, temp);

                    }
                }
            }

            // Find Faces
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].StartsWith("define              facemt              set                 Pressure_faces"))
                {
                    int index = 0;
                    for (int j = i + 1; j < data.Length; j++)
                    {
                        if (!data[j].StartsWith(" ")) { i = data.Length; break; }

                        for (int k = 0; k < Math.Floor((decimal)(data[j].Length / 18)); k++)
                        {
                            Face temp = new Face(data[j].Substring(18 * k, 18).Replace(" ", ""), index, "Marc");
                            temp.Assign_Node_Marc(Elements);
                            Faces.Add(temp);
                            index++;
                        }

                    }
                }
            }
        }

        private void Create_Mesh_LSDyna(string[] data)
        {
            // Find Nodes
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].StartsWith("*NODE"))
                {
                    for (int j = i + 1; j < data.Length; j++)
                    {
                        if (data[j].StartsWith("*")) { i = j - 1; break; }
                        if (!string.IsNullOrWhiteSpace(data[j]) && !data[j].StartsWith("$"))
                        {
                            try
                            {
                                Node temp = new Node(data[j], "LSDyna");
                                Nodes.Add(temp.ID, temp);
                            }
                            catch
                            {
                                Errors += "    Error: NODE input in line:  " + data[j] + "\n";
                            }
                        }
                    }
                }
            }

            // Find Faces
            for (int i = 0; i < data.Length; i++)
            {
                bool stop = false;
                if (data[i].StartsWith("*SET_SEGMENT"))
                {
                    int index = 0;
                    for (int j = i + 1; j < data.Length; j++)
                    {
                        if (data[j].StartsWith("*"))
                        {
                            stop = true;
                            break;
                        }
                        if (!string.IsNullOrWhiteSpace(data[j]) && !data[j].StartsWith("$") && !data[j].Contains("MECH"))
                        {
                            try
                            {
                                Face temp = new Face(data[j], index, "LSDyna");
                                Faces.Add(temp);
                                index++;
                            }
                            catch
                            {
                                Errors += "    Error: SEGMENT input in line:  " + data[j] + "\n";
                            }
                        }
                    }
                }
                if (stop) break;
            }
        }

        private void Create_ActorFEM()
        {
            // Create vtkPoints
            PointsFEM = vtkPoints.New();
            Dictionary<int, int> Index = new Dictionary<int, int>();  // Connects Node ID and it's vtkPoint index

            int index = 0;
            foreach(int i in Nodes.Keys)
            {
                PointsFEM.InsertNextPoint(Nodes[i].X, Nodes[i].Y, Nodes[i].Z);
                Index.Add(Nodes[i].ID, index);
                index++;
            }

            GridFEM = vtkUnstructuredGrid.New();
            GridFEM.SetPoints(PointsFEM);

            // Add Face mesh
            for (int f = 0; f < Faces.Count; f++)
            {
                Face face = Faces[f];

                if (face.Nodes.Count == 4)
                {
                    vtkQuad quad = vtkQuad.New();
                    quad.GetPointIds().SetNumberOfIds(4);
                    for (int i = 0; i < 4; i++)
                    {
                        quad.GetPointIds().SetId(i, Index[face.Nodes[i]]);
                    }
                    GridFEM.InsertNextCell(quad.GetCellType(), quad.GetPointIds());
                }
                if (face.Nodes.Count == 3)
                {
                    vtkTriangle triangle = vtkTriangle.New();
                    triangle.GetPointIds().SetNumberOfIds(3);
                    for (int i = 0; i < 3; i++)
                    {
                        triangle.GetPointIds().SetId(i, Index[face.Nodes[i]]);
                    }
                    GridFEM.InsertNextCell(triangle.GetCellType(), triangle.GetPointIds());
                }
            }

            MapperFEM = vtkDataSetMapper.New();
            MapperFEM.SetInput(GridFEM);

            ActorFEM = new vtkActor();
            ActorFEM.SetMapper(MapperFEM);
            ActorFEM.GetProperty().SetColor(1, 1, 1);
            ActorFEM.GetProperty().SetEdgeVisibility(0);
            ActorFEM.VisibilityOff();

        }

        private void Create_ActorCFD(RenderInterface iRen)
        {
            PointsCFD = vtkPoints.New();
            Vertices = vtkCellArray.New();
            ColorDataCFD = vtkUnsignedCharArray.New();
            ColorDataCFD.SetNumberOfComponents(3);

            // Create vtkPoints
            for (int i = 0; i < CFD.Count; i++)
            {
                PointsCFD.InsertNextPoint(CFD[i].X, CFD[i].Y, CFD[i].Z);
                double[] dcolor = iRen.Get_ColorTable().GetColor(CFD[i].P);
                ColorDataCFD.InsertNextTuple3(dcolor[0] * 255, dcolor[1] * 255, dcolor[2] * 255);
                vtkVertex vertex = vtkVertex.New();
                vertex.GetPointIds().SetId(0, i);
                Vertices.InsertNextCell(vertex);
            }

            GridCFD = vtkPolyData.New();
            GridCFD.SetPoints(PointsCFD);
            GridCFD.GetPointData().SetScalars(ColorDataCFD);
            GridCFD.SetVerts(Vertices);

            MapperCFD = vtkPolyDataMapper.New();
            MapperCFD.SetInput(GridCFD);
            MapperCFD.ScalarVisibilityOn();

            ActorCFD = vtkActor.New();
            ActorCFD.SetMapper(MapperCFD);
            ActorCFD.GetProperty().SetPointSize(SizeCFD);
            ActorCFD.VisibilityOff();
        }

        public void Clip_Model(bool clip, RenderInterface iRen)
        {
            if (clip == true)
            {
                ActorFEM.GetMapper().AddClippingPlane(iRen.Get_ClipPlane());
                ActorCFD.GetMapper().AddClippingPlane(iRen.Get_ClipPlane());
            }
            else
            {
                ActorFEM.GetMapper().RemoveClippingPlane(iRen.Get_ClipPlane());
                ActorCFD.GetMapper().RemoveClippingPlane(iRen.Get_ClipPlane());
            }
        }

        public void Update_ColorCFD(RenderInterface iRen)
        {
            ColorDataCFD = vtkUnsignedCharArray.New();
            ColorDataCFD.SetNumberOfComponents(3);

            // Assign colors
            for (int i = 0; i < CFD.Count; i++)
            {
                double[] dcolor = iRen.Get_ColorTable().GetColor(CFD[i].P);
                ColorDataCFD.InsertNextTuple3(dcolor[0] * 255, dcolor[1] * 255, dcolor[2] * 255);
            }
            GridCFD.GetPointData().SetScalars(ColorDataCFD);
        }

        public void Update_ColorFEM(RenderInterface iRen)
        {
            ColorDataFEM = vtkUnsignedCharArray.New();
            ColorDataFEM.SetNumberOfComponents(3);

            // Assign colors
            for (int i=0; i< Faces.Count; i++)
            {
                double[] dcolor = new double[3] { 1.0, 1.0, 1.0 };

                if (Faces[i].NoPressure == false)
                {
                    dcolor = iRen.Get_ColorTable().GetColor(Faces[i].P);
                }
                ColorDataFEM.InsertNextTuple3(dcolor[0] * 255.0, dcolor[1] * 255.0, dcolor[2] * 255.0);
            }
            GridFEM.GetCellData().SetScalars(ColorDataFEM);
        }

        public void Clear_ColorFEM()
        {
            ColorDataFEM = vtkUnsignedCharArray.New();
            ColorDataFEM.SetNumberOfComponents(3);

            // Assign colors
            for (int i = 0; i < Faces.Count; i++)
            {
                ColorDataFEM.InsertNextTuple3(255.0, 255.0, 255.0);
            }
            GridFEM.GetCellData().SetScalars(ColorDataFEM);
        }

        private double[] GetPressureRange()
        {
            List<double> p = new List<double>();
            foreach (CFD_Point c in CFD)
            {
                p.Add(c.P);
            }
            double[] range = new double[2] { p.Min(), p.Max() };

            return range;
        }

        public double[] GetPressureMappedRange()
        {
            List<double> p = new List<double>();
            foreach (Face f in Faces)
            {
                if (f.NoPressure == false)
                {
                    p.Add(f.P);
                }
            }
            double[] range = new double[2] { p.Min(), p.Max() };

            return range;
        }

    }

    public class Node
    {
        public int ID { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        Functions fun = new Functions();

        public Node(string input, string solver)
        {
            List<string> data = new List<string>();

            if (solver == "Marc")
            {
                data.Add(input.Substring(0, 10).Replace(" ", ""));
                data.Add(input.Substring(10, 20).Replace(" ", ""));
                data.Add(input.Substring(30, 20).Replace(" ", ""));
                data.Add(input.Substring(50, 20).Replace(" ", ""));

                for (int i = 1; i < 4; i++)
                {
                    if (data[i].Substring(1).Contains("+"))
                        data[i] = fun.BeforeLast(data[i], "+") + "E+" + fun.After(data[i].Substring(1), "+");
                    if (data[i].Substring(1).Contains("-"))
                        data[i] = fun.BeforeLast(data[i], "-") + "E-" + fun.After(data[i].Substring(1), "-");
                }

                ID = int.Parse(data[0]);
                X = double.Parse(data[1], CultureInfo.InvariantCulture);
                Y = double.Parse(data[2], CultureInfo.InvariantCulture);
                Z = double.Parse(data[3], CultureInfo.InvariantCulture);
            }

            if (solver == "LSDyna")
            {
                data.Add(input.Substring(0, 8).Replace(" ", ""));
                data.Add(input.Substring(8, 16).Replace(" ", ""));
                data.Add(input.Substring(24, 16).Replace(" ", ""));
                data.Add(input.Substring(40, 16).Replace(" ", ""));

                ID = int.Parse(data[0]);
                X = double.Parse(data[1], CultureInfo.InvariantCulture);
                Y = double.Parse(data[2], CultureInfo.InvariantCulture);
                Z = double.Parse(data[3], CultureInfo.InvariantCulture);
            }
        }
    }

    public class Element
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public int Type_No { get; set; }
        public List<int> Nodes { get; set; }
        public List<int[]> Face { get; set; }

        public Element(string input)
        {
            List<string> data = new List<string>();
            Face = new List<int[]>();

            if (input.Length == 100) Type = "HEXA";
            if (input.Length == 80) Type = "PENTA";
            if (input.Length == 60 || input.Length == 120) Type = "TETRA";

            if (Type == "HEXA")
            {
                for (int i = 0; i < 10; i++)
                {
                    data.Add(input.Substring(10 * i, 10).Replace(" ", ""));
                }
            }
            if (Type == "PENTA")
            {
                for (int i = 0; i < 8; i++)
                {
                    data.Add(input.Substring(10 * i, 10).Replace(" ", ""));
                }
            }
            if (Type == "TETRA")
            {
                for (int i = 0; i < 6; i++)
                {
                    data.Add(input.Substring(10 * i, 10).Replace(" ", ""));
                }
            }

            ID = int.Parse(data[0]);
            Type_No = int.Parse(data[1]);

            Nodes = new List<int>();
            for (int i = 2; i < data.Count; i++) Nodes.Add(int.Parse(data[i]));
            Nodes = Nodes.Distinct().ToList();

            if (Type == "HEXA")
            {
                Face.Add(new int[4] { Nodes[0], Nodes[1], Nodes[5], Nodes[4] });
                Face.Add(new int[4] { Nodes[1], Nodes[2], Nodes[6], Nodes[5] });
                Face.Add(new int[4] { Nodes[2], Nodes[3], Nodes[7], Nodes[6] });
                Face.Add(new int[4] { Nodes[3], Nodes[0], Nodes[4], Nodes[7] });
                Face.Add(new int[4] { Nodes[0], Nodes[1], Nodes[2], Nodes[3] });
                Face.Add(new int[4] { Nodes[5], Nodes[4], Nodes[7], Nodes[6] });
            }
            if (Type == "PENTA")
            {
                Face.Add(new int[4] { Nodes[0], Nodes[1], Nodes[4], Nodes[3] });
                Face.Add(new int[4] { Nodes[1], Nodes[2], Nodes[5], Nodes[4] });
                Face.Add(new int[4] { Nodes[2], Nodes[0], Nodes[3], Nodes[5] });
                Face.Add(new int[3] { Nodes[0], Nodes[2], Nodes[1] });
                Face.Add(new int[3] { Nodes[3], Nodes[4], Nodes[5] });
            }
            if(Type == "TETRA")
            {
                Face.Add(new int[3] { Nodes[0], Nodes[1], Nodes[3]});
                Face.Add(new int[3] { Nodes[1], Nodes[2], Nodes[3] });
                Face.Add(new int[3] { Nodes[2], Nodes[0], Nodes[3] });
                Face.Add(new int[3] { Nodes[0], Nodes[1], Nodes[2] });
            }
        }
    }

    public class Face
    {
        public int Index { get; }
        public int EID { get; set; }
        public int Face_No { get; set; }
        public List<int> Nodes { get; }
        public Point3D Centroid { get; set; }
        public double R { get; set; }
        public double P { get; set; }
        public bool NoPressure { get; set; }
        public double[] Bounds { get; set; }

        Functions fun = new Functions();

        public Face(string input, int index, string solver)
        {
            Index = index;
            Nodes = new List<int>();
            Centroid = new Point3D();
            NoPressure = true;


            if (solver == "Marc")
            {
                EID = int.Parse(fun.Before(input, ":"));
                Face_No = int.Parse(fun.After(input, ":"));
            }
            if (solver == "LSDyna")
            {
                Nodes.Add(int.Parse(input.Substring(0, 10).Replace(" ", "")));
                Nodes.Add(int.Parse(input.Substring(10, 10).Replace(" ", "")));
                Nodes.Add(int.Parse(input.Substring(20, 10).Replace(" ", "")));
                if (input.Substring(20, 10).Replace(" ", "") != input.Substring(30, 10).Replace(" ", ""))
                {
                    Nodes.Add(int.Parse(input.Substring(30, 10).Replace(" ", "")));
                }
            }
       }

        public void Assign_Node_Marc(Dictionary<int, Element> Elem)
        {
            foreach (int n in Elem[EID].Face[Face_No])
            {
                Nodes.Add(n);
            }
        }

        public void Calculate_Prop(Dictionary<int, Node> NodeLib, double RadiusScale)
        {
            // Calculate Centroid
            if (Nodes.Count == 4) Centroid = fun.QuadCenter(Nodes, NodeLib);
            if (Nodes.Count == 3) Centroid = fun.TriangleCenter(Nodes[0], Nodes[1], Nodes[2], NodeLib);

            // Calculate initial Face radius
            R = fun.Rad(Nodes, Centroid, NodeLib) * RadiusScale;

            // Define box sourrounding face
            // Minimal and Maximal coordinates are taken from nodes
            Bounds = new double[6];
            foreach (int n in Nodes)
            {
                if (NodeLib[n].X - R * 0.5 < Bounds[0]) Bounds[0] = NodeLib[n].X - R * 0.5;
                if (NodeLib[n].X + R * 0.5 > Bounds[1]) Bounds[1] = NodeLib[n].X + R * 0.5;
                if (NodeLib[n].Y - R * 0.5 < Bounds[2]) Bounds[2] = NodeLib[n].Y - R * 0.5;
                if (NodeLib[n].Y + R * 0.5 > Bounds[3]) Bounds[3] = NodeLib[n].Y + R * 0.5;
                if (NodeLib[n].Z - R * 0.5 < Bounds[4]) Bounds[4] = NodeLib[n].Z - R * 0.5;
                if (NodeLib[n].Z + R * 0.5 > Bounds[5]) Bounds[5] = NodeLib[n].Z + R * 0.5;
            }
        }

    }

    public class CFD_Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double P { get; set; }

        public CFD_Point(string input, string units)
        {
            List<string> data = new List<string>();

            if (data.Count < 4)
            {
                data = Regex.Split(input, @"\t").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            if (data.Count < 4)
            {
                data = Regex.Split(input, @"[,]").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            if (data.Count < 4)
            {
                data = Regex.Split(input, @"\s").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            X = double.Parse(data[0], CultureInfo.InvariantCulture);
            Y = double.Parse(data[1], CultureInfo.InvariantCulture);
            Z = double.Parse(data[2], CultureInfo.InvariantCulture);
            P = double.Parse(data[3], CultureInfo.InvariantCulture);

            if (units == "m, Pa") { X = X * 1000; Y = Y * 1000; Z = Z * 1000; P = P * 0.000001; }
            if (units == "m, MPa") { X = X * 1000; Y = Y * 1000; Z = Z * 1000; }
            if (units == "m, kPa") { X = X * 1000; Y = Y * 1000; Z = Z * 1000; P = P * 0.001; }
            if (units == "m, mPa") { X = X * 1000; Y = Y * 1000; Z = Z * 1000; P = P * 0.000000001; }
            if (units == "mm, MPa") { }
            if (units == "mm, kPa") { P *= 0.001; }
            if (units == "mm, Pa") { P *= 0.000001; }
            if (units == "mm, mPa") { P *= 0.000000001; }

        }
    }

}