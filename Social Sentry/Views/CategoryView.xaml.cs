using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Social_Sentry.ViewModels;

namespace Social_Sentry.Views
{
    public partial class CategoryView : System.Windows.Controls.UserControl
    {
        public CategoryView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = DataContext as CategoryViewModel;
            if (vm != null)
            {
                vm.LoadCategories();
            }
            DrawChart();
        }

        private void DrawChart()
        {
            var vm = DataContext as CategoryViewModel;
            if (vm == null || vm.Categories.Count == 0) return;

            ChartCanvas.Children.Clear();

            double centerX = ChartCanvas.Width / 2;
            double centerY = ChartCanvas.Height / 2;
            double radius = 110;
            double thickness = 30;
            double startAngle = -90; // Start at top

            // Check if we have valid percentages
            double totalPercentage = 0;
            foreach(var cat in vm.Categories) totalPercentage += cat.Percentage;
            
            if (totalPercentage <= 0.001)
            {
                // No data - draw empty grey ring
                var path = CreateArc(centerX, centerY, radius, 0, 359.99, thickness, "#1E1E1E"); // Dark grey
                // Or slightly lighter to be visible against background
                path = CreateArc(centerX, centerY, radius, 0, 359.99, thickness, "#2D2D2D");
                ChartCanvas.Children.Add(path);
                return; 
            }

            foreach (var category in vm.Categories)
            {
                if (category.Percentage <= 0) continue;

                double sweepAngle = category.Percentage * 360;
                if (sweepAngle >= 360) sweepAngle = 359.99; // Avoid full circle issues with ArcSegment

                var path = CreateArc(centerX, centerY, radius, startAngle, sweepAngle, thickness, category.Color);
                ChartCanvas.Children.Add(path);

                startAngle += sweepAngle;
            }
        }

        private System.Windows.Shapes.Path CreateArc(double centerX, double centerY, double radius, double startAngle, double sweepAngle, double thickness, string colorHex)
        {
            double endAngle = startAngle + sweepAngle;

            // Convert angles to radians
            double startRad = (Math.PI / 180) * startAngle;
            double endRad = (Math.PI / 180) * endAngle;

            // Calculate start and end points
            System.Windows.Point startPoint = new System.Windows.Point(
                centerX + radius * Math.Cos(startRad),
                centerY + radius * Math.Sin(startRad));

            System.Windows.Point endPoint = new System.Windows.Point(
                centerX + radius * Math.Cos(endRad),
                centerY + radius * Math.Sin(endRad));

            bool isLargeArc = sweepAngle > 180.0;

            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
            ArcSegment arc = new ArcSegment
            {
                Point = endPoint,
                Size = new System.Windows.Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise,
                RotationAngle = 0
            };
            figure.Segments.Add(arc);
            geometry.Figures.Add(figure);

            System.Windows.Media.Brush brush;
            try
            {
                var converted = new System.Windows.Media.BrushConverter().ConvertFromString(colorHex);
                brush = (System.Windows.Media.Brush?)converted ?? System.Windows.Media.Brushes.Gray;
            }
            catch
            {
                brush = System.Windows.Media.Brushes.Gray;
            }

            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                StrokeEndLineCap = System.Windows.Media.PenLineCap.Round
            };

            return path;
        }
    }
}
