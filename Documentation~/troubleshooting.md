# Troubleshooting

## ***I can't get the app to connect to the editor***

Ensure that you have:

* The proper [network setup](setup-network.md) on your Editor workstation. Most connection issues arise from incorrect network configurations.
* Created a server in the Live Capture window.
* Started the server in the Editor by pressing the **Start** button in the Live Capture window.
* WiFi enabled on both the iPad/iPhone and the Editor workstation and they are connected to the same network.

If you still can't connect: on the app, switch to **Manual** mode and manually enter the port and IP. The port is shown in the Live Capture window. Try all of the different IP values from the **Available Interfaces** section of the Live Capture window until you find one that works.

## ***I'm connected but nothing is happening in the editor***

Ensure that:
* There is a **Take Recorder** object in your scene (**GameObject > Live Capture > Take Recorder**).
* There is a device to handle incoming data streams by adding a device to **Capture Devices** on the **Take Recorder** component in your scene.
* The device game object (e.g. "New VirtualCameraDevice") is a child of a game object with a **Take Recorder** component and the device is assigned in the **Capture Devices** list of the **Take Recorder**.
* The toggle to the left of the device in the **Capture Devices** section of the **Take Recorder** component is enabled.
* **Live** mode is enabled on the **Take Recorder**.
* In the case of using the Virtual Camera or Face Capture apps, ensure that the device has an **Actor** assigned.
