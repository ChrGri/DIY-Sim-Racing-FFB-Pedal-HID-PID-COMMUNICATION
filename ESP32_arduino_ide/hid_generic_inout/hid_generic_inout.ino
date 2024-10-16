/*********************************************************************
 Adafruit invests time and resources providing this open source code,
 please support Adafruit and open-source hardware by purchasing
 products from Adafruit!

 MIT license, check LICENSE for more information
 Copyright (c) 2019 Ha Thach for Adafruit Industries
 All text above, and the splash screen below must be included in
 any redistribution
*********************************************************************/

/* This example demonstrate HID Generic raw Input & Output.
 * It will receive data from Host (In endpoint) and echo back (Out endpoint).
 * HID Report descriptor use vendor for usage page (using template TUD_HID_REPORT_DESC_GENERIC_INOUT)
 *
 * There are 2 ways to test the sketch
 * 1. Using nodejs
 * - Install nodejs and npm to your PC
 *
 * - Install excellent node-hid (https://github.com/node-hid/node-hid) by
 *   $ npm install node-hid
 *
 * - Run provided hid test script
 *   $ node hid_test.js
 *
 * 2. Using python
 * - Install `hid` package (https://pypi.org/project/hid/) by
 *   $ pip install hid
 *
 * - hid package replies on hidapi (https://github.com/libusb/hidapi) for backend,
 *   which already available in Linux. However on windows, you may need to download its dlls from their release page and
 *   copy it over to folder where python is installed.
 *
 * - Run provided hid test script to send and receive data to this device.
 *   $ python3 hid_test.py
 */

#include "Adafruit_TinyUSB.h"



#define USB_DEVICE_DESCRIPTOR_TYPE 0x01
#define USB_HID_DESCRIPTOR_TYPE 0x21

// USB Device Descriptor

const uint8_t DevDesc[] = {
  0x12,             // bLength
  USB_DEVICE_DESCRIPTOR_TYPE, // bDescriptorType
  0x00, 0x02,       // bcdUSB (2.0)
  0x00,             // bDeviceClass (Defined at Interface level)
  0x00,             // bDeviceSubClass
  0x00,             // bDeviceProtocol
  0x40,             // bMaxPacketSize0
  0x00, 0x00,       // idVendor (Replace with your Vendor ID)
  0x00, 0x00,       // idProduct (Replace with your Product ID)
  0x00, 0x01,       // bcdDevice (1.0)
  0x01,             // iManufacturer (String Index)
  0x02,             // iProduct (String Index)
  0x03,             // iSerialNumber (String Index)
  0x01              // bNumConfigurations
};

// USB Configuration Descriptor

const uint8_t CfgDesc[] = {
  0x09,             // bLength
  0x02,             // bDescriptorType (Configuration)
  0x29, 0x00,       // wTotalLength (41 bytes)
  0x01,             // bNumInterfaces
  0x01,             // bConfigurationValue
  0x00,             // iConfiguration (String Index)
  0x80,             // bmAttributes (Bus Powered)
  0x32,             // bMaxPower (100mA)

  // Interface Descriptor
  0x09,             // bLength
  0x04,             // bDescriptorType (Interface)
  0x00,             // bInterfaceNumber
  0x00,             // bAlternateSetting
  0x01,             // bNumEndpoints
  0x03,             // bInterfaceClass    (HID)
  0x00,             // bInterfaceSubClass
  0x00,             // bInterfaceProtocol
  0x00,             // iInterface    (String Index)

  // HID Descriptor
  0x09,             // bLength
  USB_HID_DESCRIPTOR_TYPE, // bDescriptorType (HID)
  0x11, 0x01,       // bcdHID (1.11)
  0x00,             // bCountryCode
  0x01,             // bNumDescriptors
  0x22,             // bDescriptorType (Report)
  0x3F, 0x00,       // wDescriptorLength (63 bytes)

  // Endpoint Descriptor
  0x07,             // bLength
  0x05,             // bDescriptorType (Endpoint)
  0x81,             // bEndpointAddress (IN Endpoint 1)
  0x03,             // bmAttributes (Interrupt)
  0x08, 0x00,       // wMaxPacketSize    (8 bytes)
  0x0A              // bInterval (10ms)
};

// HID Report Descriptor

