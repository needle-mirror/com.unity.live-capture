# Virtual Camera Device component

The Virtual Camera Device is a Live Capture Device that records and applies, in real time, camera properties from a connected Client Device to a [Virtual Camera Actor](ref-component-virtual-camera-actor.md).

The Virtual Camera Device communicates with a specific connected Client Device (Companion App installed on a physical mobile device) in order to retrieve the camera properties. At the same time, the Virtual Camera Device emits a low latency video stream, rendering the animated camera on the Client Device.

![](images/ref-component-virtual-camera-device.png)

## Bindings

| **Property** | **Description** |
|:---|:---|
| **Client Device** | The connected Client Device to use.<br />The selection list reflects the list of Clients that are currently connected to the Server in the [Connections window](ref-window-connections.md). |
| **Actor** | The target Camera Actor to animate. |

## Channels

This section allows you to control which channels to activate or deactivate for recording when the Take Recorder is in Live mode.

Each channel represents a camera property that you might want to animate separately from the others during the recording of a take:
* **Position**
* **Rotation**
* **Focal Length**
* **Focus Distance**
* **Aperture**

## Camera properties

### Lens Asset / Lens

| **Property** | **Description** |
|:---|:---|
| **Lens Asset** | Allows you to set the values stored in an available **LensAsset** which belongs to a **LensKit** asset. |
| **Focal Length** | The focal length of the lens in millimeters. |
| **Focus Distance** | The focus distance of the lens in world units. |
| **Aperture** | The aperture of the lens in f-numbers. |

### Camera Body

| **Property** | **Description** |
|:---|:---|
| **Sensor Preset** | Set the values stored in one of the available **FormatPresets**. |
| **Sensor Size** | The size of the camera sensor in millimeters. |
| **ISO** | Set the sensibility of the real-world camera sensor. Higher values increase the Camera's sensitivity to light and result in faster exposure times. |
| **Shutter Speed** | Sets the exposure time in seconds for the camera. Lower values result in less exposed pictures. |

## Settings

Controls the overall behavior of the camera motion and other functions. These properties are shared with the connected client device.

| **Property** | **Description** |
|:---|:---|
| **Damping** | Enable or disable damping effect.<br />• The **Body** of the damping represents how long it takes, in seconds, to reach the target position. You can set it separately for all three directions<br />• The **Aim** is the same as the body but for the rotation. You can set it once for all three rotations.|
| **Position Lock** | Use these toggle buttons to separately lock the position along all three axes. The position lock is relative to the origin. |
| **Rotation Lock** | Use these toggle buttons to separately lock the rotation around all three axes. The rotation lock is relative to the origin. |
| **Auto Horizon** | Enable this option to force the roll rotation to be zero. You can use this option only when the **Rotation Lock** roll axis is disabled. |
| **Ergonomic Tilt** | Offsets the camera rotation on the tilt axis. This helps you manipulate the Client Device in a more comfortable way.<br />This property is only applied when the client is connected. |
| **Motion Scale** | Applies a scale to the device motion when you control the Virtual Camera position.<br />A scale of (1,1,1) means that the Virtual Camera movement matches the device position in the real world. This is useful if the virtual world is bigger than the physical room space you are using. |
| **Joystick Sensitivity** | Sets the camera motion speed obtained when you use the joysticks.<br />A speed of (1,2,1) moves the pedestal axis two time faster. |
| **Pedestal Space** | Sets the pedestal joystick motion to be relative to the Origin or to the Local pose. |
| **Aspect Ratio Preset** | Allows you to set a camera crop aspect ratio from a list of common values. |
| **Crop Aspect** | The aspect ratio of the crop mask. |
| **Focus Mode** | Disables the focus mode or sets it to manual, automatic or spatial. |
| **Reticle Control** | Allows you to control the reticle from the Device, or from the Game view as well as from the Device (not mutually exclusive). |
| **Focus Distance Offset**   | The offset applied to the focus distance when using auto focus. |

## Video settings

The Open Video Settings button opens the the [Live Capture / Virtual Camera / Video Server](ref-window-preferences.md) section of the Unity project Preferences windows.
