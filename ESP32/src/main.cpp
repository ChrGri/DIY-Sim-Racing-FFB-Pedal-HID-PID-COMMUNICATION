#include <Arduino.h>
#include <Adafruit_TinyUSB.h>  // Correct TinyUSB library for ESP32

// HID Descriptor for a PID (Physical Interface Device) Steering Wheel
uint8_t const hid_report_descriptor[] = {
  0x05, 0x01,                   // Usage Page (Generic Desktop)
  0x09, 0x04,                   // Usage (Joystick)
  0xA1, 0x01,                   // Collection (Application)
  
  // X axis (Steering)
  0x09, 0x30,                   // Usage (X)
  0x15, 0x00,                   // Logical Minimum (0)
  0x26, 0xFF, 0x7F,             // Logical Maximum (32767)
  0x75, 0x10,                   // Report Size (16)
  0x95, 0x01,                   // Report Count (1)
  0x81, 0x02,                   // Input (Data, Variable, Absolute)
  
  // Throttle (Pedal)
  0x09, 0x32,                   // Usage (Z)
  0x15, 0x00,                   // Logical Minimum (0)
  0x26, 0xFF, 0x7F,             // Logical Maximum (32767)
  0x75, 0x10,                   // Report Size (16)
  0x95, 0x01,                   // Report Count (1)
  0x81, 0x02,                   // Input (Data, Variable, Absolute)
  
  // Brake (Pedal)
  0x09, 0x31,                   // Usage (Y)
  0x15, 0x00,                   // Logical Minimum (0)
  0x26, 0xFF, 0x7F,             // Logical Maximum (32767)
  0x75, 0x10,                   // Report Size (16)
  0x95, 0x01,                   // Report Count (1)
  0x81, 0x02,                   // Input (Data, Variable, Absolute)
  
  // Buttons
  0x05, 0x09,                   // Usage Page (Button)
  0x19, 0x01,                   // Usage Minimum (Button 1)
  0x29, 0x10,                   // Usage Maximum (Button 16)
  0x15, 0x00,                   // Logical Minimum (0)
  0x25, 0x01,                   // Logical Maximum (1)
  0x75, 0x01,                   // Report Size (1)
  0x95, 0x10,                   // Report Count (16)
  0x81, 0x02,                   // Input (Data, Variable, Absolute)
  0xC0                          // End Collection
};

// Global constant force feedback value
int16_t constantForceValue = 0;

// Declare the HID device object
Adafruit_USBD_HID usb_hid;

// Define input pins for steering, throttle, and brake
const int steeringPin = 36;  // Example pin
const int throttlePin = 39;  // Example pin
const int brakePin = 34;     // Example pin

// Define button pins
const int button1Pin = 23;   // Example button pin
const int button2Pin = 22;   // Example button pin

void setup() {
  // Initialize serial for debugging
  Serial.begin(115200);
  while (!Serial) delay(10);  // Wait for Serial port to open

  // Initialize analog input pins
  pinMode(steeringPin, INPUT);
  pinMode(throttlePin, INPUT);
  pinMode(brakePin, INPUT);
  
  // Initialize button pins
  pinMode(button1Pin, INPUT_PULLUP);
  pinMode(button2Pin, INPUT_PULLUP);
  
  // Initialize USB HID
  TinyUSBDevice.begin();
  usb_hid.setReportDescriptor(hid_report_descriptor, sizeof(hid_report_descriptor));
  usb_hid.begin();
  
  Serial.println("USB HID Steering Wheel with Force Feedback ready");
}

// Read analog values and map them to HID ranges
int16_t readSteering() {
  return map(analogRead(steeringPin), 0, 4095, -32768, 32767);
}

int16_t readThrottle() {
  return map(analogRead(throttlePin), 0, 4095, 0, 32767);
}

int16_t readBrake() {
  return map(analogRead(brakePin), 0, 4095, 0, 32767);
}

// Read buttons
uint16_t readButtons() {
  uint16_t buttons = 0;
  
  if (digitalRead(button1Pin) == LOW) buttons |= 0x01;
  if (digitalRead(button2Pin) == LOW) buttons |= 0x02;
  
  return buttons;
}

// Process incoming force feedback data
void processForceFeedback(uint8_t const *buffer, uint16_t len) {
  if (len >= 2) {
    constantForceValue = (int16_t)(buffer[0] | (buffer[1] << 8));
    Serial.print("Received Constant Force Value: ");
    Serial.println(constantForceValue);
  }
}

// USB HID report handler (for receiving force feedback)
void tud_hid_report_complete_cb(uint8_t instance, uint8_t const* report, uint8_t len) {
  processForceFeedback(report, len);
}

void loop() {
  // Read analog inputs
  int16_t steering = readSteering();
  int16_t throttle = readThrottle();
  int16_t brake = readBrake();
  
  // Read button states
  uint16_t buttons = readButtons();
  
  // Send gamepad report (Note: You'll need to send appropriate reports for HID)
  usb_hid.sendReport(0, &steering, sizeof(steering));
  usb_hid.sendReport(0, &throttle, sizeof(throttle));
  usb_hid.sendReport(0, &brake, sizeof(brake));
  usb_hid.sendReport(0, &buttons, sizeof(buttons));

  // Simulate constant force by reading the force value and logging
  Serial.print("Applying Constant Force: ");
  Serial.println(constantForceValue);
  
  delay(10); // Small delay to ensure stability
}
