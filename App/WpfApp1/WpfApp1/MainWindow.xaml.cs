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
using System.Xml.Schema;
using SharpDX.DirectInput;
using System.Collections.ObjectModel;

public unsafe struct splineInfo
{
    public fixed double xPos[10];  // Fixed-size array of 10 elements
    public fixed double yPos[10];   // Fixed-size array of 5 elements
    public double xMin;
    public double xMax;
    public int numberOfSplinePoints;
    public double yMin;
    public double yMax;

    public fixed double a[10];   // Fixed-size array of 5 elements
    public fixed double b[10];   // Fixed-size array of 5 elements
    public fixed double c[10];   // Fixed-size array of 5 elements
    public fixed double d[10];   // Fixed-size array of 5 elements
    public fixed double h[10];   // Fixed-size array of 5 elements
}


namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private FFBWheelController controller;
        private CancellationTokenSource _cancellationTokenSource;
        private string ffbWheelDeviceGuid_str = "";

        private UInt16 ffbUpdateInterval = 0;

        double devicePosGlobal_fl64 = 0;
        double ffbForce_global_fl64 = 0;

        long executionTimeMeasuredInMs_FFBtask_l = 0;




        // Declare the ObservableCollection
        public ObservableCollection<WheelChoice> MyComboBoxItems { get; set; }



        private splineInfo splineInfo_st;


        private const int PointRadius = 5;
        private const int CanvasWidth = 300;
        private const int CanvasHeight = 200;
        private List<Ellipse> points;
        private Ellipse selectedPoint = null;
        private bool isDragging = false;


        private List<Ellipse> state_point;

        private DirectInput directInput;



        public MainWindow()
        {
            InitializeComponent();
            CreateInitialPoints();
            DrawSpline();
            DrawLine();

            directInput = new DirectInput();
            //var devices = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices);


            MyComboBoxItems = new ObservableCollection<WheelChoice>();
            DataContext = this;

            UpdateComboBox();

            

        }


        public class WheelChoice
        {
            public WheelChoice(string display, string value)
            {
                Display = display;
                Value = value;
            }

            public string Value { get; set; }
            public string Display { get; set; }
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
                var point = CreatePoint(spacing * i - 0*PointRadius / 2, MyCanvas.Height - (vert_spacing * i - 0*PointRadius / 2));
                point.MouseLeftButtonUp += MyCanvas_MouseUp;
                points.Add(point);
                MyCanvas.Children.Add(point);
            }

            state_point = new List<Ellipse>();
            var point_ = CreatePoint(0, 0);
            MyCanvas.Children.Add(point_);
            state_point.Add(point_);


            

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

        private void DrawLine()
        {

            // Create a Line object
            Line verticalLine = new Line
            {
                X1 = 0, // Starting X coordinate
                Y1 = 00,  // Starting Y coordinate
                X2 = 0, // Ending X coordinate (same as X1 for a vertical line)
                Y2 = MyCanvas.Height, // Ending Y coordinate
                Stroke = Brushes.Black,     // Line color
                StrokeThickness = 2         // Line thickness
            };

            // Add the line to the Canvas
            MyCanvas.Children.Add(verticalLine);

        }
        private void DrawSpline()
        {
            //MyCanvas.Children.Clear();


            // Remove all elements of a specific type, for example, all Rectangle elements
            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (UIElement child in MyCanvas.Children)
            {
                if ( (child is Ellipse) ||(child is Polyline))  // Replace Rectangle with the type you're targeting
                {
                    elementsToRemove.Add(child);  // Add to a temporary list to avoid modifying the collection while iterating
                }
            }

            // Now remove the elements
            foreach (UIElement element in elementsToRemove)
            {
                MyCanvas.Children.Remove(element);
            }



            // Redraw the points
            foreach (var point in points)
            {
                MyCanvas.Children.Add(point);
            }


            //DrawLine();





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
        unsafe private List<Point> GetNaturalSpline(double[] x, double[] y)
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
                splineInfo_st.h[i] = h[i];
            }




            for (int i = 0; i < n; i++)
            {
                splineInfo_st.xPos[i] = x[i];
                splineInfo_st.yPos[i] = y[i];
            }

            splineInfo_st.xMin = x[0];
            splineInfo_st.xMax = x[n-1];
            splineInfo_st.numberOfSplinePoints = n;
            splineInfo_st.yMin = y[0];
            splineInfo_st.yMax = y[n - 1];


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

                splineInfo_st.a[j] = a[j];
                splineInfo_st.b[j] = b[j];
                splineInfo_st.c[j] = c[j];
                splineInfo_st.d[j] = d[j];
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



        unsafe private double GetSplineValue(double x)
        {
            
            double splineY = 0;

            uint selectedSplineIdx = 0;

            double xNormalized = Math.Abs(x * (splineInfo_st.xMax - splineInfo_st.xMin));

            double sign = 1;
            if (x < 0)
            { sign = -1; }

            

            for (uint splineIdx = 0; splineIdx < splineInfo_st.numberOfSplinePoints; splineIdx++)
            {
                if (splineInfo_st.xPos[splineIdx] >= xNormalized)
                {
                    break;
                }
                selectedSplineIdx = splineIdx;
            }

            double xPosWidth = splineInfo_st.xPos[1] - splineInfo_st.xPos[0];

            double xPosDelta = xNormalized - splineInfo_st.xPos[selectedSplineIdx];

            if (xPosDelta < 0)
            {
                xPosDelta = 0;
            }

            if (xPosDelta > xPosWidth)
            {
                xPosDelta = xPosWidth;
            }

            double t = xPosDelta / xPosWidth;

            double splineX = splineInfo_st.xPos[selectedSplineIdx] + t * splineInfo_st.h[selectedSplineIdx];


            splineY = splineInfo_st.a[selectedSplineIdx] + splineInfo_st.b[selectedSplineIdx] * (splineX - splineInfo_st.xPos[selectedSplineIdx]) + splineInfo_st.c[selectedSplineIdx] * Math.Pow(splineX - splineInfo_st.xPos[selectedSplineIdx], 2) + splineInfo_st.d[selectedSplineIdx] * Math.Pow(splineX - splineInfo_st.xPos[selectedSplineIdx], 3);


            double splineY_ = splineInfo_st.yMin - splineY;

            // Clamp the value between minValue and maxValue
            double clampedValue = Math.Clamp(splineY_, splineInfo_st.yMax, splineInfo_st.yMin);


            

            return sign * clampedValue;
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
            controller = new FFBWheelController(windowHandle, myTextBox, ffbWheelDeviceGuid_str, MyCanvas);

            //var tmp = MyCanvas;

            myTextBox2.Text = "Position (normalized)";
            //myTextBox2.Text += "\n" + "Position filtered";
            myTextBox2.Text += "\n" + "Repition time in ms";
            //myTextBox2.Text += "\n" + "Force output";
            myTextBox2.Text += "\n" + "Target force (normalized)";

            // Start polling in a background thread with cancellation token
            try
            {
                //await Task.Run(() => StartPollingFFB(_cancellationTokenSource.Token));
                Task.Run(() => StartPollingFFB(_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Polling stopped.");
            }


            // Start polling in a background thread with cancellation token
            try
            {
                Task.Run(() => StartPollingGUI(_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Polling stopped.");
            }





        }



        // Event handler for when the ComboBox selection changes
        private void myComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            string tmp = (string)myComboBox.SelectedValue;

            ffbWheelDeviceGuid_str  = tmp;

        }


        // Event handler for Slider value change
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Update the TextBlock with the current value of the slider
            //sliderValueText.Text = $"Current Value: {mySlider.Value:F0}";

            ffbUpdateInterval = (UInt16)mySlider.Value;
        }


        // Method to get all Line elements from a Canvas
        private List<Line> GetAllLinesFromCanvas(Canvas canvas)
        {
            List<Line> lines = new List<Line>();

            // Iterate over each child element in the Canvas
            foreach (var child in canvas.Children)
            {
                // Check if the child is a Line
                if (child is Line line)
                {
                    // Add the line to the list
                    lines.Add(line);
                }
            }

            return lines;
        }


        // Event handler to programmatically update the ComboBox items

        private void UpdateComboBox()
        {


            MyComboBoxItems.Clear();

            var devices = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices);
            //var devices = directInput.GetDevices(DeviceType.ControlDevice, DeviceEnumerationFlags.AllDevices);
            

            var wheelDeviceSelectionArray = new List<WheelChoice>();
            foreach (var device in devices)
            {
                MyComboBoxItems.Add(new WheelChoice(device.InstanceName, device.ProductGuid.ToString()));
            }

        }



        private void myComboBox_DropDownOpened(object sender, EventArgs e)
        {
            UpdateComboBox();
        }


        private void StartPollingFFB(CancellationToken cancellationToken)
        {
            // Erstelle eine Stoppuhr
            Stopwatch stopwatch = new Stopwatch();

            // Polling loop for force feedback updates
            while (!cancellationToken.IsCancellationRequested) // Check if cancellation is requested
            {
                // Starte die Messung
                stopwatch.Restart();

                // get device state
                double devicePos = controller.ReadDeviceState();
                double targetForce = GetSplineValue(devicePos);
                devicePosGlobal_fl64 = devicePos;

                targetForce /= 200;// MyCanvas.Height;

                // redraw vertical line
                //MyCanvas.Dispatcher.Invoke(() =>
                //{
                //    var lines = GetAllLinesFromCanvas(MyCanvas);

                //    lines[0].X1 = Math.Abs(devicePos) * MyCanvas.Width;
                //    lines[0].X2 = lines[0].X1;
                //});





                ffbForce_global_fl64 = controller.ApplySpringEffect(devicePos, targetForce);
                System.Threading.Thread.Sleep(ffbUpdateInterval); // Small delay for polling

                // Stoppe die Zeitmessung
                stopwatch.Stop();

                executionTimeMeasuredInMs_FFBtask_l = stopwatch.ElapsedMilliseconds;
            }
        }



        private void StartPollingGUI(CancellationToken cancellationToken)
        {
            // Erstelle eine Stoppuhr
            //Stopwatch stopwatch = new Stopwatch();
            //long executionTimeMeasuredInMs_l = 0;

            // Polling loop for force feedback updates
            while (!cancellationToken.IsCancellationRequested) // Check if cancellation is requested
            {
                // Starte die Messung
                //stopwatch.Restart();

                // redraw vertical line
                MyCanvas.Dispatcher.Invoke(() =>
                {
                    var lines = GetAllLinesFromCanvas(MyCanvas);

                    lines[0].X1 = Math.Abs(devicePosGlobal_fl64) * MyCanvas.Width;
                    lines[0].X2 = lines[0].X1;
                });




                // Zugriff auf TextBox.Text über den Dispatcher
                string userInput = null;
                myTextBox.Dispatcher.Invoke(() =>
                {
                    // Hier wird der Zugriff auf die TextBox innerhalb des UI-Threads ausgeführt
                    //userInput = myTextBox.Text;

                    myTextBox.Text = Math.Round(devicePosGlobal_fl64,3).ToString();
                    //myTextBox.Text += "\n" + position_filtered_fl.ToString();
                    myTextBox.Text += "\n" + executionTimeMeasuredInMs_FFBtask_l.ToString();
                    //myTextBox.Text += "\n" + forceOutput.ToString();
                    myTextBox.Text += "\n" + Math.Round(ffbForce_global_fl64,3).ToString();



                });


                System.Threading.Thread.Sleep(20); // Small delay for polling
                // Stoppe die Zeitmessung
                //stopwatch.Stop();

                //executionTimeMeasuredInMs_FFBtask_l = stopwatch.ElapsedMilliseconds;
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
