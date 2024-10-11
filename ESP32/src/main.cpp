#include <Arduino.h>
#include "USB.h"
#include "tusb.h"

// HID PID Report Descriptor for Steering Wheel
const uint8_t hid_report_descriptor[] = {
  // HID descriptor for steering wheel
  0x05, 0x01,        // Usage Page (Generic Desktop)
  0x09, 0x04,        // Usage (Joystick)
  0xA1, 0x01,        // Collection (Application)
  0xA1, 0x02,        // Collection (Logical)
  
  // Axis (steering)
  0x09, 0x30,        // Usage (X - Axis)
  0x15, 0x00,        // Logical Minimum (0)
  0x26, 0xFF, 0x03,  // Logical Maximum (1023)
  0x75, 0x10,        // Report Size (16)
  0x95, 0x01,        // Report Count (1)
  0x81, 0x02,        // Input (Data, Variable, Absolute)

  // Buttons
  0x09, 0x01,        // Usage (Button 1)
  0x15, 0x00,        // Logical Minimum (0)
  0x25, 0x01,        // Logical Maximum (1)
  0x75, 0x01,        // Report Size (1)
  0x95, 0x08,        // Report Count (8)
  0x81, 0x02,        // Input (Data, Variable, Absolute)

  // Force Feedback
  0x05, 0x0F,        // Usage Page (Physical Interface)
  0x09, 0x21,        // Usage (Set Effect Report)
  0xA1, 0x02,        // Collection (Logical)
  0x85, 0x01,        // Report ID (1)
  0x09, 0x22,        // Usage (Effect Block Index)
  0x15, 0x01,        // Logical Minimum (1)
  0x25, 0x28,        // Logical Maximum (40)
  0x75, 0x08,        // Report Size (8)
  0x95, 0x01,        // Report Count (1)
  0x91, 0x02,        // Output (Data, Variable, Absolute)
  0xC0,              // End Collection
  0xC0               // End Collection
};

// Force feedback parameters
struct {
  uint8_t effectBlockIndex;
  uint16_t effectMagnitude;
  uint16_t effectDuration;
} force_feedback;

// Steering axis and button states
int16_t steeringPosition = 0;
uint8_t buttonState = 0;

USBHID usbHID;

void setup() {
  Serial.begin(115200);

  // Initialize USB HID
  usbHID.begin(hid_report_descriptor, sizeof(hid_report_descriptor));

  // Setup Force Feedback effect
  memset(&force_feedback, 0, sizeof(force_feedback));



  // Setup digital input for button
  pinMode(2, INPUT_PULLUP); // Button on GPIO 2
}



// Sends the HID input report to the host
void sendHIDReport() {
  uint8_t report[3];

  // Steering position (2 bytes, little-endian)
  report[0] = steeringPosition & 0xFF;
  report[1] = (steeringPosition >> 8) & 0xFF;

  // Button state (1 byte)
  report[2] = buttonState;

  // Send report to the host
  usbHID.sendReport(report, sizeof(report));
}

// Polls for HID output reports from the host (force feedback)
void pollHIDOutputReport() {
  uint8_t report[64]; // Adjust buffer size based on output report size

  // Check if there's a report from the host
  int len = usbHID.receiveReport(report, sizeof(report));
  if (len > 0) {
    // Process force feedback command from the host
    processForceFeedback(report, len);
  }
}

// Process the received force feedback report
void processForceFeedback(const uint8_t* report, uint16_t len) {
  // Parse the report (this is just an example, you need to adapt it based on the report structure)
  force_feedback.effectBlockIndex = report[0]; // Effect block index
  force_feedback.effectMagnitude = report[1] | (report[2] << 8); // Magnitude
  force_feedback.effectDuration = report[3] | (report[4] << 8);  // Duration

  // Example: Print received force feedback parameters
  Serial.print("FFB Effect Block Index: ");
  Serial.println(force_feedback.effectBlockIndex);
  Serial.print("FFB Magnitude: ");
  Serial.println(force_feedback.effectMagnitude);
  Serial.print("FFB Duration: ");
  Serial.println(force_feedback.effectDuration);

  // Apply force feedback (e.g., adjust motor, apply constant force, etc.)
  applyForceFeedback();
}

// Apply the force feedback effect to the steering wheel
void applyForceFeedback() {
  // Placeholder for motor control logic
  // You can control motor or actuator here based on the force feedback parameters
  Serial.println("Applying force feedback to motor/actuator...");
}


void loop() {
  // Read steering position from ADC

  // Read button state (pressed or not)
  buttonState = digitalRead(2) == LOW ? 0x01 : 0x00;

  // Send HID input report to the host (steering + button states)
  sendHIDReport();

  // Poll for HID output reports (force feedback commands)
  pollHIDOutputReport();

  delay(10); // Small delay to reduce CPU usage
}