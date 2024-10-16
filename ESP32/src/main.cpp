#include <Adafruit_TinyUSB.h>




// HID report descriptor for Force Feedback (PID)
// Note: This is a basic structure and needs to be tailored to your specific device
uint8_t const desc_hid_report[] = {
    // Custom HID Descriptor - you will need to modify this based on your FFB HID requirements
    0x05, 0x01,       // Usage Page (Generic Desktop Ctrls)
    0x09, 0x04,       // Usage (Joystick)
    0xa1, 0x01,       // Collection (Application)
    // Buttons (12 buttons)
    0x05, 0x09,       // Usage Page (Button)
    0x19, 0x01,       // Usage Minimum (Button 1)
    0x29, 0x0C,       // Usage Maximum (Button 12)
    0x15, 0x00,       // Logical Minimum (0)
    0x25, 0x01,       // Logical Maximum (1)
    0x95, 0x0C,       // Report Count (12)
    0x75, 0x01,       // Report Size (1)
    0x81, 0x02,       // Input (Data,Var,Abs)

    // Axes (X, Y)
    0x05, 0x01,       // Usage Page (Generic Desktop Ctrls)
    0x09, 0x01,       // Usage (Pointer)
    0xA1, 0x00,       // Collection (Physical)
    0x09, 0x30,       // Usage (X)
    0x09, 0x31,       // Usage (Y)
    0x15, 0x81,       // Logical Minimum (-127)
    0x25, 0x7F,       // Logical Maximum (127)
    0x75, 0x08,       // Report Size (8)
    0x95, 0x02,       // Report Count (2)
    0x81, 0x02,       // Input (Data,Var,Abs)
    0xC0,             // End Collection (Physical)

    0xc0              // End Collection (Application)
};

// USB HID instance
Adafruit_USBD_HID usb_hid;

// Buffer to hold HID reports
uint8_t report_buffer[4];

// Force Feedback commands handler (to be implemented)
void handleFFBCommands(uint8_t *data, uint16_t length) {
    // Parse and handle force feedback data here
    // Depending on the incoming command, you can interpret the FFB signals.
    Serial.println("FFB Command Received");
    for (int i = 0; i < length; i++) {
        Serial.print(data[i], HEX);
        Serial.print(" ");
    }
    Serial.println();
}

void setup() {
  Serial.begin(115200);
  
  // Configure USB HID with FFB HID descriptor
  usb_hid.setDescriptor(desc_hid_report, sizeof(desc_hid_report));

  // Begin HID and wait until it is ready
  usb_hid.begin();
  while( !TinyUSBDevice.mounted() ) {
    delay(10);
  }

  Serial.println("HID device initialized");
}

void loop() {
  // Check if HID host sends any data
  if (usb_hid.available()) {
    uint16_t len = usb_hid.recv(report_buffer, sizeof(report_buffer));
    if (len > 0) {
      handleFFBCommands(report_buffer, len);  // Handle FFB commands
    }
  }

  // Send a basic HID report periodically
  report_buffer[0] = 0;  // Button states
  report_buffer[1] = 50; // X axis value
  report_buffer[2] = 50; // Y axis value

  usb_hid.sendReport(0, report_buffer, sizeof(report_buffer));

  delay(10);
}
