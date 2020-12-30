using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace CFD_Mapper
{
    /// <summary>
    /// Logika interakcji dla klasy Window.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Mode switch
        bool OutputMode;
        bool MappingDone;

        // Mapping settings
        string SmoothingType;
        int DecimalPrecision;
        double RadiusScale;
        string solver;

        // Main objects
        Database DB;
        Functions fun;
        RenderInterface iRen;

        // Error report
        string Mapping_Warning;
        int Mapping_WarningNumb;

        public MainWindow()
        {
            InitializeComponent();
            fun = new Functions();

            Width = 1; Height = 1;
            Top = 0; Left = 0;

            Show();
            CreateNew();

            Width = 1200; Height = 900;
            MinWidth = 515; MinHeight = 500;
            Top = 80; Left = 200;

        }

        public void CreateNew()
        {
            // Clear output window
            Output.Document.Blocks.Clear();

            // Initialize status
            DB = null;
            MappingDone = false;
            OutputMode = false;

            string outtext =
                " ****************************************************************************************************************************************************\n" +
                " CFD to FEM PRESSURE MAPPER\n".PadLeft(110, ' ') +
                " ****************************************************************************************************************************************************\n" +
                "  LICENSE NOTE:\n" +
                "  Copyright (c) 2020 Michal Galuszka\n" +
                "  This is an open source software distribiuted with BSD License.                                                   \n" +
                "  Copyright note, list of contributors and open source code packages used are shown in Help -> About               \n" +
                "  Suggestions and comments are welcomed. Please report any bugs encountered in either the documentation or results.\n" +
                "  The authors do not assume any responsibility for the accuracy of any results obtained from this software.        \n" +
                " ==========================================================================================\n\n\n";

            Output.AppendText(outtext);

            // Create vtk window
            iRen = new RenderInterface(VTK_Window);

            // Add Items to ComboBoxes
            Smoothing.Items.Add(new ComboBoxItem().Content = "Min");
            Smoothing.Items.Add(new ComboBoxItem().Content = "Max");
            Smoothing.Items.Add(new ComboBoxItem().Content = "Average");
            Smoothing.Items.Add(new ComboBoxItem().Content = "Linear");
            Smoothing.Items.Add(new ComboBoxItem().Content = "Gaussian");
            Smoothing.SelectedIndex = 2;
            Precision.Items.Add(new ComboBoxItem().Content = "0");
            Precision.Items.Add(new ComboBoxItem().Content = "1");
            Precision.Items.Add(new ComboBoxItem().Content = "2");
            Precision.Items.Add(new ComboBoxItem().Content = "3");
            Precision.Items.Add(new ComboBoxItem().Content = "5");
            Precision.Items.Add(new ComboBoxItem().Content = "8");
            Precision.SelectedIndex = 0;
            Radius_fac.Items.Add(new ComboBoxItem().Content = "0.5");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "1.0");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "1.5");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "2.0");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "2.5");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "3.0");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "5.0");
            Radius_fac.Items.Add(new ComboBoxItem().Content = "10");
            Radius_fac.SelectedIndex = 1;
            Units.Items.Add(new ComboBoxItem().Content = "m, Pa");
            Units.Items.Add(new ComboBoxItem().Content = "m, kPa");
            Units.Items.Add(new ComboBoxItem().Content = "m, MPa");
            Units.Items.Add(new ComboBoxItem().Content = "m, mPa");
            Units.Items.Add(new ComboBoxItem().Content = "mm, Pa");
            Units.Items.Add(new ComboBoxItem().Content = "mm, kPa");
            Units.Items.Add(new ComboBoxItem().Content = "mm, MPa");
            Units.Items.Add(new ComboBoxItem().Content = "mm, mPa");
            Units.SelectedIndex = 0;
            CFD_size.Items.Add(new ComboBoxItem().Content = "0.25");
            CFD_size.Items.Add(new ComboBoxItem().Content = "0.5");
            CFD_size.Items.Add(new ComboBoxItem().Content = "1");
            CFD_size.Items.Add(new ComboBoxItem().Content = "2");
            CFD_size.Items.Add(new ComboBoxItem().Content = "3");
            CFD_size.Items.Add(new ComboBoxItem().Content = "4");
            CFD_size.Items.Add(new ComboBoxItem().Content = "5");
            CFD_size.Items.Add(new ComboBoxItem().Content = "10");
            CFD_size.Items.Add(new ComboBoxItem().Content = "15");
            CFD_size.SelectedIndex = 2;

            // Activate/Deactivate Buttons and others
            ClipCheck.IsEnabled = false;
            ShowClip.IsEnabled = false;
            ClipX.IsEnabled = false;
            ClipY.IsEnabled = false;
            ClipZ.IsEnabled = false;
            Reverse.IsEnabled = false;
            Mapping.IsEnabled = false;
            ColorSettings.IsEnabled = false;
            MappingSettings.IsEnabled = false;
            Export.IsEnabled = false;
            ClipBox.IsEnabled = false;

            // Clear text boxes
            Model_path.Text = "";
            CFD_path.Text = "";
            Manual_Min.Text = "";
            Manual_Max.Text = "";
        }

        public void Execute_Mapping()
        {
            Stopwatch sw = new Stopwatch(); sw.Start();
            Mapping_Warning = "";
            Mapping_WarningNumb = 0;

            // Make copy of CFD point List (exclude points outside FEM domain)
            List<CFD_Point> CFD_inFEM = DB.CFD.Where(i =>
                i.X >= DB.ActorFEM.GetBounds()[0] &&
                i.X <= DB.ActorFEM.GetBounds()[1] &&
                i.Y >= DB.ActorFEM.GetBounds()[2] &&
                i.Y <= DB.ActorFEM.GetBounds()[3] &&
                i.Z >= DB.ActorFEM.GetBounds()[4] &&
                i.Z <= DB.ActorFEM.GetBounds()[5] ).ToList();

            //Loop over all lines
            Parallel.ForEach(DB.Faces, Face =>
            {
                // CFD Pressure inside Influence area
                List<double> Pressure = new List<double>();
                List<double> Distance = new List<double>();

                // Calculate Face Properties
                Face.Calculate_Prop(DB.Nodes, RadiusScale);

                foreach (CFD_Point cfd in CFD_inFEM)
                {
                    if (cfd.X > Face.Bounds[0])
                    {
                        if (cfd.X < Face.Bounds[1])
                        {
                            if (cfd.Y > Face.Bounds[2])
                            {
                                if (cfd.Y < Face.Bounds[3])
                                {
                                    if (cfd.Z > Face.Bounds[4])
                                    {
                                        if (cfd.Z < Face.Bounds[5])
                                        {
                                            Point3D PointCFD = new Point3D { X = cfd.X, Y = cfd.Y, Z = cfd.Z };
                                            double Dist = fun.Distance(PointCFD, Face.Centroid);
                                            if (Dist <= Face.R)
                                            {
                                                Pressure.Add(cfd.P);
                                                Distance.Add(Dist);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (Pressure.Count > 0)
                {
                    // Pressure assigned
                    Face.NoPressure = false;

                    //Calculate pressure
                    if (SmoothingType == "Max") Face.P = Math.Round(Pressure.Max(), DecimalPrecision);
                    if (SmoothingType == "Min") Face.P = Math.Round(Pressure.Min(), DecimalPrecision);
                    if (SmoothingType == "Average") Face.P = Math.Round(Pressure.Average(), DecimalPrecision);

                    // Linear smoothing
                    if (SmoothingType == "Linear")
                    {
                        // Initialize sum Pressure and sum of weigths
                        double P = 0;
                        double SumW = 0;

                        for (int i = 0; i < Pressure.Count; i++)
                        {
                            // Calcuate weigth as   1 - distance / (radius of influence area)
                            double weight = 1 - Distance[i] / Face.R;
                            P += Pressure[i] * weight;
                            SumW += weight;
                        }

                        // Divide weighted sum of pressure by sum of weight
                        Face.P = Math.Round(P / SumW, DecimalPrecision);
                    }

                    // Gaussian smoothing
                    if (SmoothingType == "Gaussian")
                    {
                        // Initialize sum Pressure and sum of weigths
                        double P = 0;
                        double SumW = 0;

                        double sigma = 0.20;

                        for (int i = 0; i < Pressure.Count; i++)
                        {
                            // Calcuate weigth as   1 / exp[- (D/R)^2 / 2*sigma^2 ]
                            double weight = 1 / Math.Exp(-Math.Pow(Distance[i] / Face.R, 2) / (2 * Math.Pow(sigma, 2)));
                            P += Pressure[i] * weight;
                            SumW += weight;
                        }

                        // Divide weighted sum of pressure by number of points
                        Face.P = Math.Round(P / SumW, DecimalPrecision);
                    }

                }
                else
                {
                    Face.NoPressure = true;
                    Mapping_WarningNumb++;
                    Mapping_Warning += "   Warning: Face " + Face.Index + ": has no CFD point in range. No pressure assign\n";
                }
            });

            // Active/Deactive Buttons
            MappingDone = true;
            Plot_output.IsEnabled = true;
            Export.IsEnabled = true;

            // If OutputMode was last one update FEM colors
            if (OutputMode == true)
            {
                DB.Update_ColorFEM(iRen);
                iRen.Refresh();
            }

            // Stop time measurement
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);

            // Show Message
            System.Windows.MessageBox.Show("Mapping completed in " +
                ((double)sw.ElapsedMilliseconds / 1000.0).ToString("f2", CultureInfo.InvariantCulture) + " s.\n"
                + Mapping_WarningNumb.ToString() + " faces have no pressure - no CFD point in range.",
                "Mapping Status");

            // Write Mappping errors to Output Window
            Output.ScrollToVerticalOffset(200);
            Output.AppendText(
                " ==========================================================================================\n" +
                "   Mapping Solver output: " +
                "     Total CPU time: " + ((double)sw.ElapsedMilliseconds / 1000.0).ToString("f2", CultureInfo.InvariantCulture) + " s\n\n");

            if (Mapping_WarningNumb > 0)
            {
                Output.AppendText(
                    "   Warning: " + Mapping_WarningNumb.ToString() + " faces have no pressure - no CFD point in range.\n" +
                    "            Consider bigger 'Radius factor' value to expand face influence area.\n\n");
            }
            else Output.AppendText("   No warnings! Pressure assigned to all faces.\n\n");
        }

        public void Export_MarcOutput(string path)
        {
            StringBuilder proc = new StringBuilder();

            // ========================== MSC Marc OUTPUT FILE ==================================

            proc.Append("*set_update off\n");
            proc.Append("*set_undo off\n");
            proc.Append("*new_md_table 1 1\n");
            proc.Append("*table_name Pressure_0_to_1\n");
            proc.Append("*set_md_table_type 1\n");
            proc.Append("time\n");
            proc.Append("*table_add\n");
            proc.Append("0\n");
            proc.Append("0\n");
            proc.Append("1\n");
            proc.Append("1\n");

            // Sort Pressure by the same value
            Dictionary<double, List<string>> PSorted = new Dictionary<double, List<string>>();

            foreach (Face face in DB.Faces)
            {
                if (face.NoPressure == false && face.P != 0)
                {
                    // if pressure exists in dictionary
                    if (PSorted.ContainsKey(face.P))
                    {
                        // if last string is smaller than 120 char
                        if (PSorted[face.P][PSorted[face.P].Count - 1].Length < 120)
                        {
                            PSorted[face.P][PSorted[face.P].Count - 1] += face.EID.ToString() + ":" + face.Face_No.ToString() + " ";
                        }
                        else
                        {
                            string newline = face.EID.ToString() + ":" + face.Face_No.ToString() + " ";
                            PSorted[face.P].Add(newline);

                        }
                    }
                    else
                    {
                        PSorted.Add(face.P, new List<string>());
                        string newline = face.EID.ToString() + ":" + face.Face_No.ToString() + " ";
                        PSorted[face.P].Add(newline);
                    }
                }
            }

            // Convert dictionary keys to list to order ascending
            List<double> PList = PSorted.Keys.ToList();
            PList.Sort();

            foreach (double P in PList)
            {
                proc.Append("*new_apply *apply_type face_load\n");
                proc.Append("*apply_name Pressure_" + Math.Round(P, DecimalPrecision).ToString(CultureInfo.InvariantCulture) + "\n");
                proc.Append("*apply_dof p *apply_dof_value p\n");
                proc.Append("*apply_dof_table p\n");
                proc.Append("Pressure_0_to_1\n");
                proc.Append("*apply_option dist_ld_contact:suppress\n");
                proc.Append("*apply_dof_value p " + P.ToString(CultureInfo.InvariantCulture) + "\n");
                proc.Append("*add_apply_faces\n");

                foreach (string s in PSorted[P])
                {
                    proc.Append(s + "\n");
                }
                proc.Append("# | End of List\n");

                foreach (string s in DB.Loadcase)
                {
                    proc.Append("*edit_loadcase " + s + "\n" +
                            "*add_loadcase_loads Pressure_" + P.ToString(CultureInfo.InvariantCulture) + "\n");
                }

                proc.Append("*add_job_applys Pressure_" + P.ToString(CultureInfo.InvariantCulture) + "\n");

            }
            proc.Append("*set_update on\n*set_undo on\n");

            File.WriteAllText(fun.BeforeLast(path, ".") + "_PressureMapCFD.proc", proc.ToString());

            System.Windows.MessageBox.Show("Output file saved in selected model directory");
        }

        public void Export_LSDynaOutput(string path)
        {
            StringBuilder proc = new StringBuilder();

            // ========================== LS-DYNA OUTPUT FILE ==================================

            // Create default curve
            proc.Append("$\n");
            proc.Append("*DEFINE_CURVE_TITLE\n");
            proc.Append("Pressure curve\n");
            proc.Append("$#    lcid      sidr       sfa       sfo      offa      offo    dattyp     lcint\n");
            proc.Append("     10001         0       1.0       1.0       0.0       0.0         0         0\n");
            proc.Append("$#                a1                  o1\n");
            proc.Append("                 0.0                 0.0\n");
            proc.Append("                 1.0                 1.0\n$\n");

            proc.Append("*LOAD_SEGMENT\n");
            proc.Append("$#    lcid        sf        at        n1        n2        n3        n4\n");

            foreach (Face face in DB.Faces)
            {
                if (face.NoPressure == false && face.P != 0)
                {
                    if (face.Nodes.Count == 4)
                    {
                        string LCID = "10001";
                        string SF = face.P.ToString("0.000#E+00", CultureInfo.InvariantCulture);
                        string N1 = face.Nodes[0].ToString();
                        string N2 = face.Nodes[1].ToString();
                        string N3 = face.Nodes[2].ToString();
                        string N4 = face.Nodes[3].ToString();

                        proc.Append(string.Format("{0,10}{1,10}{2,10}{3,10}{4,10}{5,10}{6,10}\n",
                                    LCID, SF, "0.0", N1, N2, N3, N4));
                    }
                    if (face.Nodes.Count == 3)
                    {
                        string LCID = "10001";
                        string SF = face.P.ToString("0.000#E+00", CultureInfo.InvariantCulture);
                        string N1 = face.Nodes[0].ToString();
                        string N2 = face.Nodes[1].ToString();
                        string N3 = face.Nodes[2].ToString();
                        string N4 = face.Nodes[2].ToString();

                        proc.Append(string.Format("{0,10}{1,10}{2,10}{3,10}{4,10}{5,10}{6,10}\n",
                                    LCID, SF, "0.0", N1, N2, N3, N4));
                    }
                }
            }
            proc.Append("$");

            // Write to file
            File.WriteAllText(fun.BeforeLast(path, ".") + "_PressureMapCFD.k", proc.ToString());

            System.Windows.MessageBox.Show("Output file saved in selected model directory");
        }


        // =============================== BUTTONS CLICK ========================================

        private void Plot_input_Click(object sender, RoutedEventArgs e)
        {
            if (DB == null)
            {
                bool start = true;

                if (!File.Exists(Model_path.Text)) { start = false; System.Windows.MessageBox.Show("Model file does not exist!", "Input error"); }

                if (!File.Exists(CFD_path.Text)) { start = false; System.Windows.MessageBox.Show("CFD data file does not exist!", "Input error"); }

                if (start)
                {
                    string pathFEM = Model_path.Text;
                    string pathCFD = CFD_path.Text;
                    string units = Units.SelectedItem.ToString();
                    float sizeCFD = float.Parse(CFD_size.SelectedItem.ToString(), CultureInfo.InvariantCulture);

                    DB = new Database(pathFEM, pathCFD, solver, units, iRen, sizeCFD);
                    DB.ActorFEM.VisibilityOn();
                    DB.ActorCFD.VisibilityOn();

                    iRen.ChangeScalarLabelFormat(2, 'f');
                    iRen.ChangeScalarName("Pressure\n[MPa]");
                    iRen.ShowScalarBar();
                    iRen.FitView();
                    iRen.Refresh();

                    // Set Clip Plane (initially normal to X)
                    iRen.SetClipPlane('X', DB);

                    // Set color range
                    Manual_Min.Text = iRen.Get_ColorTable().GetTableRange()[0].ToString("f3", CultureInfo.InvariantCulture);
                    Manual_Max.Text = iRen.Get_ColorTable().GetTableRange()[1].ToString("f3", CultureInfo.InvariantCulture);

                    //Activate/Deactivate Buttons and others
                    ClipCheck.IsEnabled = true;
                    Mapping.IsEnabled = true;
                    ColorSettings.IsEnabled = true;
                    MappingSettings.IsEnabled = true;
                    Units.IsEnabled = false;
                    ClipBox.IsEnabled = true;

                    // Report Errors
                    Output.ScrollToVerticalOffset(200);
                    Output.AppendText(
                        " ==========================================================================================\n" +
                        "   File reading and Database built: \n");

                    if (DB.Errors.Length > 0) Output.AppendText(DB.Errors);
                    else Output.AppendText("    No errors! Database successfully created.\n\n");
                }
            }
            else
            {
                OutputMode = false;
                DB.Clear_ColorFEM();
                DB.ActorCFD.VisibilityOn();
                iRen.Refresh();
            }
        }

        private void Plot_output_Click(object sender, RoutedEventArgs e)
        {
            OutputMode = true;
            DB.Update_ColorFEM(iRen);

            // Hide all CFD Actors
            DB.ActorCFD.VisibilityOff();
            iRen.Refresh();

        }

        private void Model_button_Click(object sender, RoutedEventArgs e)
        {
            string filter = "MSC Marc (*.dat)|*.dat|" + "LS-Dyna (*.k, *.key)|*.k;*.key";

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = filter,
                FilterIndex = 0,
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Model_path.Text = dialog.FileName;
                if (fun.After(Model_path.Text, ".").Contains("k"))
                {
                    solver = "LSDyna";
                }
                if (fun.After(Model_path.Text, ".").Contains("dat"))
                {
                    solver = "Marc";
                }
            }
            else
            {
                Model_path.Text = "";
            }
        }

        private void CFD_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog_CFD = new OpenFileDialog
            {
                Filter = "CFD file (*.txt, *.dat)|*.txt;*.dat",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            if (dialog_CFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CFD_path.Text = dialog_CFD.FileName;
            }
            else
            {
                CFD_path.Text = "";
            }
        }

        private void Mapping_Click(object sender, RoutedEventArgs e)
        {
            Execute_Mapping();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (MappingDone == true)
            {
                if (solver == "Marc")
                {
                    Export_MarcOutput(Model_path.Text);
                }

                if (solver == "LSDyna")
                {
                    Export_LSDynaOutput(Model_path.Text);
                }
            }
        }

        private void CFD_size_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DB != null)
            {
                float sizeCFD = float.Parse(CFD_size.SelectedItem.ToString(), CultureInfo.InvariantCulture);
                DB.ActorCFD.GetProperty().SetPointSize(sizeCFD);
                iRen.Refresh();
            }
        }

        private void Manual_Range_Apply(object sender, RoutedEventArgs e)
        {
            double[] CurrentRange = iRen.Get_ColorTable().GetTableRange();

            if (double.TryParse(Manual_Min.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                CurrentRange[0] = double.Parse(Manual_Min.Text, CultureInfo.InvariantCulture);

            if (double.TryParse(Manual_Max.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                CurrentRange[1] = double.Parse(Manual_Max.Text, CultureInfo.InvariantCulture);

            iRen.ChangeColorRange(CurrentRange[0], CurrentRange[1]);
            DB.Update_ColorCFD(iRen);
            if (MappingDone == true) DB.Update_ColorFEM(iRen);
            iRen.Refresh();
        }

        private void ClipCheck_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox c = sender as System.Windows.Controls.CheckBox;
            if (c.IsChecked == true)
            {
                ShowClip.IsEnabled = true;
                ShowClip.IsChecked = true;
                ClipX.IsEnabled = true;
                ClipY.IsEnabled = true;
                ClipZ.IsEnabled = true;
                Reverse.IsEnabled = true;

                DB.Clip_Model(true, iRen);
                iRen.ShowClipSlider();
                iRen.Refresh();
            }
            else
            {
                ShowClip.IsEnabled = false;
                ShowClip.IsChecked = false;
                ClipX.IsEnabled = false;
                ClipY.IsEnabled = false;
                ClipZ.IsEnabled = false;
                Reverse.IsEnabled = false;

                DB.Clip_Model(false, iRen);
                iRen.HideClipSlider();
                iRen.Refresh();
            }
        }

        private void ClipNormal(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;

            if (button.Content.ToString().Contains("X"))
            {
                iRen.SetClipPlane('X', DB);
            }

            if (button.Content.ToString().Contains("Y"))
            {
                iRen.SetClipPlane('Y', DB);
            }

            if (button.Content.ToString().Contains("Z"))
            {
                iRen.SetClipPlane('Z', DB);
            }
            iRen.Refresh();
        }

        private void ShowClipGrid_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox c = sender as System.Windows.Controls.CheckBox;
            if (DB != null)
            {
                if (c.IsChecked == true)
                {
                    iRen.ClipPlaneActor.VisibilityOn();
                    iRen.Refresh();
                }
                else
                {
                    iRen.ClipPlaneActor.VisibilityOff();
                    iRen.Refresh();
                }
            }
        }

        private void Reverse_Click(object sender, RoutedEventArgs e)
        {
            iRen.ClipPlane.SetNormal(
                iRen.Get_ClipPlane().GetNormal()[0] * -1.0,
                iRen.Get_ClipPlane().GetNormal()[1] * -1.0,
                iRen.Get_ClipPlane().GetNormal()[2] * -1.0);
            iRen.Refresh();
        }


        // ============================ MENU ITEMS CLICK =========================================

        private void New_Click(object sender, RoutedEventArgs e)
        {
            DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("This project will be deleted", "New Project", MessageBoxButtons.OKCancel);
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                // Create new instance
                CreateNew();
            }
        }

        private void ChangeColor(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button b = sender as System.Windows.Controls.Button;

            if (b.Name == "Background_T1")
            {
                iRen.ChangeBackgroundColor(false, new double[3] { 255, 255, 255 }, new double[3] { 255, 255, 255 });
            }
            if (b.Name == "Background_T2")
            {
                iRen.ChangeBackgroundColor(false, new double[3] { 82, 87, 110 }, new double[3] { 82, 87, 110 });
            }
            if (b.Name == "Background_T3")
            {
                iRen.ChangeBackgroundColor(true, new double[3] { 163, 163, 163 }, new double[3] { 45, 85, 125 });
            }
            if (b.Name == "Background_T4")
            {
                iRen.ChangeBackgroundColor(true, new double[3] { 255, 255, 255 }, new double[3] { 150, 150, 150 });
            }
            if (b.Name == "Label_T1")
            {
                iRen.ChangeLabelColor(new double[3] { 1.0, 1.0, 1.0 });
            }
            if (b.Name == "Label_T2")
            {
                iRen.ChangeLabelColor(new double[3] { 0.0, 0.0, 0.0 });
            }
            iRen.Refresh();
        }

        private void Wireframe_ON(object sender, RoutedEventArgs e)
        {
            if (DB != null)
            {
                DB.ActorFEM.GetProperty().SetEdgeVisibility(1);
                iRen.Refresh();
            }
        }

        private void Wireframe_OFF(object sender, RoutedEventArgs e)
        {
            if (DB != null)
            {
                DB.ActorFEM.GetProperty().SetEdgeVisibility(0);
                iRen.Refresh();
            }
        }

        private void Transparent_ON(object sender, RoutedEventArgs e)
        {
            if (DB != null)
            {
                DB.ActorFEM.GetProperty().SetOpacity(0.2);
                iRen.Refresh();
            }
        }

        private void Transparent_OFF(object sender, RoutedEventArgs e)
        {
            if (DB != null)
            {
                DB.ActorFEM.GetProperty().SetOpacity(1.0);
                iRen.Refresh();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            About WinAbout = new About();
            WinAbout.Owner = this;
            WinAbout.ShowDialog();
        }

        // =============================== OTHERS ===============================================

        private void Smoothing_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Smoothing.SelectedIndex == 0) SmoothingType = "Min";
            if (Smoothing.SelectedIndex == 1) SmoothingType = "Max";
            if (Smoothing.SelectedIndex == 2) SmoothingType = "Average";
            if (Smoothing.SelectedIndex == 3) SmoothingType = "Linear";
            if (Smoothing.SelectedIndex == 4) SmoothingType = "Gaussian";
        }

        private void Precision_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Precision.SelectedIndex == 0) DecimalPrecision = 0;
            if (Precision.SelectedIndex == 1) DecimalPrecision = 1;
            if (Precision.SelectedIndex == 2) DecimalPrecision = 2;
            if (Precision.SelectedIndex == 3) DecimalPrecision = 3;
            if (Precision.SelectedIndex == 4) DecimalPrecision = 5;
            if (Precision.SelectedIndex == 5) DecimalPrecision = 8;
        }

        private void Radius_fac_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Radius_fac.SelectedIndex == 0) RadiusScale = 0.25;
            if (Radius_fac.SelectedIndex == 1) RadiusScale = 0.5;
            if (Radius_fac.SelectedIndex == 2) RadiusScale = 1;
            if (Radius_fac.SelectedIndex == 3) RadiusScale = 2;
            if (Radius_fac.SelectedIndex == 4) RadiusScale = 3;
            if (Radius_fac.SelectedIndex == 5) RadiusScale = 4;
            if (Radius_fac.SelectedIndex == 6) RadiusScale = 5;
            if (Radius_fac.SelectedIndex == 7) RadiusScale = 10;
            if (Radius_fac.SelectedIndex == 8) RadiusScale = 15;
        }

    }
}