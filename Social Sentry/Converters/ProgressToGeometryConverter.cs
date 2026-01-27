using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Social_Sentry.Converters
{
    public class ProgressToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // Ensure progress is between 0 and 1
                progress = Math.Max(0, Math.Min(1, progress));

                // Size of the box (assumed square)
                double size = 120; // Default size if not passed
                if (parameter is string paramStr && double.TryParse(paramStr, out double parsedSize))
                {
                    size = parsedSize;
                }

                double strokeWidth = 6;
                double radius = (size - strokeWidth) / 2;
                Point center = new Point(size / 2, size / 2);

                if (progress >= 1)
                {
                    // Full circle
                    EllipseGeometry circle = new EllipseGeometry(center, radius, radius);
                    return circle;
                }

                // Calculate end point
                double startAngle = -90; // Top
                double endAngle = startAngle + (progress * 360);

                // Convert to radians
                double startRad = startAngle * Math.PI / 180;
                double endRad = endAngle * Math.PI / 180;

                Point startPoint = new Point(
                    center.X + radius * Math.Cos(startRad),
                    center.Y + radius * Math.Sin(startRad));

                Point endPoint = new Point(
                    center.X + radius * Math.Cos(endRad),
                    center.Y + radius * Math.Sin(endRad));

                bool isLargeArc = progress > 0.5;

                PathFigure figure = new PathFigure();
                figure.StartPoint = startPoint;

                ArcSegment arc = new ArcSegment(
                    endPoint,
                    new Size(radius, radius),
                    0,
                    isLargeArc,
                    SweepDirection.Clockwise,
                    true);

                figure.Segments.Add(arc);

                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                return geometry;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
