using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace WpfApp
{
    public class FFBWheelController : IDisposable
    {

        private TextBox myTextBox;  // Store a reference to the TextBox

        float position_filtered_fl = 0.0f;

        private Joystick wheel;
        private DirectInput directInput;

        private SharpDX.DirectInput.Effect constantForceEffect; // Store the constant force effect
        private EffectParameters effectParameters;
        bool effectWasInitialized = false;

        private bool centerPosHasBeenInitialized = false;
        private float centerPosition = 0.0f; // Default center position

        // PID coefficients
        private float Kp = 5.0f;  // Proportional
        private float Ki = 0.01f; // Integral
        private float Kd = 0.0f;  // Derivative

        private float integral = 0;
        private float previousError = 0;

        private readonly int targetVID = 0x0483; // Replace with actual Wheel's VID
        private readonly int targetPID = 0xA355; // Replace with actual Wheel's PID

        public FFBWheelController(IntPtr formHandle, TextBox textBox)
        {

            myTextBox = textBox;  // Save the TextBox reference


            // Initialize DirectInput and find the wheel device
            directInput = new DirectInput();
            var devices = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices);

            foreach (var device in devices)
            {
                int pid = device.ProductGuid.ToString("X").Length >= 4 ? Convert.ToInt32(device.ProductGuid.ToString().Substring(0, 4), 16) : 0;
                int vid = device.ProductGuid.ToString("X").Length >= 8 ? Convert.ToInt32(device.ProductGuid.ToString().Substring(4, 4), 16) : 0;

                // Check if the device matches the target VID and PID
                if (vid == targetVID && pid == targetPID)
                {
                    wheel = new Joystick(directInput, device.InstanceGuid);
                    //wheel.SetCooperativeLevel(formHandle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
                    wheel.SetCooperativeLevel(formHandle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
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

        public void ApplySpringEffect(long executionTimeMeasuredInMs_l)
        {
            if (wheel == null) return;

            //wheel.Poll();
            var state = wheel.GetCurrentState();

            float currentPosition = state.X;
            currentPosition /= 65536.0f;
            currentPosition -= 0.5f;
            currentPosition *= 20000.0f;

            float alpha = 0.0f;
            position_filtered_fl = position_filtered_fl * alpha + currentPosition * (1.0f - alpha);
            //currentPosition = position_filtered_fl;


            if (!centerPosHasBeenInitialized)
            {
                centerPosition = currentPosition;
                centerPosHasBeenInitialized = true;
            }

            


            // PID calculation
            float error = centerPosition - position_filtered_fl;
            integral += error;
            float derivative = error - previousError;
            float forceOutput = (Kp * error) + (Ki * integral) + (Kd * derivative);
            forceOutput *= -1;

            SetForceFeedback(forceOutput);
            previousError = error;


            // Zugriff auf TextBox.Text über den Dispatcher
            //string userInput = null;
            myTextBox.Dispatcher.Invoke(() =>
            {
                // Hier wird der Zugriff auf die TextBox innerhalb des UI-Threads ausgeführt
                //userInput = myTextBox.Text;

                myTextBox.Text = currentPosition.ToString();
                myTextBox.Text += "\n" + position_filtered_fl.ToString();
                myTextBox.Text += "\n" + executionTimeMeasuredInMs_l.ToString();
                myTextBox.Text += "\n" + forceOutput.ToString();
            });
        }

        private void SetForceFeedback(float force)
        {
            if (wheel == null) return;


            
                int clampedForce = (int)Math.Clamp(force, -10000, 10000);
                var effects = wheel.GetEffects();
                var constantForceEffectInfo = effects.FirstOrDefault(e => e.Name.Contains("Constant"));

                if (constantForceEffectInfo == null)
                {
                    Console.WriteLine("Constant force effect not supported on this device.");
                    return;
                }

                var constantForce = new ConstantForce { Magnitude = clampedForce };
                // Create the effect parameters for the constant force effect
                var effectParameters_lcl = new EffectParameters
                {
                    Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets, // Set relevant flags
                    Duration = int.MaxValue, // Infinite duration
                    SamplePeriod = 0,
                    Gain = 10000, // Maximum gain
                    TriggerButton = -1,
                    TriggerRepeatInterval = 0,//int.MaxValue,
                    StartDelay = 0,
                    Axes = new[] { 0 }, // Set the axes for the effect
                    Directions = new[] { 0 }, // Specify the direction of the force along the axes
                    Envelope = null,
                    Parameters = constantForce
                };

            if (effectWasInitialized == false)
            {
                try
                {

                    
                    var effect = new SharpDX.DirectInput.Effect(wheel, constantForceEffectInfo.Guid, effectParameters_lcl);
                    wheel.SendForceFeedbackCommand(ForceFeedbackCommand.StopAll);
                    wheel.SendForceFeedbackCommand(ForceFeedbackCommand.Reset);
                    effect.Start(1);


                    constantForceEffect = effect;
                    effectParameters = effectParameters_lcl;


                    //for (UInt16 loopIdx = 0; loopIdx < 100; loopIdx++)
                    //{

                    //    var constantForce_lcl = new ConstantForce { Magnitude = loopIdx };

                    //    effectParameters.Parameters = constantForce_lcl;

                    //    effect.SetParameters(effectParameters, EffectParameterFlags.Start); // Aktualisiere den Effekt

                    //}

                    effectWasInitialized = true;




                }
                catch (SharpDX.SharpDXException ex)
                {
                    Console.WriteLine($"Error creating effect: {ex.Message}");
                }
            }
            else
            {

                // ChatGPT command: wie kann ich mit using SharpDX.DirectInput;  einen aktiven ffb effekt manipulieren?
                // Effekt stoppen, bevor die Parameter geändert werden
                var constantForce_lcl = new ConstantForce { Magnitude = clampedForce };
                effectParameters.Parameters = constantForce_lcl;
                //constantForceEffect.SetParameters(effectParameters, EffectParameterFlags.Start | EffectParameterFlags.TypeSpecificParameters); // Aktualisiere den Effekt
                constantForceEffect.SetParameters(effectParameters, EffectParameterFlags.TypeSpecificParameters); // Aktualisiere den Effekt


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
}
