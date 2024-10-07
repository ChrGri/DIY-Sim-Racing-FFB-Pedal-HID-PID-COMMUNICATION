using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private FFBWheelController controller;
        private CancellationTokenSource _cancellationTokenSource;


        public MainWindow()
        {
            InitializeComponent();
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            // Show a message box when the button is clicked
            MessageBox.Show("Hello, World!");

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
