using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Kitware.VTK;

namespace CFD_Mapper
{
    public class Functions
    {
        public string Between(string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            int posB = value.LastIndexOf(b);
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        public string Before(string value, string a)
        {
            int posA = value.IndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            return value.Substring(0, posA);
        }

        public string BeforeLast(string value, string a)
        {
            int posA = value.LastIndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            return value.Substring(0, posA);
        }

        public string After(string value, string a)
        {
            int posA = value.LastIndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= value.Length)
            {
                return "";
            }
            return value.Substring(adjustedPosA);
        }

        public double Distance(Point3D N1, Point3D N2)
        {
            double d = Math.Sqrt(
                        Math.Pow(N1.X - N2.X, 2) +
                        Math.Pow(N1.Y - N2.Y, 2) +
                        Math.Pow(N1.Z - N2.Z, 2));
            return d;
        }

        public Point3D QuadCenter(List<int> N, Dictionary<int, Node> Nodes)
        {
            Point3D P1 = TriangleCenter(N[0], N[1], N[2], Nodes);
            Point3D P2 = TriangleCenter(N[2], N[3], N[0], Nodes);

            Point3D P = new Point3D
            {
                X = (P1.X + P2.X) / 2,
                Y = (P1.Y + P2.Y) / 2,
                Z = (P1.Z + P2.Z) / 2
            };
            return P;
        }

        public Point3D TriangleCenter(int n1, int n2, int n3, Dictionary<int, Node> Nodes)
        {
            Point3D P = new Point3D
            {
                X = (Nodes[n1].X + Nodes[n2].X + Nodes[n3].X) / 3,
                Y = (Nodes[n1].Y + Nodes[n2].Y + Nodes[n3].Y) / 3,
                Z = (Nodes[n1].Z + Nodes[n2].Z + Nodes[n3].Z) / 3
            };

            return P;
        }

        public double Rad(List<int> N, Point3D C, Dictionary<int, Node> Nodes)
        {
            double rad = 0;
            if (N.Count == 4)
            {
                Node n1 = Nodes[N[0]];
                Node n2 = Nodes[N[1]];
                Node n3 = Nodes[N[2]];
                Node n4 = Nodes[N[3]];

                double rad1 = Math.Sqrt(Math.Pow(n1.X - C.X, 2) + Math.Pow(n1.Y - C.Y, 2) + Math.Pow(n1.Z - C.Z, 2));
                double rad2 = Math.Sqrt(Math.Pow(n2.X - C.X, 2) + Math.Pow(n2.Y - C.Y, 2) + Math.Pow(n2.Z - C.Z, 2));
                double rad3 = Math.Sqrt(Math.Pow(n3.X - C.X, 2) + Math.Pow(n3.Y - C.Y, 2) + Math.Pow(n3.Z - C.Z, 2));
                double rad4 = Math.Sqrt(Math.Pow(n4.X - C.X, 2) + Math.Pow(n4.Y - C.Y, 2) + Math.Pow(n4.Z - C.Z, 2));

                rad = (rad1 + rad2 + rad3 + rad4) / 4;
            }
            if (N.Count == 3)
            {
                Node n1 = Nodes[N[0]];
                Node n2 = Nodes[N[1]];
                Node n3 = Nodes[N[2]];

                double rad1 = Math.Sqrt(Math.Pow(n1.X - C.X, 2) + Math.Pow(n1.Y - C.Y, 2) + Math.Pow(n1.Z - C.Z, 2));
                double rad2 = Math.Sqrt(Math.Pow(n2.X - C.X, 2) + Math.Pow(n2.Y - C.Y, 2) + Math.Pow(n2.Z - C.Z, 2));
                double rad3 = Math.Sqrt(Math.Pow(n3.X - C.X, 2) + Math.Pow(n3.Y - C.Y, 2) + Math.Pow(n3.Z - C.Z, 2));

                rad = (rad1 + rad2 + rad3) / 3;
            }

            return rad;
        }

    }
    public static class ExtensionMethods
    {
        public static double RoundOff(this double i, double interval)
        {
            return ((Math.Round(i / interval)) * interval);
        }
    }

}
