
//using System;
//using HidSharp;
//using HidSharp.Reports;
//using HidLibrary;


// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

//private const int VendorId = 0x0483;  // Replace with your steering wheel's Vendor ID
//private const int ProductId = 0xA355; // Replace with your steering wheel's Product ID

using System;
using SharpDX.DirectInput;

using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;


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
    private float Kp = 0.1f;  // Proportional
    private float Ki = 0.01f; // Integral
    private float Kd = 0.05f; // Derivative

    private float integral = 0;
    private float previousError = 0;

    // Define your wheel's VID and PID here
    private readonly int targetVID = 0x0483; // Replace with your Wheel's VID (e.g., Logitech = 0x046D)
    private readonly int targetPID = 0xA355; // Replace with your Wheel's PID (e.g., G29 = 0xC24F)

    public FFBWheelController()
    {
        // Initialize DirectInput
        directInput = new DirectInput();

        // Find the specific FFB wheel using VID and PID
        var devices = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices);
        // Erstellen Sie ein unsichtbares Fenster


        // Holen Sie sich das Handle des Konsolenfensters
        IntPtr consoleWindowHandle = GetConsoleWindow();

        // Erstellen eines unsichtbaren Fensters, um ein gültiges Fensterhandle zu erhalten
        Form dummyForm = new Form
        {
            Visible = false // Das Fenster wird nicht angezeigt
        };

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
                if (consoleWindowHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Fehler beim Abrufen des Konsolenfenster-Handles.");
                    return;
                }
                wheel.SetCooperativeLevel(dummyForm.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);


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

    public void ApplySpringEffect()
    {
        if (wheel == null) return;

        // Poll the current state of the wheel
        wheel.Poll();
        var state = wheel.GetCurrentState();

        Console.WriteLine($"Pos: {state.X} ");

        // Get the wheel's current position (assuming X axis is the steering wheel axis)
        int currentPosition = state.X;

        if (centerPosHasBeenInitialized == true)
        { 
            centerPosition = state.X; 
        }
        

        // Calculate the error (difference between current position and center)
        float error = centerPosition - currentPosition;

        // PID calculation
        integral += error;
        float derivative = error - previousError;

        // PID output is the force to apply to the wheel
        float forceOutput = (Kp * error) + (Ki * integral) + (Kd * derivative);

        // Apply force feedback based on PID output
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

class Program
{
    static void Main(string[] args)
    {
        FFBWheelController controller = new FFBWheelController();

        while (true)
        {
            controller.ApplySpringEffect();
            System.Threading.Thread.Sleep(10); // Small delay for polling
        }
    }
}


