using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AngularSimulator.Graphic
{
    class DrawWheel
    {
        public Path GeometryDrawingExample()
        {

            RectangleGeometry myRectangleGeometry = new RectangleGeometry();
            myRectangleGeometry.Rect = new Rect(120, 120, 25, 25);
            
            CombinedGeometry combinedGeometry = new CombinedGeometry();
            EllipseGeometry outterEllipse = new EllipseGeometry(new Rect(0, 0, 120, 120));
            EllipseGeometry innerEllipse = new EllipseGeometry(new Rect(10, 10, 100, 100));
            combinedGeometry.Geometry1 = outterEllipse;
            combinedGeometry.Geometry2 = innerEllipse;
            combinedGeometry.GeometryCombineMode = GeometryCombineMode.Xor;

            GeometryGroup shapes = new GeometryGroup();
            shapes.Children.Add(combinedGeometry);
            shapes.Children.Add(myRectangleGeometry);

            Path myPath = new Path();
            myPath.Fill = Brushes.LemonChiffon;
            myPath.Stroke = Brushes.Black;
            myPath.StrokeThickness = 1;
            myPath.Data = shapes;

            return myPath;

        }
    }
}