const uint8_t ReportDesc[] = {
  0x05, 0x01,       // Usage Page (Generic Desktop)
  0x09, 0x04,       // Usage (Joystick)
  0xA1, 0x01,       // Collection (Application)

  // Steering Wheel Axis
  0x09, 0x39,       // Usage (Wheel)
  0x15, 0x81,       // Logical Minimum (-127)
  0x25, 0x7F,       // Logical Maximum (127)
  0x75, 0x08,       // Report Size (8 bits)
  0x95, 0x01,       // Report Count (1)
  0x81, 0x02,       // Input (Data,Var,Abs)

  // Pedals (2 Axes)
  0x05, 0x02,       // Usage Page (Simulation Controls)
  0x09, 0xBB,       // Usage (Throttle)
  0x15, 0x00,       // Logical Minimum (0)
  0x26, 0xFF, 0x00, // Logical Maximum (255)
  0x75, 0x08,       // Report Size (8   bits)
  0x95, 0x01,       // Report Count (1)
  0x81, 0x02,       // Input (Data,Var,Abs)

  0x09, 0xBA,       // Usage (Rudder)
  0x15, 0x00,       // Logical Minimum (0)
  0x26, 0xFF, 0x00, // Logical Maximum (255)
  0x75, 0x08,       // Report Size (8 bits)
  0x95, 0x01,       // Report Count (1)
  0x81, 0x02,       // Input (Data,Var,Abs)

  // Buttons (Example with 4 buttons)
  0x05, 0x09,       // Usage Page (Button)
  0x19, 0x01,       // Usage Minimum (Button 1)
  0x29, 0x04,       // Usage Maximum (Button 4)
  0x15, 0x00,       // Logical Minimum (0)
  0x25, 0x01,       // Logical Maximum (1)
  0x75, 0x01,       // Report Size   (1 bit)
  0x95, 0x04,       // Report Count (4)
  0x81, 0x02,       // Input (Data,Var,Abs)

  // Padding for byte alignment
  0x75, 0x04,       // Report Size (4 bits)
  0x95, 0x01,       // Report Count (1)
  0x81, 0x03,       // Input (Constant)

  0xC0              // End Collection
};


// HID report descriptor using TinyUSB's template
// Generic In Out with 64 bytes report (max)
uint8_t const desc_hid_report2[] = {
    TUD_HID_REPORT_DESC_GENERIC_INOUT(64)
};
uint8_t const desc_hid_report3[] = {
    TUD_HID_REPORT_DESC_GAMEPAD()
};


int16_t steeringPosition = 0;    // Steering axis, 0 center, -32767 full left, 32767 full right

const uint8_t desc_hid_report[] = {
  0x05, 0x01,                    // USAGE_PAGE (Generic Desktop)
  //0x09, 0x04,                    // USAGE (Joystick)
  0x09, 0x06,                    // USAGE (Driving)
  0xA1, 0x01,                    // COLLECTION (Application)
  
  // Steering axis
  0x09, 0x30,                    // USAGE (X)
  0x16, 0x01, 0x80,                    // LOGICAL_MINIMUM (0)
  0x26, 0xFF, 0x7F,              // LOGICAL_MAXIMUM (32767)
  0x75, 0x10,                    // REPORT_SIZE (16 bits)
  0x95, 0x01,                    // REPORT_COUNT (1)
  0x81, 0x02,                    // INPUT (Data, Variable, Absolute)

  0xC0,                           // END_COLLECTION

  /*// Button (Horn)
  0x05, 0x09,                    // USAGE_PAGE (Button)
  0x19, 0x01,                    // USAGE_MINIMUM (Button 1)
  0x29, 0x01,                    // USAGE_MAXIMUM (Button 1)
  0x15, 0x00,                    // LOGICAL_MINIMUM (0)
  0x25, 0x01,                    // LOGICAL_MAXIMUM (1)
  0x75, 0x01,                    // REPORT_SIZE (1 bit)
  0x95, 0x01,                    // REPORT_COUNT (1)
  0x81, 0x02,                    // INPUT (Data, Variable, Absolute)

  // Padding to byte boundary
  0x75, 0x07,                    // REPORT_SIZE (7 bits)
  0x95, 0x01,                    // REPORT_COUNT (1)
  0x81, 0x03,                    // INPUT (Constant, Variable, Absolute)*/

  // Force Feedback
  /*0x05, 0x0F,                    // USAGE_PAGE (Force Feedback)
  0x09, 0xBB,                    // USAGE (Effect Block Index)
  0x15, 0x00,                    // LOGICAL_MINIMUM (0)
  0x25, 0x01,                    // LOGICAL_MAXIMUM (1) // Assuming only one effect block for now
  0x75, 0x08,                    // REPORT_SIZE (8 bits)
  0x95, 0x01,                    // REPORT_COUNT (1)
  0x91, 0x02,                    // OUTPUT (Data, Variable, Absolute)

  0x09, 0xC4,                    // USAGE (Effect Magnitude)
  0x15, 0x00,                    // LOGICAL_MINIMUM (0)
  0x26, 0xFF, 0x7F,              // LOGICAL_MAXIMUM (32767)
  0x75, 0x10,                    // REPORT_SIZE (16 bits)
  0x95, 0x01,                    // REPORT_COUNT (1)
  0x91, 0x02,                    // OUTPUT (Data, Variable, Absolute)

  0xC0                           // END_COLLECTION
  */
};


