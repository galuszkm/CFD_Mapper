using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace CFD_Mapper
{
    public class RenderInterface
    {
        // VTK rendering objects
        private RenderWindowControl RenWinControl;
        private vtkRenderWindow renderWindow;
        private vtkRenderer Viewport;
        private vtkScalarBarActor ScalarBar;
        private vtkTextProperty TextProp;
        private vtkLookupTable colorLookupTable;
        private vtkSliderRepresentation2D SliderRep;
        private vtkOrientationMarkerWidget Widget;
        private vtkSliderWidget SliderWidget;
        private vtkAxesActor Axes;
        public vtkPlane ClipPlane;
        public double ClipPlaneSize;
        public vtkActor ClipPlaneActor;

        public RenderInterface(Grid Window)
        {
            vtkObject.GlobalWarningDisplayOff(); // Turn off Warning Output Window - annoying sometimes

            WindowsFormsHost VTK_Window = new WindowsFormsHost(); // Create Windows Forms Host for VTK Window
            RenWinControl = new RenderWindowControl();  // Initialize VTK Renderer Window Control

            // Clear input Window and add new host
            Window.Children.Clear();
            Window.Children.Add(VTK_Window);
            VTK_Window.Child = RenWinControl;

            // Create Render Window
            renderWindow = RenWinControl.RenderWindow;
            renderWindow.Render();

            // Initialize View
            Viewport = renderWindow.GetRenderers().GetFirstRenderer();
            Viewport.RemoveAllViewProps();
            CreateViewportBorder(Viewport, new double[3] { 128.0, 128.0, 128.0 });

            // Set default background color
            Viewport.GradientBackgroundOn();
            Viewport.SetBackground(163.0 / 255.0, 163.0 / 255.0, 163.0 / 255.0);
            Viewport.SetBackground2(45.0 / 255.0, 85.0 / 255.0, 125.0 / 255.0);


            // Other properties
            Viewport.GetActiveCamera().ParallelProjectionOn();

            // Create new Properties and Objects
            CreateColorMap();
            CreateScalarBar();
            CreateAxes();
            CreateSlider();
            CreateClipPlane();

        }

        private void CreateViewportBorder(vtkRenderer renderer, double[] color)
        {
            // points start at upper right and proceed anti-clockwise
            vtkPoints points = vtkPoints.New();
            points.SetNumberOfPoints(4);
            points.InsertPoint(0, 1, 1, 0);
            points.InsertPoint(1, 2e-3, 1, 0);
            points.InsertPoint(2, 2e-3, 2e-3, 0);
            points.InsertPoint(3, 1, 3e-3, 0);

            // create cells, and lines
            vtkCellArray cells = vtkCellArray.New();
            cells.Initialize();

            vtkPolyLine lines = vtkPolyLine.New();
            lines.GetPointIds().SetNumberOfIds(5);
            for (int i = 0; i < 4; ++i) lines.GetPointIds().SetId(i, i);
            lines.GetPointIds().SetId(4, 0);
            cells.InsertNextCell(lines);

            // now make tge polydata and display it
            vtkPolyData poly = vtkPolyData.New();
            poly.Initialize();
            poly.SetPoints(points);
            poly.SetLines(cells);

            // use normalized viewport coordinates since
            // they are independent of window size
            vtkCoordinate coordinate = vtkCoordinate.New();
            coordinate.SetCoordinateSystemToNormalizedViewport();

            vtkPolyDataMapper2D mapper = vtkPolyDataMapper2D.New();
            mapper.SetInput(poly);
            mapper.SetTransformCoordinate(coordinate);

            vtkActor2D actor = vtkActor2D.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetColor(color[0], color[1], color[2]);
            // line width should be at least 2 to be visible at extremes

            actor.GetProperty().SetLineWidth((float)0.5); // Line Width

            renderer.AddViewProp(actor);
        }

        private void CreateColorMap()
        {
            // Create the color map
            colorLookupTable = vtkLookupTable.New();
            colorLookupTable.SetHueRange(0.667, 0.0);

            // Assign default number of colors
            //colorLookupTable.SetNumberOfColors(9);
            colorLookupTable.Build();

        }

        private void CreateScalarBar()
        {
            // Initialize ScalarBar actor
            ScalarBar = vtkScalarBarActor.New();
            ScalarBar.SetLookupTable(colorLookupTable);

            // Assign default number of colors and label format
            ScalarBar.SetNumberOfLabels(12);
            ScalarBar.SetLabelFormat("%.2e");

            TextProp = vtkTextProperty.New();
            TextProp.SetFontSize(12);
            TextProp.SetBold(0);
            TextProp.SetFontFamilyToArial();
            TextProp.ItalicOff();
            TextProp.SetJustificationToLeft();
            TextProp.SetVerticalJustificationToBottom();
            TextProp.ShadowOff();
            TextProp.SetColor(1, 1, 1);

            ScalarBar.SetTitleTextProperty(TextProp);
            ScalarBar.SetLabelTextProperty(TextProp);

            // Assign default size of Scalar Bar
            ScalarBar.SetMaximumWidthInPixels(90);
            ScalarBar.SetPosition(0.015, 0.10);
            ScalarBar.SetPosition2(0.16, 0.90);

            //Hide ScalarBar
            ScalarBar.VisibilityOff();

            //Add to Viewport
            Viewport.AddActor2D(ScalarBar);
        }

        private void CreateAxes()
        {
            //  -------------------- Axes of Coordinate system --------------------------------------------
            Axes = new vtkAxesActor();
            Axes.GetXAxisShaftProperty().SetLineWidth((float)2.0);
            Axes.GetYAxisShaftProperty().SetLineWidth((float)2.0);
            Axes.GetZAxisShaftProperty().SetLineWidth((float)2.0);
            Axes.GetXAxisShaftProperty().SetRepresentationToSurface();
            Axes.GetYAxisShaftProperty().SetRepresentationToSurface();
            Axes.GetZAxisShaftProperty().SetRepresentationToSurface();

            Axes.GetXAxisCaptionActor2D().GetCaptionTextProperty().SetFontSize(20);
            Axes.GetXAxisCaptionActor2D().GetCaptionTextProperty().SetBold(0);
            Axes.GetXAxisCaptionActor2D().GetCaptionTextProperty().ItalicOff();
            Axes.GetXAxisCaptionActor2D().GetCaptionTextProperty().ShadowOff();
            Axes.GetXAxisCaptionActor2D().GetTextActor().SetTextScaleModeToNone();

            Axes.GetYAxisCaptionActor2D().GetCaptionTextProperty().SetFontSize(20);
            Axes.GetYAxisCaptionActor2D().GetCaptionTextProperty().SetBold(0);
            Axes.GetYAxisCaptionActor2D().GetCaptionTextProperty().ItalicOff();
            Axes.GetYAxisCaptionActor2D().GetCaptionTextProperty().ShadowOff();
            Axes.GetYAxisCaptionActor2D().GetTextActor().SetTextScaleModeToNone();

            Axes.GetZAxisCaptionActor2D().GetCaptionTextProperty().SetFontSize(20);
            Axes.GetZAxisCaptionActor2D().GetCaptionTextProperty().SetBold(0);
            Axes.GetZAxisCaptionActor2D().GetCaptionTextProperty().ItalicOff();
            Axes.GetZAxisCaptionActor2D().GetCaptionTextProperty().ShadowOff();
            Axes.GetZAxisCaptionActor2D().GetTextActor().SetTextScaleModeToNone();

            Widget = new vtkOrientationMarkerWidget();
            Widget.SetOrientationMarker(Axes);
            Widget.SetInteractor(renderWindow.GetInteractor());
            Widget.SetViewport(0.7, 0.7, 1.2, 1.2);
            Widget.SetEnabled(1);
            Widget.InteractiveOff();

        }

        private void CreateSlider()
        {
            //  ------------ Cliping slider ------------------------------------------------------------
            SliderRep = vtkSliderRepresentation2D.New();

            // Default value range and slider in the middle
            SliderRep.SetMinimumValue(0);
            SliderRep.SetMaximumValue(10);
            SliderRep.SetValue(5);

            // Title and Label properties
            SliderRep.SetTitleText("Clip Plane position");
            SliderRep.GetTitleProperty().SetFontSize(12);
            SliderRep.GetTitleProperty().SetFontFamilyToArial();
            SliderRep.GetTitleProperty().SetBold(0);
            SliderRep.SetTitleHeight(0.03);
            SliderRep.GetLabelProperty().SetFontSize(12);
            SliderRep.GetLabelProperty().SetFontFamilyToArial();
            SliderRep.GetLabelProperty().SetBold(0);

            // Slider positon - normalized to viewport
            SliderRep.GetPoint1Coordinate().SetCoordinateSystemToNormalizedViewport();
            SliderRep.GetPoint1Coordinate().SetValue(0.15, 0.09);
            SliderRep.GetPoint2Coordinate().SetCoordinateSystemToNormalizedViewport();
            SliderRep.GetPoint2Coordinate().SetValue(0.95, 0.09);

            // Slider dimensions
            SliderRep.SetSliderLength(0.08);
            SliderRep.SetSliderWidth(0.025);
            SliderRep.SetHandleSize(0.01);
            SliderRep.SetTubeWidth(0.005);
            SliderRep.SetEndCapLength(0.00);

            // Slider color properties:
            SliderRep.GetSliderProperty().SetColor(180.0 / 255.0, 180.0 / 255.0, 180.0 / 255.0);  // Change the color of the knob that slides
            SliderRep.GetTitleProperty().SetColor(1.0, 1.0, 1.0);                                 // Change the color of the text indicating what the slider controls
            SliderRep.GetLabelProperty().SetColor(1.0, 1.0, 1.0);                                 // Change the color of the text displaying the value
            SliderRep.GetSelectedProperty().SetColor(131.0 / 255, 245.0 / 255.0, 3.0 / 255.0);    // Change the color of the knob when the mouse is held on it
            SliderRep.GetTubeProperty().SetColor(180.0 / 255.0, 180.0 / 255.0, 180.0 / 255.0);    // Change the color of the bar
            SliderRep.GetCapProperty().SetColor(180.0 / 255.0, 180.0 / 255.0, 180.0 / 255.0);     // Change the color of the ends of the bar

            // Slider Widget
            SliderWidget = vtkSliderWidget.New();
            SliderWidget.SetInteractor(renderWindow.GetInteractor());
            SliderWidget.SetRepresentation(SliderRep);
            SliderWidget.SetAnimationModeToAnimate();
            SliderWidget.SetEnabled(0);
            SliderWidget.InteractionEvt += new vtkObject.vtkObjectEventHandler(MoveClipPlane);
        }

        private void CreateClipPlane()
        {
            // Clip Plane
            ClipPlane = vtkPlane.New();

            vtkPoints ClipPoints = vtkPoints.New();
            ClipPlaneSize = 1;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    double y = j * ClipPlaneSize / 9 - ClipPlaneSize / 2;
                    double z = i * ClipPlaneSize / 9 - ClipPlaneSize / 2;
                    ClipPoints.InsertNextPoint(0, y, z);
                }
            }
            vtkUnstructuredGrid ClipGrid = vtkUnstructuredGrid.New();
            ClipGrid.SetPoints(ClipPoints);
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    vtkQuad quad = vtkQuad.New();
                    quad.GetPointIds().SetId(0, i * 10 + j);
                    quad.GetPointIds().SetId(1, i * 10 + j + 1);
                    quad.GetPointIds().SetId(2, (i + 1) * 10 + j + 1);
                    quad.GetPointIds().SetId(3, (i + 1) * 10 + j);
                    ClipGrid.InsertNextCell(quad.GetCellType(), quad.GetPointIds());
                }
            }

            vtkDataSetMapper ClipMapper = vtkDataSetMapper.New();
            ClipMapper.SetInput(ClipGrid);

            ClipPlaneActor = vtkActor.New();
            ClipPlaneActor.SetMapper(ClipMapper);
            ClipPlaneActor.GetProperty().EdgeVisibilityOn();
            ClipPlaneActor.GetProperty().SetColor(195.0 / 255.0, 195.0 / 255.0, 195.0 / 255.0);
            ClipPlaneActor.GetProperty().SetOpacity(0.1);
            ClipPlaneActor.VisibilityOff();
            Viewport.AddActor(ClipPlaneActor);
        }

        public void MoveClipPlane(vtkObject sender, vtkObjectEventArgs e)
        {
            if (Math.Abs(ClipPlane.GetNormal()[0]) == 1) ClipPlane.SetOrigin(SliderRep.GetValue(), ClipPlane.GetOrigin()[1], ClipPlane.GetOrigin()[2]);
            if (Math.Abs(ClipPlane.GetNormal()[1]) == 1) ClipPlane.SetOrigin(ClipPlane.GetOrigin()[0], SliderRep.GetValue(), ClipPlane.GetOrigin()[2]);
            if (Math.Abs(ClipPlane.GetNormal()[2]) == 1) ClipPlane.SetOrigin(ClipPlane.GetOrigin()[0], ClipPlane.GetOrigin()[1], SliderRep.GetValue());

            ClipPlaneActor.SetPosition(
                                ClipPlane.GetOrigin()[0],
                                ClipPlane.GetOrigin()[1],
                                ClipPlane.GetOrigin()[2]);
        }

        
        // ===================== PUBLIC methods to modify Graphical View properties ===================

        public void ChangeBackgroundColor(bool gradient, double[] color1, double[] color2)
        {
            if (gradient == true)
            {
                Viewport.GradientBackgroundOn();
                Viewport.SetBackground(color1[0] / 255.0, color1[1] / 255.0, color1[2] / 255.0);
                Viewport.SetBackground2(color2[0] / 255.0, color2[1] / 255.0, color2[2] / 255.0);
            }
            else
            {
                Viewport.GradientBackgroundOff();
                Viewport.SetBackground(color1[0] / 255.0, color1[1] / 255.0, color1[2] / 255.0);
            }
        }

        public void ChangeLabelColor(double[] color)
        {
            TextProp.SetColor(color[0], color[1], color[2]);
            SliderRep.GetTitleProperty().SetColor(color[0], color[1], color[2]);
            SliderRep.GetLabelProperty().SetColor(color[0], color[1], color[2]);

            ScalarBar.SetLabelTextProperty(TextProp);
            ScalarBar.SetTitleTextProperty(TextProp);

            Axes.GetXAxisCaptionActor2D().GetCaptionTextProperty().SetColor(color[0], color[1], color[2]);
            Axes.GetYAxisCaptionActor2D().GetCaptionTextProperty().SetColor(color[0], color[1], color[2]);
            Axes.GetZAxisCaptionActor2D().GetCaptionTextProperty().SetColor(color[0], color[1], color[2]);
        }

        public void ChangeColorRange(double min, double max)
        {
            colorLookupTable.SetTableRange(min, max);
        }

        public void ChangeColorNumber(int numb)
        {
            colorLookupTable.SetNumberOfColors(numb);
            ScalarBar.SetNumberOfLabels(numb);
        }

        public void ChangeScalarName(string title)
        {
            ScalarBar.SetTitle(title);
        }

        public void ChangeScalarLabelFormat(int precision, char format)
        {
            ScalarBar.SetLabelFormat("%." + precision.ToString() + format.ToString());
        }

        public vtkLookupTable Get_ColorTable()
        {
            return colorLookupTable;
        }

        public void HideScalarBar()
        {
            ScalarBar.VisibilityOff();
        }

        public void ShowScalarBar()
        {
            ScalarBar.VisibilityOn();
        }

        public void AddActor(vtkActor actor)
        {
            Viewport.AddActor(actor);
        }

        public void Refresh()
        {
            Viewport.Render();
            Viewport.ResetCameraClippingRange();
            RenWinControl.Refresh();
        }

        public void FitView()
        {
            Viewport.ResetCamera();
        }

        public vtkPlane Get_ClipPlane()
        {
            return ClipPlane;
        }

        public void ShowClipSlider()
        {
            SliderWidget.SetEnabled(1);
            ClipPlaneActor.VisibilityOn();
        }

        public void HideClipSlider()
        {
            SliderWidget.SetEnabled(0);
            ClipPlaneActor.VisibilityOff();
        }

        public void SetClipPlane(char N, Database DB)
        {
            double[] BoundaryRange = new double[6];
            double[] FEMBound = DB.ActorFEM.GetBounds();
            double[] CFDBound = DB.ActorCFD.GetBounds();

            // Minimum X
            if (FEMBound[0] <= CFDBound[0]) BoundaryRange[0] = FEMBound[0];
            else BoundaryRange[0] = CFDBound[0];

            // Maximum X
            if (FEMBound[1] >= CFDBound[1]) BoundaryRange[1] = FEMBound[1];
            else BoundaryRange[1] = CFDBound[1];

            // Minimum Y
            if (FEMBound[2] <= CFDBound[2]) BoundaryRange[2] = FEMBound[2];
            else BoundaryRange[2] = CFDBound[2];

            // Maximum Y
            if (FEMBound[3] >= CFDBound[3]) BoundaryRange[3] = FEMBound[3];
            else BoundaryRange[3] = CFDBound[3];

            // Minimum Z
            if (FEMBound[4] <= CFDBound[4]) BoundaryRange[4] = FEMBound[4];
            else BoundaryRange[4] = CFDBound[4];

            // Maximum Z
            if (FEMBound[5] >= CFDBound[5]) BoundaryRange[5] = FEMBound[5];
            else BoundaryRange[5] = CFDBound[5];

            // Clip Plane Scale
            List<double> temp = new List<double>();
            temp.Add(Math.Abs(BoundaryRange[1] - BoundaryRange[0]) * 1.1);
            temp.Add(Math.Abs(BoundaryRange[3] - BoundaryRange[2]) * 1.1);
            temp.Add(Math.Abs(BoundaryRange[5] - BoundaryRange[4]) * 1.1);

            ClipPlaneSize = temp.Max();
            ClipPlaneActor.SetScale(ClipPlaneSize);

            // Update Positions
            ClipPlane.SetOrigin(
                (BoundaryRange[1] + BoundaryRange[0]) / 2.0,
                (BoundaryRange[3] + BoundaryRange[2]) / 2.0,
                (BoundaryRange[5] + BoundaryRange[4]) / 2.0);

            ClipPlaneActor.SetPosition(
                ClipPlane.GetOrigin()[0],
                ClipPlane.GetOrigin()[1],
                ClipPlane.GetOrigin()[2]);

            double min = 0; double max = 1;
            if (N == 'X')
            {
                min = BoundaryRange[0];
                max = BoundaryRange[1];
                ClipPlane.SetNormal(1, 0, 0);
                ClipPlaneActor.SetOrientation(0, 0, 0);
            }
            if (N == 'Y')
            {
                min = BoundaryRange[2];
                max = BoundaryRange[3];
                ClipPlane.SetNormal(0, 1, 0);
                ClipPlaneActor.SetOrientation(0, 0, 90);
            }
            if (N == 'Z')
            {
                min = BoundaryRange[4];
                max = BoundaryRange[5]; 
                ClipPlane.SetNormal(0, 0, 1);
                ClipPlaneActor.SetOrientation(0, 90, 0);
            }
            // Update slider values
            SliderRep.SetMinimumValue(min);
            SliderRep.SetMaximumValue(max);
            SliderRep.SetValue((max + min) / 2);

        }
    }

}
