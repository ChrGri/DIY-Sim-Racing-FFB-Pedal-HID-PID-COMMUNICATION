# DIY-Sim-Racing-FFB-Pedal-HID-PID-COMMUNICATION

To make the FFB pedal compatible with classical FFB wheel signals and thus make it fully controlable via racing games, I needed to understand the HID PID protocol. Here is an app to controll my steering wheel via HID PID messages. 

The GUI of the app is depicted in the image below:<br>
![Aufzeichnung2024-10-14081815-ezgif com-video-to-gif-converter](https://github.com/user-attachments/assets/2cc9911d-19fb-4f66-85d6-e254df3275c3)

The next steps will be to make the FFB pedal accept the HID PID messages.


# ToDo

- list available steering wheels in drop down menu
- make steering wheel selectable from drop-down menu
- takeover force-travel curve from SimHub plugin
- make PID parameters tunable
- filter settings
- tunable update rate
- design ESP32 S2 firmware tonreceive HID PID messages