// USB HID object
Adafruit_USBD_HID usb_hid;

// the setup function runs once when you press reset or power the board
void setup() {
  // Manual begin() is required on core without built-in support e.g. mbed rp2040

  //TinyUSBDevice.setProductDescriptor(DevDesc);
  //TinyUSBDevice.setManufacturerDescriptor("DIY");
  //TinyUSBDevice.setProductDescriptor("S2 mini wheel");

  if (!TinyUSBDevice.isInitialized()) {
    TinyUSBDevice.begin(0);
  }

  Serial.begin(115200);


  // Notes: following commented-out functions has no affect on ESP32
  usb_hid.enableOutEndpoint(true);
  usb_hid.setPollInterval(2);
  //usb_hid.setReportDescriptor(ReportDesc, sizeof(ReportDesc));
  //usb_hid.setStringDescriptor("S2 mini steering");
  //usb_hid.setReportCallback();

  // If already enumerated, additional class driverr begin() e.g msc, hid, midi won't take effect until re-enumeration
  if (TinyUSBDevice.mounted()) {
    TinyUSBDevice.detach();
    delay(10);
    TinyUSBDevice.attach();
  }

  Serial.println("Adafruit TinyUSB HID Generic In Out example");
}

void loop() {

  // Simulate steering wheel movement (oscillating back and forth)
  steeringPosition = (sin(millis() / 1000.0 * 2 * PI) * 32767);  // Generate a sinusoidal steering position
  

  #ifdef TINYUSB_NEED_POLLING_TASK
  // Manual call tud_task since it isn't called by Core's background
  TinyUSBDevice.task();
  #endif

  uint8_t report[5]; // 2 bytes for steering, 1 byte for button, 2 bytes for effect magnitude
  
  // Steering axis (16-bit, little-endian)
  report[0] = steeringPosition & 0xFF;
  report[1] = (steeringPosition >> 8) & 0xFF;
  usb_hid.sendReport(0, report, sizeof(report));

  delay(10);
}

// Invoked when received GET_REPORT control request
// Application must fill buffer report's content and return its length.
// Return zero will cause the stack to STALL request
uint16_t get_report_callback(uint8_t report_id, hid_report_type_t report_type, uint8_t* buffer, uint16_t reqlen) {
  // not used in this example
  (void) report_id;
  (void) report_type;
  (void) buffer;
  (void) reqlen;
  return 0;
}

// Invoked when received SET_REPORT control request or
// received data on OUT endpoint ( Report ID = 0, Type = 0 )
void set_report_callback(uint8_t report_id, hid_report_type_t report_type, uint8_t const* buffer, uint16_t bufsize) {
  // This example doesn't use multiple report and report ID
  (void) report_id;
  (void) report_type;



   uint8_t report[5]; // 2 bytes for steering, 1 byte for button, 2 bytes for effect magnitude
  
  // Steering axis (16-bit, little-endian)
  report[0] = steeringPosition & 0xFF;
  report[1] = (steeringPosition >> 8) & 0xFF;
  
  // Button (1 bit for button pressed/released)
  /*report[2] = buttonPressed ? 0x01 : 0x00;

  // Effect Block Index
  report[3] = effectBlockIndex;

  // Effect Magnitude (16-bit, little-endian)
  report[4] = effectMagnitude & 0xFF;
  report[5] = (effectMagnitude >> 8) & 0xFF;
*/
  // echo back anything we received from host
  //usb_hid.sendReport(0, buffer, bufsize);

  usb_hid.sendReport(0, report, sizeof(report));
}




// Diese Funktionen werden von TinyUSB intern aufgerufen, um die Deskriptoren abzurufen
const uint8_t * tud_descriptor_device_cb(void) {
  return DevDesc;
}

const uint8_t * tud_descriptor_configuration_cb(uint8_t index)
 {
  (void) index; // für den Moment nicht verwendet
  return CfgDesc;
}

/*const uint16_t  tud_descriptor_string_cb(uint8_t index, uint16_t langid) {
  (void) index; (void) langid;
  return NULL; // String   
}*/

const uint8_t * tud_hid_descriptor_report_cb(uint8_t itf) {
  (void) itf;
  return desc_hid_report;
}
