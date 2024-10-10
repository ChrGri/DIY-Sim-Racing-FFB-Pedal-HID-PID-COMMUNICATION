using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;



using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private FFBWheelController controller;
        private CancellationTokenSource _cancellationTokenSource;




        private const int PointRadius = 5;
        private const int CanvasWidth = 300;
        private const int CanvasHeight = 200;
        private List<Ellipse> points;
        private Ellipse selectedPoint = null;
        private bool isDragging = false;




        public MainWindow()
        {
            InitializeComponent();
            CreateInitialPoints();
            DrawSpline();
        }



        private void CreateInitialPoints()
        {
            points = new List<Ellipse>();
            double[] initialX = { 50, 100, 150, 200, 250, 300 };
            double[] initialY = { 50, 100, 150, 100, 50, 100 };

            double spacing = MyCanvas.Width / 5;
            double vert_spacing = MyCanvas.Height / 5;
            for (int i = 0; i < initialX.Length; i++)
            {
                var point = CreatePoint(spacing * i - PointRadius / 2, MyCanvas.Height - (vert_spacing * i - PointRadius / 2));
                point.MouseLeftButtonUp += MyCanvas_MouseUp;
                points.Add(point);
                MyCanvas.Children.Add(point);
            }
        }

        private Ellipse CreatePoint(double x, double y)
        {
            Ellipse point = new Ellipse
            {
                Width = PointRadius * 2,
                Height = PointRadius * 2,
                Fill = Brushes.Blue
            };

            Canvas.SetLeft(point, x - PointRadius);
            Canvas.SetTop(point, y - PointRadius);

            return point;
        }

        private void MyCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(MyCanvas);

            foreach (var point in points)
            {
                double pointX = Canvas.GetLeft(point) + PointRadius;
                double pointY = Canvas.GetTop(point) + PointRadius;

                if (Math.Abs(mousePos.X - pointX) < PointRadius && Math.Abs(mousePos.Y - pointY) < PointRadius)
                {
                    selectedPoint = point;
                    isDragging = true;
                    break;
                }
            }
        }

        private void MyCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedPoint != null)
            {
                Point mousePos = e.GetPosition(MyCanvas);
                double newY = Math.Max(PointRadius, Math.Min(mousePos.Y, CanvasHeight - PointRadius));

                Canvas.SetTop(selectedPoint, newY - PointRadius);
                DrawSpline();
            }
        }

        private void MyCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            selectedPoint = null;
        }

        private void DrawSpline()
        {
            MyCanvas.Children.Clear();

            // Redraw the points
            foreach (var point in points)
            {
                MyCanvas.Children.Add(point);
            }

            // Gather X and Y coordinates of points
            double[] xValues = new double[points.Count];
            double[] yValues = new double[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                xValues[i] = Canvas.GetLeft(points[i]) + PointRadius;
                yValues[i] = Canvas.GetTop(points[i]) + PointRadius;
            }

            // Get natural cubic spline segments
            var splinePoints = GetNaturalSpline(xValues, yValues);

            // Draw the spline
            Polyline splineLine = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Points = new PointCollection(splinePoints)
            };

            MyCanvas.Children.Add(splineLine);
        }

        // Method to calculate natural cubic spline points
        private List<Point> GetNaturalSpline(double[] x, double[] y)
        {
            List<Point> splinePoints = new List<Point>();
            int n = x.Length;

            if (n < 2) return splinePoints; // Need at least 2 points to draw a spline

            double[] a = new double[n];
            double[] b = new double[n];
            double[] d = new double[n];
            double[] h = new double[n - 1];
            double[] alpha = new double[n - 1];
            double[] c = new double[n];
            double[] l = new double[n];
            double[] mu = new double[n];
            double[] z = new double[n];

            // Step 1: Calculate h and alpha
            for (int i = 0; i < n - 1; i++)
            {
                h[i] = x[i + 1] - x[i];
                alpha[i] = (3 / h[i]) * (y[i + 1] - y[i]) - (3 / h[Math.Max(0, i - 1)]) * (y[i] - y[Math.Max(0, i - 1)]);
            }

            // Step 2: Set up the system of equations for c
            l[0] = 1;
            mu[0] = 0;
            z[0] = 0;

            for (int i = 1; i < n - 1; i++)
            {
                l[i] = 2 * (x[i + 1] - x[i - 1]) - h[i - 1] * mu[i - 1];
                mu[i] = h[i] / l[i];
                z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
            }

            l[n - 1] = 1;
            z[n - 1] = 0;
            c[n - 1] = 0;

            // Step 3: Solve for b, d, and c
            for (int j = n - 2; j >= 0; j--)
            {
                c[j] = z[j] - mu[j] * c[j + 1];
                b[j] = (y[j + 1] - y[j]) / h[j] - h[j] * (c[j + 1] + 2 * c[j]) / 3;
                d[j] = (c[j + 1] - c[j]) / (3 * h[j]);
                a[j] = y[j];
            }

            // Step 4: Generate points on the spline
            int steps = 50; // Number of steps between points for smoothness

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j <= steps; j++)
                {
                    double t = j / (double)steps;
                    double splineX = x[i] + t * h[i];
                    double splineY = a[i] + b[i] * (splineX - x[i]) + c[i] * Math.Pow(splineX - x[i], 2) + d[i] * Math.Pow(splineX - x[i], 3);

                    splinePoints.Add(new Point(splineX, splineY));
                }
            }

            return splinePoints;
        }






        /*        Buitton         */
        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            // Show a message box when the button is clicked
            MessageBox.Show("Be carefull! FFB spring effect is starting after button press");

            // Retrieve the handle (HWND) of the current window
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            // Initialize the CancellationTokenSource for task cancellation
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize the FFBWheelController with the window handle
            controller = new FFBWheelController(windowHandle, myTextBox);

            // Start polling in a background thread with cancellation token
            try
            {
                await Task.Run(() => StartPolling(_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Polling stopped.");
            }
        }

        private void StartPolling(CancellationToken cancellationToken)
        {
            // Erstelle eine Stoppuhr
            Stopwatch stopwatch = new Stopwatch();
            long executionTimeMeasuredInMs_l = 0;

            // Polling loop for force feedback updates
            while (!cancellationToken.IsCancellationRequested) // Check if cancellation is requested
            {
                // Starte die Messung
                stopwatch.Restart();

                controller.ApplySpringEffect(executionTimeMeasuredInMs_l);
                System.Threading.Thread.Sleep(2); // Small delay for polling

                // Stoppe die Zeitmessung
                stopwatch.Stop();

                executionTimeMeasuredInMs_l = stopwatch.ElapsedMilliseconds;
            }
        }

        // Override the OnClosed method to clean up resources when the window is closed
        protected override void OnClosed(EventArgs e)
        {
            // Signal the cancellation token to stop the task
            _cancellationTokenSource?.Cancel();


            // Dispose of the controller when the window is closed
            controller?.Dispose();
            base.OnClosed(e);

        }
    }
}
