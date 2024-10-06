using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using System;
using SharpDX.DirectInput;
using System.Runtime.InteropServices;
using System.Windows.Interop; // Include this namespace to use WindowInteropHelper


namespace WpfApp
{


    public class FFBWheelController
    {



        // Importieren der GetConsoleWindow-Funktion aus der User32.dll
        //[DllImport("user32.dll")]
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();



        private Joystick wheel;
        private DirectInput directInput;

        private bool centerPosHasBeenInitialized = false;
        private int centerPosition = 33000; // Assuming 0 is the center position

        // PID coefficients
        private float Kp = 0.5f;  // Proportional
        private float Ki = 0.00f; // Integral
        private float Kd = 0.0f; // Derivative

        private float integral = 0;
        private float previousError = 0;

        // Define your wheel's VID and PID here
        private readonly int targetVID = 0x0483; // Replace with your Wheel's VID (e.g., Logitech = 0x046D)
        private readonly int targetPID = 0xA355; // Replace with your Wheel's PID (e.g., G29 = 0xC24F)

        public FFBWheelController(IntPtr formHandle)
        {
            // Initialize DirectInput
            directInput = new DirectInput();

            // Find the specific FFB wheel using VID and PID
            var devices = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices);
            // Erstellen Sie ein unsichtbares Fenster


            //// Holen Sie sich das Handle des Konsolenfensters
            //IntPtr consoleWindowHandle = GetConsoleWindow();

            // Erstellen eines unsichtbaren Fensters, um ein gültiges Fensterhandle zu erhalten
            //Form dummyForm = new Form
            //{
            //    Visible = false // Das Fenster wird nicht angezeigt
            //};

            foreach (var device in devices)
            {
                // Retrieve VID and PID from the device
                int vid = device.ProductGuid.ToString("X").Length >= 4 ? Convert.ToInt32(device.ProductGuid.ToString().Substring(0, 4), 16) : 0;
                int pid = device.ProductGuid.ToString("X").Length >= 8 ? Convert.ToInt32(device.ProductGuid.ToString().Substring(4, 4), 16) : 0;

                if (true)//(vid == targetVID && pid == targetPID)
                {
                    // Initialize the wheel device
                    wheel = new Joystick(directInput, device.InstanceGuid);



                    // Set the cooperative level
                    //wheel.SetCooperativeLevel(consoleWindowHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                    //wheel.Acquire();



                    // Überprüfen, ob das Handle gültig ist
                    //if (consoleWindowHandle == IntPtr.Zero)
                    //{
                    //    Console.WriteLine("Fehler beim Abrufen des Konsolenfenster-Handles.");
                    //    return;
                    //}
                    //wheel.SetCooperativeLevel(formHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                    wheel.SetCooperativeLevel(formHandle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);


                    //// Acquire the wheel
                    wheel.Acquire();
                    Console.WriteLine($"Found and acquired FFB wheel with VID: {vid:X} and PID: {pid:X}");
                    break;
                }
            }

            if (wheel == null)
            {
                Console.WriteLine($"No FFB wheel found with VID: {targetVID:X} and PID: {targetPID:X}");
            }
        }

        public void ApplySpringEffect(IntPtr windowHandle)
        {
            if (wheel == null) return;

            // Poll the current state of the wheel
            wheel.Poll();
            var state = wheel.GetCurrentState();

            Console.WriteLine($"Pos: {state.X} ");

            // Get the wheel's current position (assuming X axis is the steering wheel axis)
            int currentPosition = state.X;

            if (centerPosHasBeenInitialized == false)
            {
                centerPosition = state.X;
                centerPosHasBeenInitialized = true;
            }


            // Calculate the error (difference between current position and center)
            float error = centerPosition - currentPosition;

            // PID calculation
            integral += error;
            float derivative = error - previousError;

            // PID output is the force to apply to the wheel
            float forceOutput = (Kp * error) + (Ki * integral) + (Kd * derivative);
            forceOutput *= -1;

            // Apply force feedback based on PID output

            // Get the text from the TextBox and show it in a MessageBox
            //string userInput = myTextBox.Text;
            //MessageBox.Show($"You entered: {userInput}");


            SetForceFeedback(forceOutput);

            // Update previous error
            previousError = error;
        }

        private void SetForceFeedback(float force)
        {
            if (wheel == null) return;

            // Ensure the force is within the valid range of the device
            int clampedForce = (int)Math.Clamp(force, -10000, 10000); // Range for force feedback varies by device

            // Get the effect information for a constant force effect
            var effects = wheel.GetEffects();

            // Using FirstOrDefault() to find the Constant Force effect
            var constantForceEffectInfo = effects.FirstOrDefault(e => e.Name.Contains("Constant"));

            if (constantForceEffectInfo == null)
            {
                Console.WriteLine("Constant force effect not supported on this device.");
                return;
            }


            // Create a ConstantForce structure
            var constantForce = new ConstantForce
            {
                Magnitude = clampedForce
            };


            // Create the effect parameters for the constant force effect
            var effectParameters = new EffectParameters
            {
                Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets, // Set relevant flags
                Duration = int.MaxValue, // Infinite duration
                SamplePeriod = 0,
                Gain = 10000, // Maximum gain
                TriggerButton = -1,
                TriggerRepeatInterval = int.MaxValue,
                StartDelay = 0,
                Axes = new[] { 0 }, // Set the axes for the effect
                Directions = new[] { 0 }, // Specify the direction of the force along the axes
                Envelope = null,
                Parameters = constantForce
            };



            try
            {
                // Create the effect object using the effect info and parameters
                var effect = new Effect(wheel, constantForceEffectInfo.Guid, effectParameters);

                // Acquire the device exclusively
                //wheel.Acquire();


                // Stop all effects and reset
                wheel.SendForceFeedbackCommand(ForceFeedbackCommand.StopAll);
                wheel.SendForceFeedbackCommand(ForceFeedbackCommand.Reset);

                // Start the effect
                effect.Start();
            }
            catch (SharpDX.SharpDXException ex)
            {
                Console.WriteLine($"Error creating effect: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (wheel != null)
            {
                wheel.Unacquire();
                wheel.Dispose();
            }

            directInput.Dispose();
        }
    }




    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            // Show a message box when the button is clicked
            MessageBox.Show("Hello, World!");


            // Retrieve the handle (HWND) of the current window
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            FFBWheelController controller = new FFBWheelController(windowHandle);

            while (true)
            {
                controller.ApplySpringEffect(windowHandle);
                System.Threading.Thread.Sleep(10); // Small delay for polling
            }


        }
    }
}
