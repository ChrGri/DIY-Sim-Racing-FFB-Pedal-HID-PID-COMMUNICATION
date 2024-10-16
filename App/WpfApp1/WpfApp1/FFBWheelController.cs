﻿using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using static WpfApp.MainWindow;

namespace WpfApp
{
    public class FFBWheelController : IDisposable
    {

        private TextBox myTextBox;  // Store a reference to the TextBox
        private Canvas myCanvas;

        float position_filtered_fl = 0.0f;


        double axisMinValue_fl64 = 0;
        double axisMaxValue_fl64 = 10000;
        double axisSaturation_fl64 = 10000;


        private Joystick wheel;
        private DirectInput directInput;

        private SharpDX.DirectInput.Effect constantForceEffect; // Store the constant force effect
        private EffectParameters effectParameters;
        bool effectWasInitialized = false;

        private bool centerPosHasBeenInitialized = false;
        private float centerPosition = 0.0f; // Default center position

        // PID coefficients
        private float Kp = 1.0f;  // Proportional
        private float Ki = 0.0f; // Integral
        private float Kd = 0.05f;  // Derivative

        private float integral = 0;
        private float previousError = 0;

        private readonly int targetVID = 0x0483; // Replace with actual Wheel's VID
        private readonly int targetPID = 0xA355; // Replace with actual Wheel's PID

        public FFBWheelController(IntPtr formHandle, TextBox textBox, string ffbWheelDeviceGuid_str, Canvas canvas)
        {

            myTextBox = textBox;  // Save the TextBox reference
            myCanvas = canvas;

            // Initialize DirectInput and find the wheel device
            directInput = new DirectInput();
            var devices = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices);

            foreach (var device in devices)
            {
                int pid = device.ProductGuid.ToString("X").Length >= 4 ? Convert.ToInt32(device.ProductGuid.ToString().Substring(0, 4), 16) : 0;
                int vid = device.ProductGuid.ToString("X").Length >= 8 ? Convert.ToInt32(device.ProductGuid.ToString().Substring(4, 4), 16) : 0;

                // Check if the device matches the target VID and PID
                if (device.ProductGuid.ToString() == ffbWheelDeviceGuid_str)
                //if (vid == targetVID && pid == targetPID)
                {
                    wheel = new Joystick(directInput, device.InstanceGuid);
                    //wheel.SetCooperativeLevel(formHandle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
                    wheel.SetCooperativeLevel(formHandle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
                    wheel.Acquire();

                    // Get axis range for each axis
                    foreach (var deviceObject in wheel.GetObjects())
                    {
                        // Check if the object is an axis by comparing its type with DeviceObjectTypeFlags.Axis
                        if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                        {
                            // Get object properties to retrieve axis range
                            var axisProperties = wheel.GetObjectPropertiesById(deviceObject.ObjectId);

                            if (axisProperties != null)
                            {
                                axisMinValue_fl64 = axisProperties.Range.Minimum;
                                axisMaxValue_fl64 = axisProperties.Range.Maximum;
                                axisSaturation_fl64 = axisProperties.Saturation;
                            }
                        }
                    }


                    Console.WriteLine($"Found and acquired FFB wheel with VID: {vid:X} and PID: {pid:X}");
                    break;
                }
            }

            if (wheel == null)
            {
                Console.WriteLine($"No FFB wheel found with VID: {targetVID:X} and PID: {targetPID:X}");
            }
        }

        public double ApplySpringEffect(double pos, double targetForce)
        {
            if (wheel == null) return 0;

            // PID calculation
            float forceOutput = 0;
            if (false)
            {
                double currentPosition = (float)pos * 1000.0;
                float error = (float)currentPosition;
                integral += error;
                float derivative = error - previousError;
                forceOutput = (Kp * error) + (Ki * integral) + (Kd * derivative);
                //forceOutput *= -1;
                previousError = error;
            }
            else 
            { 
                forceOutput = (float)targetForce * Kp * (float)axisSaturation_fl64; 
            }

            SetForceFeedback(forceOutput);

            return targetForce;
        }



        public double ReadDeviceState()
        {
            if (wheel == null) return 0;

            //wheel.Poll();
            var state = wheel.GetCurrentState();

            // normalize position
            float currentPosition = state.X;

            if (axisMaxValue_fl64 != 0)
            {
                currentPosition -= (float)axisMinValue_fl64;
                currentPosition /= (float)axisMaxValue_fl64;
                currentPosition -= 0.5f;
                currentPosition *= 2.0f;
            }
            else 
            {
                currentPosition = 0;
            }
            



            float alpha = 0.0f;
            position_filtered_fl = position_filtered_fl * alpha + currentPosition * (1.0f - alpha);
            //currentPosition = position_filtered_fl;


            if (!centerPosHasBeenInitialized)
            {
                centerPosition = currentPosition;
                centerPosHasBeenInitialized = true;
            }



            return position_filtered_fl;
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
