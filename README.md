# DIY-Sim-Racing-FFB-Pedal-HID-PID-COMMUNICATION

To make the FFB pedal compatible with classical FFB wheel signals and thus make it fully controlable via racing games, I needed to understand the HID PID protocol. Here is an app to controll my steering wheel via HID PID messages. 

The GUI of the app is depicted in the image below:<br>
![image](https://github.com/user-attachments/assets/6544b976-54b0-4020-99cf-a8efe246ccbe)


The next steps will be to make the FFB pedal accept the HID PID messages.


# ToDo

- list available steering wheels in drop down menu
- make steering wheel selectable from drop-down menu
- takeover force-travel curve from SimHub plugin
- make PID parameters tunable
- filter settings
- tunable update rate
- design ESP32 S2 firmware tonreceive HID PID messages
